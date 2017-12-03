using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using TGS.Server.IO;
using TGS.Tests;

namespace TGS.Server.Configuration.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerConfig"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerConfig
	{
		const string goodJSON = "{\"Version\":784,\"InstancePaths\":[\"asdf\"],\"RemoteAccessPort\":38600,\"PythonPath\":\"C:\\\\Python273\"}";

		[TestMethod]
		public void TestLoad()
		{
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.ReadAllText(IOManager.ConcatPath(ServerConfig.DefaultConfigDirectory, "ServerConfig.json"))).Returns(Task.FromResult(goodJSON));
			var config = ServerConfig.Load(mockIO.Object);

			mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Once());
			mockIO.ResetCalls();
			Assert.AreEqual(1, config.InstancePaths.Count);
			Assert.AreEqual("asdf", config.InstancePaths[0]);
			Assert.AreEqual(38600, config.RemoteAccessPort);
			Assert.AreEqual("C:\\Python273", config.PythonPath);
			Assert.AreEqual<ulong>(784, config.Version);

			mockIO.Setup(x => x.ReadAllText(IOManager.ConcatPath(ServerConfig.DefaultConfigDirectory, "ServerConfig.json"))).Returns(Task.FromResult("{"));
			ServerConfig.Load(mockIO.Object);
			mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Exactly(2));
			mockIO.ResetCalls();
			mockIO.Setup(x => x.ReadAllText(IOManager.ConcatPath(ServerConfig.MigrationConfigDirectory, "ServerConfig.json"))).Returns(Task.FromResult(goodJSON));
			config = ServerConfig.Load(mockIO.Object);;
			mockIO.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Exactly(2));
			mockIO.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
			Assert.AreEqual(1, config.InstancePaths.Count);
			Assert.AreEqual("asdf", config.InstancePaths[0]);
			Assert.AreEqual(38600, config.RemoteAccessPort);
			Assert.AreEqual("C:\\Python273", config.PythonPath);
			Assert.AreEqual<ulong>(784, config.Version);
		}

		[TestMethod]
		public void TestSave()
		{
			var mockIO = new Mock<IIOManager>();
			var config = new ServerConfig();
			config.Save(mockIO.Object);
			config.Save(ServerConfig.MigrationConfigDirectory, mockIO.Object);
			mockIO.Verify(x => x.WriteAllText(IOManager.ConcatPath(ServerConfig.DefaultConfigDirectory, "ServerConfig.json"), It.IsAny<string>()), Times.Once());
			mockIO.Verify(x => x.WriteAllText(IOManager.ConcatPath(ServerConfig.MigrationConfigDirectory, "ServerConfig.json"), It.IsAny<string>()), Times.Once());
		}
	}
}
