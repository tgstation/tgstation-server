using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Tgstation.Server.Host.Extensions.Tests
{
	[TestClass]
	public sealed class TestServiceCollectionExtensions
	{
		class GoodConfig
		{
			public const string Section = "asdf";
		}

		class BadConfig1
		{
#pragma warning disable IDE0051 // Remove unused private members
			const string Section = "asdf";
#pragma warning restore IDE0051 // Remove unused private members
		}

		class BadConfig2
		{
			public const bool Section = false;
		}

		class BadConfig3
		{
			//nah
		}

		[TestMethod]
		public void TestUseStandardConfig()
		{
			var serviceCollection = new ServiceCollection();
			var mockConfig = new Mock<IConfigurationSection>();
			mockConfig.Setup(x => x.GetSection(It.IsNotNull<string>())).Returns(mockConfig.Object).Verifiable();
			Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.UseStandardConfig<GoodConfig>(null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => serviceCollection.UseStandardConfig<GoodConfig>(null));
			serviceCollection.UseStandardConfig<GoodConfig>(mockConfig.Object);
			Assert.ThrowsExactly<InvalidOperationException>(() => serviceCollection.UseStandardConfig<BadConfig1>(mockConfig.Object));
			Assert.ThrowsExactly<InvalidOperationException>(() => serviceCollection.UseStandardConfig<BadConfig2>(mockConfig.Object));
			Assert.ThrowsExactly<InvalidOperationException>(() => serviceCollection.UseStandardConfig<BadConfig3>(mockConfig.Object));
			mockConfig.Verify();
		}
	}
}
