namespace Sparc.IO;

/// <summary>
/// Reads a single parameter value of type <typeparamref name="T"/> from a <see cref="PayloadReader"/>.
/// </summary>
public interface IParameterReader<T>
{
	/// <summary>
	/// Reads a parameter of type <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// If your payload is delimiter-based, use <see cref="PayloadReader.AvailableSpan"/>
	/// in combination with <see cref="PayloadReader.Advance"/>.
	/// </remarks>
	/// <param name="reader">Forward-only reader to read parameter from.</param>
	/// <returns>The value corresponding to the payload at the current position.</returns>
	T Read(ref PayloadReader reader);
}
