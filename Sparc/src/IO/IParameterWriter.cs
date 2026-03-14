namespace Sparc.IO;

/// <summary>
/// Writes a single parameter value of type <typeparamref name="T"/> into a <see cref="PayloadWriter"/>.
/// </summary>
public interface IParameterWriter<T>
{
	/// <summary>
	/// Writes a parameter of type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">Forward-only writer to write parameter to.</param>
	/// <param name="value">The value to write to the current position.</param>
	void Write(ref PayloadWriter writer, T value);
}
