using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
using Sparc.IO.Readers;
using Sparc.IO.Writers;

namespace Sparc;

internal static class ServiceProviderExtensions
{
	public static object GetParameterReaderOrWriterService(this IServiceProvider services, Type readerOrWriterType)
	{
		if (readerOrWriterType.IsGenericType && readerOrWriterType.GetGenericTypeDefinition() == typeof(IParameterReader<>))
		{
			var flatService = services.GetService(readerOrWriterType);
			return flatService is null
				? ResolveContainerReader(services, readerOrWriterType.GenericTypeArguments[0])
				: flatService;
		}
		if (readerOrWriterType.IsGenericType && readerOrWriterType.GetGenericTypeDefinition() == typeof(IParameterWriter<>))
		{
			var flatService = services.GetService(readerOrWriterType);
			return flatService is null
				? ResolveContainerWriter(services, readerOrWriterType.GenericTypeArguments[0])
				: flatService;
		}

		throw new ArgumentException($"Type '{readerOrWriterType.FullName}' must be IParameterReader<> or IParameterWriter<>");
	}

	private static object ResolveContainerReader(IServiceProvider services, Type parameterType)
	{
		if (parameterType.IsArray && parameterType.GetArrayRank() == 1)
		{
			var innerType = parameterType.GetElementType()!;
			var implementationType = typeof(ArrayParameterReader<>).MakeGenericType(innerType);
			return services.GetRequiredService(implementationType);
		}

		if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(List<>))
		{
			var innerType = parameterType.GenericTypeArguments[0];
			var implementationType = typeof(ListParameterReader<>).MakeGenericType(innerType);
			return services.GetRequiredService(implementationType);
		}

		if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			var keyType = parameterType.GenericTypeArguments[0];
			var valueType = parameterType.GenericTypeArguments[1];
			var implementationType = typeof(DictionaryParameterReader<,>).MakeGenericType(keyType, valueType);
			return services.GetRequiredService(implementationType);
		}

		var innerNullableType = Nullable.GetUnderlyingType(parameterType);
		if (innerNullableType is not null)
		{
			var implementationType = typeof(NullableParameterReader<>).MakeGenericType(innerNullableType);
			return services.GetRequiredService(implementationType);
		}

		throw new NotSupportedException($"Unsupported container type '{parameterType.FullName}'");
	}

	private static object ResolveContainerWriter(IServiceProvider services, Type parameterType)
	{
		if (parameterType.IsArray && parameterType.GetArrayRank() == 1)
		{
			var innerType = parameterType.GetElementType()!;
			var implementationType = typeof(ArrayParameterWriter<>).MakeGenericType(innerType);
			return services.GetRequiredService(implementationType);
		}

		if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(List<>))
		{
			var innerType = parameterType.GenericTypeArguments[0];
			var implementationType = typeof(ListParameterWriter<>).MakeGenericType(innerType);
			return services.GetRequiredService(implementationType);
		}

		if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			var keyType = parameterType.GenericTypeArguments[0];
			var valueType = parameterType.GenericTypeArguments[1];
			var implementationType = typeof(DictionaryParameterWriter<,>).MakeGenericType(keyType, valueType);
			return services.GetRequiredService(implementationType);
		}

		var innerNullableType = Nullable.GetUnderlyingType(parameterType);
		if (innerNullableType is not null)
		{
			var implementationType = typeof(NullableParameterWriter<>).MakeGenericType(innerNullableType);
			return services.GetRequiredService(implementationType);
		}

		throw new NotSupportedException($"Unsupported container type '{parameterType.FullName}'");
	}
}
