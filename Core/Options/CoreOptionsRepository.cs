/* Copyright (c) rubicon IT GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Options
{
  public class CoreOptionsRepository
  {
    /// <summary>
    ///   Generates an <see cref="IEnumerable{T}"/> whose generic type argument is <see cref="string"/> that represent all keywords
    ///   in a specifiable string, with each recognized keyword being one entry in the enumerable.
    /// </summary>
    /// <param name="requiredKeywords">
    ///   The string containing keywords to be converted to an enumerable. Keywords are separated by "," and possibly whitespaces.
    /// </param>
    /// <returns>
    ///   Returns an <see cref="IEnumerable{T}"/> whose generic type argument is <see cref="string"/> that represent all keywords
    ///   recognized in <paramref name="requiredKeywords"/>.
    /// </returns>
    public static IEnumerable<string> RequiredKeywordsAsEnumerable(string requiredKeywords)
    {
      return requiredKeywords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(k => k.Trim());
    }

    /// <summary>
    ///   Serializes an <see cref="CoreOptions" /> instance to a file in the file system.
    /// </summary>
    /// <param name="options">The <see cref="CoreOptions" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static async Task SaveAsync(CoreOptions options, string filePath)
    {
      await JsonOptionsManager.SerializeAsync(options, filePath);
    }

    /// <summary>
    ///   Deserializes an <see cref="CoreOptions" /> instance from a file in the file system.
    /// </summary>
    /// <param name="filePath">
    ///   The path to an options file from which a corresponding <see cref="CoreOptions" /> instance
    ///   should be constructed.
    /// </param>
    /// <returns>
    ///   An <see cref="CoreOptions" /> instance that represents to configuration contained in the file specified by
    ///   <paramref name="filePath" />.
    ///   If there were errors upon deserialization, <see langword="null" /> is returned.
    /// </returns>
    public static async Task<CoreOptions> LoadAsync(string filePath)
    {
      return await JsonOptionsManager.DeserializeAsync<CoreOptions>(filePath);
    }

    public static string GetDefaultLicenseHeader()
    {
      var sb = new StringBuilder();
      sb.AppendLine("extensions: designer.cs generated.cs");
      sb.AppendLine("extensions: .cs .cpp .h");
      sb.AppendLine("// Copyright (c) 2011 rubicon IT GmbH");
      sb.AppendLine("extensions: .aspx .ascx");
      sb.AppendLine("<%-- ");
      sb.AppendLine("Copyright (c) 2011 rubicon IT GmbH");
      sb.AppendLine("--%>");
      sb.AppendLine("extensions: .vb");
      sb.AppendLine("'Sample license text.");
      sb.AppendLine("extensions:  .xml .config .xsd");
      sb.AppendLine("<!--");
      sb.AppendLine("Sample license text.");
      sb.Append("-->");
      return sb.ToString();
    }
  }
}
