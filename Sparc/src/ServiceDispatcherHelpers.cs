using Sparc.Exceptions;
using Sparc.IO;
using System.Collections.Concurrent;
using System.Reflection;

namespace Sparc;

internal static class ServiceDispatcherHelpers
{
	internal readonly static ConstructorInfo PayloadReaderConstructor =
		typeof(PayloadReader).GetConstructor([typeof(ReadOnlySpan<byte>)])!;
	internal readonly static PropertyInfo PayloadReaderEndProperty =
		typeof(PayloadReader).GetProperty(nameof(PayloadReader.End))!;
	internal readonly static ConstructorInfo UnconsumedExceptionConstructor =
		typeof(UnconsumedDataException).GetConstructor([typeof(int)])!;
	internal readonly static ConcurrentDictionary<Type, (object, MethodInfo)> ParameterReadersByType = [];

	internal static (object, MethodInfo) GetOrAddParameterReader(Type parameterType, IServiceProvider di)
	{
		if (ParameterReadersByType.TryGetValue(parameterType, out var reader))
		{
			return reader;
		}

		var readerType = typeof(IParameterReader<>).MakeGenericType(parameterType);
		var readerImplementation = di.GetParameterReaderOrWriterService(readerType);
		var readMethod = readerType.GetMethod(nameof(IParameterReader<>.Read))!;
		return ParameterReadersByType[parameterType] = (readerImplementation, readMethod);
	}
}
