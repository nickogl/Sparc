using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sparc.IO;
using Sparc.IO.Readers;
using Sparc.IO.Writers;
using System.Numerics;

namespace Sparc;

/// <summary>
/// Extension methods for setting up RPC services and clients in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds Sparc core services, including built-in parameter readers and writers,
	/// shared generation infrastructure, and other framework-level dependencies.
	/// </summary>
	/// <remarks>
	/// If you want to override parameter readers/writers for certain types, register
	/// them as singletons prior to calling any Sparc DI extension method.
	/// </remarks>
	/// <param name="services">The service collection to add Sparc services to.</param>
	/// <returns>The same service collection for chaining.</returns>
	public static IServiceCollection AddSparc(this IServiceCollection services)
	{
		services.AddMetrics();
		services.TryAddSingleton<OperationMetrics>();

		AddParameterReaders(services);

		AddParameterWriters(services);

		return services;
	}

	/// <summary>
	/// Registers a <typeparamref name="TService"/> for the specified connection type and
	/// makes a <see cref="IServiceDispatcher{TService, TConnection}"/> available to your
	/// transport layer through dependency injection.
	/// </summary>
	/// <remarks>
	/// If you want to override <typeparamref name="TService"/> or parameter readers for
	/// certain types, register them as singletons prior to calling any DI extension method.
	/// </remarks>
	/// <typeparam name="TService">The service contract type that declares its various operations.</typeparam>
	/// <typeparam name="TConnection">The connection type passed as the first parameter to service operations.</typeparam>
	/// <param name="services">The service collection to add the service registration to.</param>
	/// <returns>The same service collection for chaining.</returns>
	public static IServiceCollection AddSparcService<TService, TConnection>(this IServiceCollection services)
		where TService : class
		where TConnection : IClientConnection
	{
		services.AddSparc();

		services.TryAddSingleton<TService>();
		services.TryAddSingleton<IServiceDispatcher<TService, TConnection>, ServiceDispatcher<TService, TConnection>>();

		return services;
	}

	/// <summary>
	/// Registers a client contract and makes a <see cref="IClientProxyFactory{TClient}"/>
	/// available to your services through dependency injection. Then simply call
	/// <see cref="IClientProxyFactory{TClient}.Create"/> in your service's constructor
	/// to create a proxy and invoke methods on the client(s) in your operations.
	/// </summary>
	/// <typeparam name="TClient">The client contract type that declares its various operations.</typeparam>
	/// <param name="services">The service collection to add the client registration to.</param>
	/// <returns>The same service collection for chaining.</returns>
	public static IServiceCollection AddSparcClient<TClient>(this IServiceCollection services) where TClient : class
	{
		services.AddSparc();

		services.TryAddSingleton<IClientProxyFactory<TClient>, ClientProxyFactory<TClient>>();

		return services;
	}

	private static void AddParameterReaders(IServiceCollection services)
	{
		// Simple types
		services.TryAddSingleton<IParameterReader<bool>, BooleanParameterReader>();
		services.TryAddSingleton<IParameterReader<byte>, ByteParameterReader>();
		services.TryAddSingleton<IParameterReader<char>, CharParameterReader>();
		services.TryAddSingleton<IParameterReader<DateTime>, DateTimeParameterReader>();
		services.TryAddSingleton<IParameterReader<DateTimeOffset>, DateTimeOffsetParameterReader>();
		services.TryAddSingleton<IParameterReader<decimal>, DecimalParameterReader>();
		services.TryAddSingleton<IParameterReader<double>, DoubleParameterReader>();
		services.TryAddSingleton<IParameterReader<float>, FloatParameterReader>();
		services.TryAddSingleton<IParameterReader<Guid>, GuidParameterReader>();
		services.TryAddSingleton<IParameterReader<short>, Int16ParameterReader>();
		services.TryAddSingleton<IParameterReader<int>, Int32ParameterReader>();
		services.TryAddSingleton<IParameterReader<long>, Int64ParameterReader>();
		services.TryAddSingleton<IParameterReader<Int128>, Int128ParameterReader>();
		services.TryAddSingleton<IParameterReader<sbyte>, SByteParameterReader>();
		services.TryAddSingleton<IParameterReader<string>, StringParameterReader>();
		services.TryAddSingleton<IParameterReader<TimeSpan>, TimeSpanParameterReader>();
		services.TryAddSingleton<IParameterReader<ushort>, UInt16ParameterReader>();
		services.TryAddSingleton<IParameterReader<uint>, UInt32ParameterReader>();
		services.TryAddSingleton<IParameterReader<ulong>, UInt64ParameterReader>();
		services.TryAddSingleton<IParameterReader<UInt128>, UInt128ParameterReader>();

		// Container types
		services.TryAddSingleton(typeof(ArrayParameterReader<>));
		services.TryAddSingleton(typeof(ListParameterReader<>));
		services.TryAddSingleton(typeof(DictionaryParameterReader<,>));
		services.TryAddSingleton(typeof(NullableParameterReader<>));
	}

	private static void AddParameterWriters(IServiceCollection services)
	{
		// Simple types
		services.TryAddSingleton<IParameterWriter<bool>, BooleanParameterWriter>();
		services.TryAddSingleton<IParameterWriter<byte>, ByteParameterWriter>();
		services.TryAddSingleton<IParameterWriter<char>, CharParameterWriter>();
		services.TryAddSingleton<IParameterWriter<DateTime>, DateTimeParameterWriter>();
		services.TryAddSingleton<IParameterWriter<DateTimeOffset>, DateTimeOffsetParameterWriter>();
		services.TryAddSingleton<IParameterWriter<decimal>, DecimalParameterWriter>();
		services.TryAddSingleton<IParameterWriter<double>, DoubleParameterWriter>();
		services.TryAddSingleton<IParameterWriter<float>, FloatParameterWriter>();
		services.TryAddSingleton<IParameterWriter<Guid>, GuidParameterWriter>();
		services.TryAddSingleton<IParameterWriter<short>, Int16ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<int>, Int32ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<Int128>, Int128ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<sbyte>, SByteParameterWriter>();
		services.TryAddSingleton<IParameterWriter<string>, StringParameterWriter>();
		services.TryAddSingleton<IParameterWriter<TimeSpan>, TimeSpanParameterWriter>();
		services.TryAddSingleton<IParameterWriter<ushort>, UInt16ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<uint>, UInt32ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<ulong>, UInt64ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<UInt128>, UInt128ParameterWriter>();
		services.TryAddSingleton<IParameterWriter<long>, Int64ParameterWriter>();

		// Container types
		services.TryAddSingleton(typeof(ArrayParameterWriter<>));
		services.TryAddSingleton(typeof(ListParameterWriter<>));
		services.TryAddSingleton(typeof(DictionaryParameterWriter<,>));
		services.TryAddSingleton(typeof(NullableParameterWriter<>));
	}
}
