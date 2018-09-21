using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Api.Models.Tests
{
	[TestClass]
	public sealed class TestIrcConnectionStringBuilder
	{
		[TestMethod]
		public void TestBasicParseAndBuild()
		{
			const string exampleString = "server;1234;nick;1;2;asdf";
			var builder = new IrcConnectionStringBuilder(exampleString);

			Assert.IsTrue(builder.Valid);
			Assert.AreEqual("server", builder.Address);
			Assert.AreEqual((ushort)1234, builder.Port);
			Assert.AreEqual("nick", builder.Nickname);
			Assert.IsTrue(builder.UseSsl.Value);
			Assert.AreEqual(IrcPasswordType.NickServ, builder.PasswordType);
			Assert.AreEqual("asdf", builder.Password);

			Assert.AreEqual(exampleString, builder.ToString());
		}
	}
}
