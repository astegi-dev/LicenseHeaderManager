using LicenseHeaderManager.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;

namespace LicenseHeaderManager.Test
{
  [TestFixture]
  internal class JsonBoolConverterTest
  {
    [Test]
    public void Test_Deserialize_True_Is_True ()
    {
      var deserializeObject = JsonConvert.DeserializeObject<bool> ("true", new JsonBoolConverter());
      Assert.That (deserializeObject, Is.True);
    }

    [Test]
    public void Test_Deserialize_False_Is_False ()
    {
      var deserializeObject = JsonConvert.DeserializeObject<bool> ("false", new JsonBoolConverter());
      Assert.That (deserializeObject, Is.False);
    }

    [Test]
    public void Test_Deserialize_Null_Throws ()
    {
      var ex = Assert.Throws<JsonReaderException> (() => JsonConvert.DeserializeObject<bool> ("null", new JsonBoolConverter()));
      Assert.That (ex.Message, Contains.Substring (JsonBoolConverter.NullLiteral));
    }

    [TestCase("\"\"")]
    [TestCase("\"True\"")]
    [TestCase("\"tRuE\"")]
    [TestCase("\"true\"")]
    [TestCase("\"False\"")]
    [TestCase("\"false\"")]
    [TestCase("\"fALsE\"")]
    public void Test_Deserialize_String_Throws(string stringLiteral)
    {
      Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<bool>(stringLiteral, new JsonBoolConverter()));
    }

    [TestCase ("0")]
    [TestCase ("-0")]
    [TestCase ("1")]
    [TestCase ("5")]
    [TestCase ("-1")]
    [TestCase ("-13")]
    public void Test_Deserialize_Number_Throws (string number)
    {
      Assert.Throws<JsonReaderException> (() => JsonConvert.DeserializeObject<bool> (number, new JsonBoolConverter()));
    }

    [Test]
    public void Test_Serialize_True_is_True ()
    {
      var serializeObject = JsonConvert.SerializeObject (true, new JsonBoolConverter());
      Assert.That (serializeObject, Is.EqualTo ("true"));
    }

    [Test]
    public void Test_Serialize_False_is_False ()
    {
      var serializeObject = JsonConvert.SerializeObject (false, new JsonBoolConverter());
      Assert.That (serializeObject, Is.EqualTo ("false"));
    }
  }
}