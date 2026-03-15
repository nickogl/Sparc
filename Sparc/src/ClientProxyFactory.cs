using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Sparc;

internal sealed class ClientProxyFactory<TClient> : IClientProxyFactory<TClient> where TClient : class
{
	private readonly static MethodInfo _getTypeFromHandleMethod =
		typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])!;
	private readonly static MethodInfo _getRequiredServiceMethod =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(m =>
				m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) &&
				m.IsGenericMethodDefinition &&
				m.GetParameters().Length == 1);
	private readonly static ConstructorInfo _objectConstructor =
		typeof(object).GetConstructor(Type.EmptyTypes)!;
	private readonly static ConstructorInfo _payloadWriterConstructor =
		typeof(PayloadWriter).GetConstructor([typeof(int)])!;
	private readonly static MethodInfo _writeOperationId =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(ClientProxyFactoryHelpers.WriteOperationId))!;
	private readonly static MethodInfo _wrappedSendMethod =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(ClientProxyFactoryHelpers.WrappedSendAsync))!;
	private readonly static ConcurrentDictionary<Type, MethodInfo> _getRequiredServiceByType = [];
	private readonly static ConcurrentDictionary<Type, MethodInfo> _parameterWriteMethodByType = [];
	private readonly TClient _client;

	public ClientProxyFactory(IServiceProvider services)
	{
		var operations = ValidateClient();
		_client = CompileClient(operations, services);
	}

	public TClient Create()
	{
		// This is a stateless object for now, so we just create it once in the constructor
		// and then re-use it across all services that inject this client. We can change
		// this method to create clients on the fly if needed in the future.
		return _client;
	}

	private static TClient CompileClient(IEnumerable<OperationSpec> operations, IServiceProvider services)
	{
		// NOTE: ModuleBuiler is not thread-safe
		using var _ = ClientProxyFactoryHelpers.ModuleBuilderLock.EnterScope();

		var proxyName = $"ClientProxy_{Guid.NewGuid()}";
		var proxy = ClientProxyFactoryHelpers.ModuleBuilder.DefineType(
			proxyName,
			TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
		proxy.AddInterfaceImplementation(typeof(TClient));

		// Field for the metrics instance to record outbound payload sizes
		var metricsField = proxy.DefineField("_metrics", typeof(OperationMetrics), FieldAttributes.Private | FieldAttributes.InitOnly);
		// Fields containing the effective parameter writer implementations
		var writerFields = new Dictionary<Type, FieldBuilder>();
		foreach (var operation in operations)
		{
			foreach (var parameter in operation.EffectiveParameters)
			{
				if (!writerFields.ContainsKey(parameter.ParameterType))
				{
					writerFields[parameter.ParameterType] = proxy.DefineField(
						$"_parameterWriter{writerFields.Count}",
						typeof(IParameterWriter<>).MakeGenericType(parameter.ParameterType),
						FieldAttributes.Private | FieldAttributes.InitOnly);
				}
			}
		}


		// Constructor to assign the writer fields which are ultimately used in the operations
		var proxyConstructor = proxy.DefineConstructor(
			MethodAttributes.Public,
			CallingConventions.Standard,
			[typeof(IServiceProvider)]);
		EmitClientConstructor(proxyConstructor.GetILGenerator(), metricsField, writerFields.Values);

		// One class method for each client operation
		foreach (var operation in operations)
		{
			var proxyOperation = proxy.DefineMethod(
				operation.Method.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				operation.Method.ReturnType,
				[.. operation.Parameters.Select(p => p.ParameterType)]);
			proxy.DefineMethodOverride(proxyOperation, operation.Method);
			EmitClientOperation(proxyOperation.GetILGenerator(), operation, metricsField, writerFields);
		}

		var proxyType = proxy.CreateType();

		return (TClient)Activator.CreateInstance(proxyType, services)!;
	}

	private static void EmitClientConstructor(ILGenerator il, FieldBuilder metricsField, IEnumerable<FieldBuilder> writerFields)
	{
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, _objectConstructor);

		// _metrics = serviceProvider.GetRequiredService<OperationMetrics>()
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Call, _getRequiredServiceByType.GetOrAdd(typeof(OperationMetrics), type =>
		{
			return _getRequiredServiceMethod.MakeGenericMethod(type);
		}));
		il.Emit(OpCodes.Stfld, metricsField);

		// _parameterWriter0..N = serviceProvider.GetRequiredService<ParamWriter0..N>()
		foreach (var field in writerFields)
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, _getRequiredServiceByType.GetOrAdd(field.FieldType, type =>
			{
				return _getRequiredServiceMethod.MakeGenericMethod(type);
			}));
			il.Emit(OpCodes.Stfld, field);
		}

		il.Emit(OpCodes.Ret);
	}

	private static void EmitClientOperation(ILGenerator il, OperationSpec operation, FieldBuilder metricsField, Dictionary<Type, FieldBuilder> writerFields)
	{
		// var payloadWriter = new PayloadWriter(initialBufferSize)
		var writerLocal = il.DeclareLocal(typeof(PayloadWriter));
		il.Emit(OpCodes.Ldc_I4, operation.Metadata.InitialBufferSize);
		il.Emit(OpCodes.Newobj, _payloadWriterConstructor);
		il.Emit(OpCodes.Stloc, writerLocal);

		// WriteOperationIdToBuffer(ref payloadWriter, operationId)
		il.Emit(OpCodes.Ldloca_S, writerLocal);
		il.Emit(OpCodes.Ldc_I4, operation.Metadata.OperationId);
		il.Emit(OpCodes.Call, _writeOperationId);

		// _parameterWriter0..N.Write(ref payloadWriter, parameter0..N)
		for (int index = 0; index < operation.EffectiveParameters.Length; index++)
		{
			var field = writerFields[operation.EffectiveParameters[index].ParameterType];
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			il.Emit(OpCodes.Ldloca_S, writerLocal);
			LoadOperationParameter(il, index);
			il.Emit(OpCodes.Callvirt, _parameterWriteMethodByType.GetOrAdd(field.FieldType, type =>
			{
				return type.GetMethod(nameof(IParameterWriter<>.Write))!;
			}));
		}

		// _metrics.RecordOutboundPayloadSize(contract, operation, operationId, payloadWriter.Written)
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, metricsField);
		il.Emit(OpCodes.Ldstr, typeof(TClient).Name);
		il.Emit(OpCodes.Ldstr, operation.Method.Name);
		il.Emit(OpCodes.Ldc_I4, operation.Metadata.OperationId);
		il.Emit(OpCodes.Ldloca_S, writerLocal);
		il.Emit(OpCodes.Call, ClientProxyFactoryHelpers.PayloadWriterGetWrittenMethod);
		il.Emit(OpCodes.Callvirt, ClientProxyFactoryHelpers.RecordOutboundPayloadSizeMethod);

		// WrappedSendAsync(connection, ref payloadWriter, cancellationToken)
		// We wrap the send call here because in case it does not complete synchronously,
		// we have to await it before we can safely dispose of the payload writer's buffer
		// and we cannot do that here without generating the async state machine ourselves
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldloca_S, writerLocal);
		LoadOperationParameter(il, operation.EffectiveParameters.Length);
		il.Emit(OpCodes.Call, _wrappedSendMethod);
		il.Emit(OpCodes.Ret);

		static void LoadOperationParameter(ILGenerator il, int index)
		{
			// We pretty much forward the parameters in order of definition, shifted
			// by 2 due to the "this" pointer and connection parameter
			if (index == 0) il.Emit(OpCodes.Ldarg_2);
			else if (index == 1) il.Emit(OpCodes.Ldarg_3);
			else il.Emit(OpCodes.Ldarga_S, (byte)(index + 2));
		}
	}

	private static IEnumerable<OperationSpec> ValidateClient()
	{
		if (!typeof(TClient).IsInterface)
		{
			ThrowProxyError("Must be an interface");
		}

		var encounteredOperationsIds = new HashSet<int>();
		foreach (var (method, metadata) in FindOperations())
		{
			if (!encounteredOperationsIds.Add(metadata.OperationId))
			{
				ThrowProxyError(method, "Operation ID is already used by another operation");
			}

			if (method.ReturnType != typeof(ValueTask))
			{
				ThrowProxyError(method, "Must return a 'ValueTask'");
			}

			var parameters = method.GetParameters();
			if (parameters.Length < 2)
			{
				ThrowProxyError(method, "Must take at least two parameters, the concrete type of the connection and a cancellation token");
			}
			if (parameters[0].ParameterType.IsByRef)
			{
				ThrowProxyError(method, "Connection type must not be passed by reference");
			}
			if (parameters[0].ParameterType != typeof(IClientConnection))
			{
				ThrowProxyError(method, "Connection type must be of type 'IClientConnection'");
			}
			for (int i = 1; i < parameters.Length - 1; i++)
			{
				var parameter = parameters[i];
				if (parameter.ParameterType.IsByRef)
				{
					ThrowProxyError(method, $"Parameter at index {i} must not be passed by reference");
				}
				if (parameters[i].ParameterType == typeof(CancellationToken))
				{
					ThrowProxyError(method, $"Parameter at index {i} is of type 'CancellationToken', but it may only appear as the last parameter");
				}
			}
			if (parameters[^1].ParameterType != typeof(CancellationToken))
			{
				ThrowProxyError(method, "Last parameter must be of type 'CancellationToken'");
			}

			var effectiveParameters = new ParameterInfo[parameters.Length - 2];
			parameters.AsSpan(1..^1).CopyTo(effectiveParameters);
			yield return new(method, parameters, effectiveParameters, metadata);
		}
	}

	private static IEnumerable<(MethodInfo, OperationAttribute)> FindOperations()
	{
		foreach (var type in typeof(TClient).GetInterfaces().Append(typeof(TClient)))
		{
			foreach (var method in type.GetMethods())
			{
				var operation = method.GetCustomAttribute<OperationAttribute>();
				if (operation is null)
				{
					ThrowProxyError($"All methods must be annotated with [Operation], but '{method.Name}' was not");
				}

				yield return (method, operation);
			}
		}
	}

	[DoesNotReturn]
	private static void ThrowProxyError(string error)
	{
		throw new ArgumentException($"Failed to create proxy for client '{typeof(TClient).FullName}': {error}");
	}

	[DoesNotReturn]
	private static void ThrowProxyError(MethodInfo method, string error)
	{
		throw new ArgumentException($"Failed to create proxy for operation '{method.DeclaringType?.FullName ?? "<global>"}.{method.Name}': {error}");
	}

	private readonly record struct OperationSpec(
		MethodInfo Method,
		ParameterInfo[] Parameters,
		ParameterInfo[] EffectiveParameters,
		OperationAttribute Metadata)
	{
	}
}
