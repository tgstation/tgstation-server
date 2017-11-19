using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TGS.Server.Components.Tests
{
	[TestClass]
	public class TestInstance
	{
		[TestMethod]
		public void TestConstruction()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
			{

			}
		}
	}
}
