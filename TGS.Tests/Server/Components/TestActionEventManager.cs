using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using System.Threading.Tasks;
using TGS.Server.IO;
using TGS.Server.Logging;
using TGS.Tests;

namespace TGS.Server.Components.Tests
{
	/// <summary>
	/// Tests for <see cref="ActionEventManager"/>
	/// </summary>
	[TestClass]
	public sealed class TestActionEventManager
	{
		[TestMethod]
		public void TestInstatiation()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
			new ActionEventManager(mockLogger.Object, mockIO.Object);
			mockIO.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Once());
		}

		[TestMethod]
		public void TestHandleEvent()
		{
			var mockLogger = new Mock<IInstanceLogger>();
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.CreateDirectory(It.IsAny<string>())).Returns(Task.FromResult(new DirectoryInfo(".")));
			var aem = new ActionEventManager(mockLogger.Object, mockIO.Object);

			Assert.IsTrue(aem.HandleEvent("x"));

			using (var t = new TestProcessPath())
			{
				mockIO.Setup(x => x.ResolvePath(It.IsAny<string>())).Returns(t.Path);
				mockIO.Setup(x => x.FileExists(t.Path)).Returns(Task.FromResult(true));

				t.ExitCode = 0;
				Assert.IsTrue(aem.HandleEvent("x"));
				t.ExitCode = 1;
				Assert.IsFalse(aem.HandleEvent("x"));
			}
		}
	}
}
