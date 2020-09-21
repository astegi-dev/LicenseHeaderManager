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
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Options;
using NUnit.Framework;

namespace Core.Tests
{
  [TestFixture]
  public class JsonOptionsManagerTest
  {
    private List<string> _paths;
    private CoreOptions _options;

    [SetUp]
    public void Setup()
    {
      _paths = new List<string>();
      _options = new CoreOptions();
    }

    [Test]
    public void Deserialize_InvalidTypeParameter_ThrowsArgumentException()
    {
      Assert.That(async () => await JsonOptionsManager.DeserializeAsync<string>(CreateTestFile()), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task DeserializeAsync_ValidTypeParameter_ReturnsContent()
    {
      var content = await JsonOptionsManager.DeserializeAsync<CoreOptions>(CreateTestFile());
      Assert.That(content, Is.TypeOf(typeof(CoreOptions)));
      Assert.That(content.UseRequiredKeywords, Is.False);
    }

    [Test]
    public void Deserialize_NoFileStream_ThrowsArgumentNullException()
    {
      Assert.That(async () => await JsonOptionsManager.DeserializeAsync<CoreOptions>(null),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_fileStreamNotPresent));
    }

    [Test]
    public void Deserialize_JsonConverterNotFound_ThrowsNotSupportedException()
    {
      Assert.That(async () => await JsonOptionsManager.DeserializeAsync<NotSupportedOptions>(CreateTestFile()),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_jsonConverterNotFound));
    }

    [Test]
    public void Deserialize_NoFile_ThrowsFileNotFoundException()
    {
      Assert.That(async () => await JsonOptionsManager.DeserializeAsync<CoreOptions>(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_fileNotFound));
    }

    [Test]
    public void Deserialize_NotValidFormat_ThrowsJsonException()
    {
      Assert.That(async () => await JsonOptionsManager.DeserializeAsync<CoreOptions>(CreateTestFile("Invalid format text")),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_fileContentFormatNotValid));
    }

    [Test]
    public void Deserialize_EmptyPath_ThrowsException()
    {
      Assert.That(
          async () => await JsonOptionsManager.DeserializeAsync<CoreOptions>(""),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_unspecifiedError));
    }

    [Test]
    public void SerializeAsync_InvalidTypeParameter_ThrowsArgumentException()
    {
      Assert.That(async () => await JsonOptionsManager.SerializeAsync("{\r\n}", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void SerializeAsync_ValidTypeParameter_DoesNotThrowException()
    {
      var testFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
      _paths.Add(testFile);
      Assert.That(async () => await JsonOptionsManager.SerializeAsync(_options, testFile), Throws.Nothing);
    }

    [Test]
    public void SerializeAsync_NoFileStream_ThrowsArgumentNullException()
    {
      //Assert.That(async () => await JsonOptionsManager.SerializeAsync<CoreOptions>(_options, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")),
      //    Throws.InstanceOf<SerializationException>().With.Message.EqualTo("File stream for deserializing configuration was not present"));
    }

    [Test]
    public void SerializeAsync_JsonConverterNotFound_ThrowsNotSupportedException()
    {
      var notSupportedOptions = new NotSupportedOptions();

      Assert.That(async () => await JsonOptionsManager.SerializeAsync<NotSupportedOptions>(notSupportedOptions, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_jsonConverterNotFound));
    }

    [Test]
    public void SerializeAsync_EmptyPath_ThrowsException()
    {
      Assert.That(
          async () => await JsonOptionsManager.SerializeAsync(_options, null),
          Throws.InstanceOf<SerializationException>().With.Message.EqualTo(JsonOptionsManager.c_unspecifiedError));
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var path in _paths)
        File.Delete(path);
    }

    private string CreateTestFile(string text = null)
    {
      var testFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
      _paths.Add(testFile);

      using (var fs = File.Create(testFile))
      {
        if (text == null)
          text = "{\r\n\"useRequiredKeywords\": false\r\n}";

        var content = Encoding.UTF8.GetBytes(text);
        fs.Write(content, 0, content.Length);
      }
      return testFile;
    }
  }
}
