using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Options
{
  public static class JsonOptionsManager
  {
    public static readonly JsonSerializerOptions SerializerDefaultOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter (JsonNamingPolicy.CamelCase, false) }
        };

    public static async Task<T> DeserializeAsync<T> (string filePath, JsonSerializerOptions serializerOptions = null)
    {
      ValidateTypeParameter (typeof (T));

      try
      {
        using var stream = new FileStream (filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<T> (stream, serializerOptions ?? SerializerDefaultOptions);
      }
      catch (ArgumentNullException ex)
      {
        throw new SerializationException ("File stream for deserializing configuration was not present", ex);
      }
      catch (NotSupportedException ex)
      {
        throw new SerializationException ("At least one JSON converter for deserializing configuration members was not found", ex);
      }
      catch (FileNotFoundException ex)
      {
        throw new SerializationException ("File to deserialize configuration from was not found", ex);
      }
      catch (JsonException ex)
      {
        throw new SerializationException ("The file content is not in a valid format", ex);
      }
      catch (Exception ex)
      {
        throw new SerializationException ("An unspecified error occured while deserializing configuration", ex);
      }
    }

    public static async Task SerializeAsync<T> (T options, string filePath, JsonSerializerOptions serializerOptions = null)
    {
      ValidateTypeParameter (typeof (T));

      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
        using var stream = new FileStream (filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync (stream, options, serializerOptions ?? SerializerDefaultOptions);
      }
      catch (ArgumentNullException ex)
      {
        throw new SerializationException ("File stream for serializing configuration was not present", ex);
      }
      catch (NotSupportedException ex)
      {
        throw new SerializationException ("At least one JSON converter for serializing configuration members was not found", ex);
      }
      catch (Exception ex)
      {
        throw new SerializationException ("An unspecified error occured while serializing configuration", ex);
      }
    }

    private static void ValidateTypeParameter (Type type)
    {
      if (!Attribute.IsDefined (type, typeof (LicenseHeaderManagerOptionsAttribute)))
        throw new ArgumentException ($"Type parameter {nameof(type)} must have attribute {nameof(LicenseHeaderManagerOptionsAttribute)}.");
    }
  }
}