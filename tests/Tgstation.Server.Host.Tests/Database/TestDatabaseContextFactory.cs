using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database.Tests
{
	[TestClass]
	public sealed class TestDatabaseContextFactory
	{
		[TestMethod]
		public void TestConstructionThrows()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new DatabaseContextFactory(null));
			var mockProvider = new Mock<IServiceProvider>();
			mockProvider.Setup(x => x.GetService(typeof(IDatabaseContext))).Verifiable();
			var mockScope = new Mock<IServiceScope>();
			mockScope.SetupGet(x => x.ServiceProvider).Returns(mockProvider.Object).Verifiable();
			var mockScopeFactory = new Mock<IServiceScopeFactory>();
			mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object).Verifiable();
			Assert.ThrowsExactly<InvalidOperationException>(() => new DatabaseContextFactory(mockScopeFactory.Object));
			mockScopeFactory.VerifyAll();
			mockScope.VerifyAll();
			mockProvider.VerifyAll();
		}

		[TestMethod]
		public async Task TestWorks()
		{
			var mockDatabase = new Mock<IDatabaseContext>();
			var mockProvider = new Mock<IServiceProvider>();
			var mockDbo = mockDatabase.Object;
			mockProvider.Setup(x => x.GetService(typeof(IDatabaseContext))).Returns(mockDbo).Verifiable();
			var mockScope = new Mock<IServiceScope>();
			mockScope.SetupGet(x => x.ServiceProvider).Returns(mockProvider.Object).Verifiable();
			var mockScopeFactory = new Mock<IServiceScopeFactory>();
			mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object).Verifiable();

			var factory = new DatabaseContextFactory(mockScopeFactory.Object);

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.UseContext(null).AsTask());

			await factory.UseContext(context =>
			{
				Assert.AreSame(mockDbo, context);
				return ValueTask.CompletedTask;
			});

			mockScopeFactory.VerifyAll();
			mockScope.VerifyAll();
			mockProvider.VerifyAll();
		}
	}
}
