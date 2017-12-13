using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.Chat.Commands;
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
		const string GoodBatch = "EXIT /B 0";
		const string BadBatch = "EXIT /B 1";

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

			var tempFile = Path.GetTempFileName();
			try
			{
				File.WriteAllText(tempFile, GoodBatch);
				var t2 = tempFile + ".bat";
				File.Move(tempFile, t2);
				tempFile = t2;
				mockIO.Setup(x => x.ResolvePath(It.IsAny<string>())).Returns(tempFile);
				mockIO.Setup(x => x.FileExists(tempFile)).Returns(Task.FromResult(true));

				Assert.IsTrue(aem.HandleEvent("x"));
				File.WriteAllText(tempFile, BadBatch);
				Assert.IsFalse(aem.HandleEvent("x"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
