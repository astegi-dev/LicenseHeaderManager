using Newtonsoft.Json;
using System;

namespace LicenseHeaderManager.Utils
{
  internal class JsonBoolConverter : JsonConverter
  {
    public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue ((bool) value);
    }

    public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      const string exceptionShortMessage = "Expected valid bool value (\"true\" or \"false\", case-sensitive), but got \"{0}\".";
      const string exceptionMessage = exceptionShortMessage + " Path '{1}', line {2}, position {3}.";
      var valueInFile = reader.Value?.ToString();

      if (valueInFile?.Equals ("true", StringComparison.InvariantCultureIgnoreCase) == true)
        return true;

      if (valueInFile?.Equals ("false", StringComparison.InvariantCultureIgnoreCase) == true)
        return false;
      
      if (reader.TokenType == JsonToken.Null || !(reader is IJsonLineInfo jsonLineInfo && jsonLineInfo.HasLineInfo()))
        throw new JsonReaderException (string.Format (exceptionShortMessage, valueInFile ?? "<null>"));

      throw new JsonReaderException (
          string.Format (exceptionMessage, valueInFile ?? "<null>", reader.Path, jsonLineInfo.LineNumber, jsonLineInfo.LinePosition),
          reader.Path,
          jsonLineInfo.LineNumber,
          jsonLineInfo.LinePosition,
          null);
    }

    public override bool CanConvert (Type objectType)
    {
      return objectType == typeof (bool);
    }
  }
}