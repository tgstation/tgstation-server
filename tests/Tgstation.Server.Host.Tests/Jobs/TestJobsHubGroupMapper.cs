using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils.SignalR;

namespace Tgstation.Server.Host.Tests.Jobs
{
	[TestClass]
	public sealed class TestJobsHubGroupMapper
	{
		[TestMethod]
		public async Task TestGroupMapping()
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			var mockHub = new Mock<IConnectionMappedHubContext<JobsHub, IJobsHub>>();
			var mockDcf = new Mock<IDatabaseContextFactory>();

			
			using var context = Utils.CreateDatabaseContext();
			mockDcf.Setup(x => x.UseContext(It.IsNotNull<Func<IDatabaseContext, ValueTask>>())).Returns<Func<IDatabaseContext, ValueTask>>(func => func(context));

			var mockPs = new PermissionSet
			{
				Id = 23421,
				InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
				AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
			};
			var testIps1 = new InstancePermissionSet
			{
				ByondRights = RightsHelper.AllRights<ByondRights>(),
				ChatBotRights = RightsHelper.AllRights<ChatBotRights>(),
				ConfigurationRights = RightsHelper.AllRights<ConfigurationRights>(),
				DreamDaemonRights = RightsHelper.AllRights<DreamDaemonRights>(),
				DreamMakerRights = RightsHelper.AllRights<DreamMakerRights>(),
				Id = 43892849,
				InstanceId = 348928,
				InstancePermissionSetRights = RightsHelper.AllRights<InstancePermissionSetRights>(),
				RepositoryRights = RightsHelper.AllRights<RepositoryRights>(),
				PermissionSetId = mockPs.Id.Value,
				PermissionSet = mockPs,
			};

			var testIps2 = new InstancePermissionSet
			{
				ByondRights = RightsHelper.AllRights<ByondRights>(),
				ChatBotRights = RightsHelper.AllRights<ChatBotRights>(),
				ConfigurationRights = RightsHelper.AllRights<ConfigurationRights>(),
				DreamDaemonRights = RightsHelper.AllRights<DreamDaemonRights>(),
				DreamMakerRights = RightsHelper.AllRights<DreamMakerRights>(),
				Id = 454354,
				InstanceId = 2234,
				InstancePermissionSetRights = RightsHelper.AllRights<InstancePermissionSetRights>(),
				RepositoryRights = RightsHelper.AllRights<RepositoryRights>(),
				PermissionSetId = mockPs.Id.Value,
				PermissionSet = mockPs,
			};
			context.InstancePermissionSets.Add(testIps1);
			context.InstancePermissionSets.Add(testIps2);

			var cancellationToken = CancellationToken.None;
			await context.SaveChangesAsync(cancellationToken);

			var mockUpdater = new Mock<IJobsHubUpdater>();

			var mapper = new JobsHubGroupMapper(
				mockHub.Object,
				mockDcf.Object,
				mockUpdater.Object,
				loggerFactory.CreateLogger<JobsHubGroupMapper>());

			await mapper.StartAsync(cancellationToken);

			var mockAuthenticationContext = new Mock<IAuthenticationContext>();
			var mockUser = new User
			{
				Id = 2134134,
			};

			mockAuthenticationContext.SetupGet(x => x.User).Returns(mockUser);

			mockAuthenticationContext.SetupGet(x => x.PermissionSet).Returns(mockPs);

			bool ran = false;
			Task Callback(IEnumerable<string> results)
			{
				ran = true;
				Assert.AreEqual(2, results.Count());
				Assert.IsTrue(results.Contains(JobsHub.HubGroupName(testIps1.InstanceId)));
				Assert.IsTrue(results.Contains(JobsHub.HubGroupName(testIps2.InstanceId)));
				return Task.CompletedTask;
			}

			await mockHub.RaiseAsync(x => x.OnConnectionMapGroups += null, mockAuthenticationContext.Object, (Func<IEnumerable<string>, Task>)Callback, cancellationToken);

			Assert.IsTrue(ran);

			await mapper.StopAsync(cancellationToken);
		}
	}
}
