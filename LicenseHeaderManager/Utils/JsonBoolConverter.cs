using System;
using Newtonsoft.Json;

namespace LicenseHeaderManager.Utils
{
  /// <summary>
  ///   Converts boolean literals found in JSON files to a <see cref="bool" /> value and vice versa.
  /// </summary>
  /// <remarks>
  ///   Newtonsoft.Json's default boolean converter is not very strict, i. e. it allows string literals like "TruE" or number
  ///   literals
  ///   like 0, 3, 4 found in a file to be converted to a <see cref="bool" /> value. This might be convenient in some
  ///   situations, but for this
  ///   project, a notion that is more oriented towards the JSON specification is pursued. Consequently, this this class only
  ///   allows the
  ///   <see cref="bool" /> literals true and false, without surrounding quotes and case-sensitive.
  /// </remarks>
  internal class JsonBoolConverter : JsonConverter
  {
    public const string NullLiteral = "{null}";

    public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue ((bool) value);
    }

    public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      // disallow null
      if (reader.TokenType == JsonToken.Null || reader.Value == null)
        throw BuildException (reader, NullLiteral);

      // allow valid boolean values
      if (reader.TokenType == JsonToken.Boolean)
        return reader.Value;

      // disallow anything else (e. g. number literals, string literals like "true")
      throw BuildException (reader, reader.Value.ToString());
    }

    private JsonReaderException BuildException (JsonReader reader, string literal)
    {
      const string exceptionShortMessage = "Expected valid bool literal (true or false, without quotes and case-sensitive), but got \"{0}\".";
      const string exceptionMessage = exceptionShortMessage + " Path '{1}', line {2}, position {3}.";

      if (reader is IJsonLineInfo jsonLineInfo && jsonLineInfo.HasLineInfo())
        return new JsonReaderException (
            string.Format (exceptionMessage, literal, reader.Path, jsonLineInfo.LineNumber, jsonLineInfo.LinePosition),
            reader.Path,
            jsonLineInfo.LineNumber,
            jsonLineInfo.LinePosition,
            null);

      return new JsonReaderException (string.Format (exceptionShortMessage, literal));
    }

    public override bool CanConvert (Type objectType)
    {
      return objectType == typeof (bool);
    }
  }
}