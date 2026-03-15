using Microsoft.Extensions.DependencyInjection;
using Sparc.Exceptions;
using Sparc.IO;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sparc;

internal sealed class ServiceDispatcher<TService, TConnection> : IServiceDispatcher<TService, TConnection>
		where TService : class
		where TConnection : IClientConnection
{
	private delegate ValueTask OperationWrapper(TConnection connection, ReadOnlySpan<byte> payload, CancellationToken cancellationToken);

	private readonly static string _contractName = typeof(TService).Name;
	private readonly OperationMetrics _operationMetrics;
	private readonly Dictionary<int, OperationWrapper> _operations;

	public ServiceDispatcher(IServiceProvider di)
	{
		var service = di.GetRequiredService<TService>();
		_operationMetrics = di.GetRequiredService<OperationMetrics>();
		_operations = CompileOperations(service, di);
	}

	public ValueTask DispatchAsync(
		int operationId,
		ReadOnlySpan<byte> payload,
		TConnection connection,
		CancellationToken cancellationToken = default)
	{
		if (!_operations.TryGetValue(operationId, out var operation))
		{
			ThrowUnknownOperation(operationId);
		}

		_operationMetrics.RecordInboundPayloadSize(
			_contractName,
			operation.Method.Name,
			operationId,
			payload.Length + sizeof(int));
		return operation(connection, payload, cancellationToken);
	}

	private static Dictionary<int, OperationWrapper> CompileOperations(TService service, IServiceProvider di)
	{
		var result = new Dictionary<int, OperationWrapper>();
		foreach (var (method, metadata) in FindOperations())
		{
			var parameters = ValidateOperation(method);
			var hasCancellationTokenParameter = HasCancellationTokenParameter(method, parameters);
			var operationParameterCount = hasCancellationTokenParameter ? parameters.Length - 1 : parameters.Length;

			// Operation wrapper parameters
			var connectionParameter = Expression.Parameter(typeof(TConnection));
			var payloadParameter = Expression.Parameter(typeof(ReadOnlySpan<byte>));
			var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken));

			// Wrapper block scope
			var readerVariable = Expression.Variable(typeof(PayloadReader));
			var variables = new List<ParameterExpression>(capacity: operationParameterCount) { readerVariable };
			var createReader = Expression.Assign(readerVariable, Expression.New(ServiceDispatcherHelpers.PayloadReaderConstructor, payloadParameter));
			var statements = new List<Expression>(capacity: operationParameterCount) { createReader };

			// Create variables for each parameter to pass it to the operation
			var operationParameters = new List<Expression>(capacity: operationParameterCount) { connectionParameter };
			for (int i = 1; i < operationParameterCount; i++)
			{
				var parameter = parameters[i];
				var parameterVariable = Expression.Variable(parameter.ParameterType);
				variables.Add(parameterVariable);
				operationParameters.Add(parameterVariable);

				var (paramReader, paramReadMethod) = ServiceDispatcherHelpers.GetOrAddParameterReader(parameters[i].ParameterType, di);
				var paramReaderExpression = Expression.Constant(paramReader);
				var paramReadCall = Expression.Call(paramReaderExpression, paramReadMethod, readerVariable);
				statements.Add(Expression.Assign(parameterVariable, paramReadCall));
			}
			if (hasCancellationTokenParameter)
			{
				// If the operation's last parameter is CancellationToken, forward it
				operationParameters.Add(cancellationTokenParameter);
			}

			// Check for unconsumed data from the payload prior to calling the operation
			var unconsumedError = Expression.New(ServiceDispatcherHelpers.UnconsumedExceptionConstructor, Expression.Constant(metadata.OperationId));
			var throwUnconsumedError = Expression.Throw(unconsumedError);
			var notEndOfPayload = Expression.Not(Expression.Property(readerVariable, ServiceDispatcherHelpers.PayloadReaderEndProperty));
			statements.Add(Expression.IfThen(notEndOfPayload, throwUnconsumedError));
			var operationTarget = Expression.Constant(service);
			var operationExpression = Expression.Call(operationTarget, method, operationParameters);
			statements.Add(operationExpression);

			var wrapperBody = Expression.Block(variables, statements);
			var wrapperParams = new[] { connectionParameter, payloadParameter, cancellationTokenParameter };
			var wrapper = Expression.Lambda<OperationWrapper>(wrapperBody, method.Name, wrapperParams).Compile();
			if (!result.TryAdd(metadata.OperationId, wrapper))
			{
				ThrowOperationError(method, "Operation ID is already used by another operation");
			}
		}

		return result;
	}

	private static IEnumerable<(MethodInfo, OperationAttribute)> FindOperations()
	{
		foreach (var implementedInterface in typeof(TService).GetInterfaces().Append(typeof(TService)))
		{
			foreach (var method in implementedInterface.GetMethods())
			{
				var operation = method.GetCustomAttribute<OperationAttribute>();
				if (operation is not null)
				{
					yield return (method, operation);
				}
			}
		}
	}

	private static ParameterInfo[] ValidateOperation(MethodInfo method)
	{
		if (method.IsStatic)
		{
			ThrowOperationError(method, "Must not be static");
		}
		if (method.ReturnType != typeof(ValueTask))
		{
			ThrowOperationError(method, "Must return a 'ValueTask'");
		}

		var parameters = method.GetParameters();
		if (parameters.Length == 0)
		{
			ThrowOperationError(method, "Must take at least one parameter, the concrete type of the connection");
		}
		if (parameters[0].ParameterType.IsByRef)
		{
			ThrowOperationError(method, "Connection type must not be passed by reference");
		}
		if (parameters[0].ParameterType != typeof(TConnection))
		{
			ThrowOperationError(method, $"Connection type must be of type '{typeof(TConnection).FullName}'");
		}
		for (int i = 1; i < parameters.Length; i++)
		{
			var parameter = parameters[i];
			if (parameter.ParameterType.IsByRef)
			{
				ThrowOperationError(method, $"Parameter at index {i} must not be passed by reference");
			}
		}

		return parameters;
	}

	private static bool HasCancellationTokenParameter(MethodInfo method, ParameterInfo[] parameters)
	{
		var result = parameters[^1].ParameterType == typeof(CancellationToken);
		for (int i = parameters.Length - 2; i >= 0; i--)
		{
			if (parameters[i].ParameterType == typeof(CancellationToken))
			{
				ThrowOperationError(method, $"Parameter at index {i} is of type 'CancellationToken', but it may only appear as the last parameter");
			}
		}

		return result;
	}

	[DoesNotReturn]
	private static void ThrowOperationError(MethodInfo method, string error)
	{
		throw new ArgumentException($"Failed to compile operation '{method.DeclaringType?.FullName ?? "<global>"}.{method.Name}': {error}");
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowUnknownOperation(int operationId)
	{
		throw new UnknownOperationException(typeof(TService), operationId);
	}
}
