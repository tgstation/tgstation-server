using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tgstation.Server.Api;
using YamlDotNet.Serialization;

namespace Tgstation.Server.Host.Extensions.Converters.Tests
{
	[TestClass]
	public sealed class TestVersionConverter
	{
		class TestObject
		{
			public Version Version { get; set; }
		}

		readonly Version testVersion;
		readonly string testYaml;

		public TestVersionConverter()
		{
			testVersion = new Version(1, 2, 3);
			testYaml = $@"{nameof(TestObject.Version)}: {testVersion.Semver()}";
		}

		[TestMethod]
		public void TestYamlSerialization()
		{
			var testObj = new TestObject
			{
				Version = testVersion
			};

			var serializedString = new SerializerBuilder()
				.WithTypeConverter(new VersionConverter())
				.Build()
				.Serialize(testObj);

			Assert.AreEqual(testYaml, serializedString.Trim());
		}
	}
}
