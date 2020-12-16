using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests
{
	sealed class UsersTest
	{
		readonly IServerClient serverClient;

		public UsersTest(IServerClient serverClient)
		{
			this.serverClient = serverClient ?? throw new ArgumentNullException(nameof(serverClient));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await Task.WhenAll(
				BasicTests(cancellationToken),
				TestCreateSysUser(cancellationToken),
				TestSpamCreation(cancellationToken)).ConfigureAwait(false);
		}

		async Task BasicTests(CancellationToken cancellationToken)
		{
			var user = await serverClient.Users.Read(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(user);
			Assert.AreEqual("Admin", user.Name);
			Assert.IsNull(user.SystemIdentifier);
			Assert.AreEqual(true, user.Enabled);
			Assert.IsNotNull(user.OAuthConnections);
			Assert.IsNotNull(user.PermissionSet);
			Assert.IsNotNull(user.PermissionSet.Id);
			Assert.IsNotNull(user.PermissionSet.InstanceManagerRights);
			Assert.IsNotNull(user.PermissionSet.AdministrationRights);

			var systemUser = user.CreatedBy;
			Assert.IsNotNull(systemUser);
			Assert.AreEqual("TGS", systemUser.Name);
			Assert.AreEqual(false, systemUser.Enabled);

			var users = await serverClient.Users.List(cancellationToken);
			Assert.IsTrue(users.Count > 0);
			Assert.IsFalse(users.Any(x => x.Id == systemUser.Id));

			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => serverClient.Users.GetId(systemUser, cancellationToken), null);
			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => serverClient.Users.Update(new UserUpdate
			{
				Id = systemUser.Id
			}, cancellationToken), null);

			var sampleOAuthConnections = new List<OAuthConnection>
			{
				new OAuthConnection
				{
					ExternalUserId = "asdfasdf",
					Provider = OAuthProvider.Discord
				}
			};
			await ApiAssert.ThrowsException<ApiConflictException>(() => serverClient.Users.Update(new UserUpdate
			{
				Id = user.Id,
				OAuthConnections = sampleOAuthConnections
			}, cancellationToken), ErrorCode.AdminUserCannotOAuth);

			var testUser = await serverClient.Users.Create(
				new UserUpdate
				{
					Name = $"BasicTestUser",
					Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
				},
				cancellationToken).ConfigureAwait(false);

			Assert.IsNotNull(testUser.OAuthConnections);
			testUser = await serverClient.Users.Update(
			   new UserUpdate
			   {
				   Id = testUser.Id,
				   OAuthConnections = sampleOAuthConnections
			   },
			   cancellationToken).ConfigureAwait(false);

			Assert.AreEqual(1, testUser.OAuthConnections.Count);
			Assert.AreEqual(sampleOAuthConnections.First().ExternalUserId, testUser.OAuthConnections.First().ExternalUserId);
			Assert.AreEqual(sampleOAuthConnections.First().Provider, testUser.OAuthConnections.First().Provider);


			var group = await serverClient.Groups.Create(
				new UserGroup
				{
					Name = "TestGroup"
				},
				cancellationToken);
			Assert.AreEqual(group.Name, "TestGroup");
			Assert.IsNotNull(group.PermissionSet);
			Assert.IsNotNull(group.PermissionSet.Id);
			Assert.AreEqual(AdministrationRights.None, group.PermissionSet.AdministrationRights);
			Assert.AreEqual(InstanceManagerRights.None, group.PermissionSet.InstanceManagerRights);

			var group2 = await serverClient.Groups.Create(new UserGroup
			{
				Name = "TestGroup2",
				PermissionSet = new PermissionSet
				{
					InstanceManagerRights = InstanceManagerRights.List
				}
			}, cancellationToken);
			Assert.AreEqual(AdministrationRights.None, group2.PermissionSet.AdministrationRights);
			Assert.AreEqual(InstanceManagerRights.List, group2.PermissionSet.InstanceManagerRights);

			var groups = await serverClient.Groups.List(cancellationToken);
			Assert.AreEqual(2, groups.Count);

			foreach (var igroup in groups)
			{
				Assert.IsNotNull(igroup.Users);
				Assert.IsNotNull(igroup.PermissionSet);
			}

			await serverClient.Groups.Delete(group2, cancellationToken);

			groups = await serverClient.Groups.List(cancellationToken);
			Assert.AreEqual(1, groups.Count);

			group.PermissionSet.InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>();
			group.PermissionSet.AdministrationRights = RightsHelper.AllRights<AdministrationRights>();
			group.Users = null;

			group = await serverClient.Groups.Update(group, cancellationToken);

			Assert.AreEqual(RightsHelper.AllRights<AdministrationRights>(), group.PermissionSet.AdministrationRights);
			Assert.AreEqual(RightsHelper.AllRights<InstanceManagerRights>(), group.PermissionSet.InstanceManagerRights);

			await ApiAssert.ThrowsException<ApiConflictException>(() => serverClient.Groups.Update(group, cancellationToken), ErrorCode.UserGroupControllerCantEditMembers);

			var testUserUpdate = new UserUpdate
			{
				Name = "TestUserWithNoPassword",
				Password = String.Empty
			};

			await ApiAssert.ThrowsException<ApiConflictException>(() => serverClient.Users.Create(testUserUpdate, cancellationToken), ErrorCode.UserPasswordLength);

			testUserUpdate.OAuthConnections = new List<OAuthConnection>
			{
				new OAuthConnection
				{
					ExternalUserId = "asdf",
					Provider = OAuthProvider.GitHub
				}
			};

			var testUser2 = await serverClient.Users.Create(testUserUpdate, cancellationToken);

			testUserUpdate = new UserUpdate
			{
				Id = testUser2.Id,
				PermissionSet = testUser2.PermissionSet,
				Group = new Api.Models.Internal.UserGroup
				{
					Id = group.Id
				},
			};
			await ApiAssert.ThrowsException<ApiConflictException>(
				() => serverClient.Users.Update(
					testUserUpdate,
					cancellationToken),
				ErrorCode.UserGroupAndPermissionSet);

			testUserUpdate.PermissionSet = null;

			testUser2 = await serverClient.Users.Update(testUserUpdate, cancellationToken);

			Assert.IsNull(testUser2.PermissionSet);
			Assert.IsNotNull(testUser2.Group);
			Assert.AreEqual(group.Id, testUser2.Group.Id);

			group = await serverClient.Groups.GetId(group, cancellationToken);
			Assert.IsNotNull(group.Users);
			Assert.AreEqual(1, group.Users.Count);
			Assert.AreEqual(testUser2.Id, group.Users.First().Id);
			Assert.IsNotNull(group.PermissionSet);
		}

		async Task TestCreateSysUser(CancellationToken cancellationToken)
		{
			var sysId = Environment.UserName;
			var update = new UserUpdate
			{
				SystemIdentifier = sysId
			};
			if (new PlatformIdentifier().IsWindows)
				await serverClient.Users.Create(update, cancellationToken);
			else
				await ApiAssert.ThrowsException<MethodNotSupportedException>(() => serverClient.Users.Create(update, cancellationToken), ErrorCode.RequiresPosixSystemIdentity);
		}

		async Task TestSpamCreation(CancellationToken cancellationToken)
		{
			ICollection<Task<User>> tasks = new List<Task<User>>();

			// Careful with this, very easy to overload the thread pool
			const int RepeatCount = 100;

			ThreadPool.GetMaxThreads(out var defaultMaxWorker, out var defaultMaxCompletion);
			ThreadPool.GetMinThreads(out var defaultMinWorker, out var defaultMinCompletion);
			try
			{
				ThreadPool.SetMinThreads(Math.Min(RepeatCount * 4, defaultMaxWorker), Math.Min(RepeatCount * 4, defaultMaxCompletion));
				for (int i = 0; i < RepeatCount; ++i)
				{
					tasks.Add(
						serverClient.Users.Create(
							new UserUpdate
							{
								Name = $"SpamTestUser_{i}",
								Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
							},
							cancellationToken));
				}

				await Task.WhenAll(tasks).ConfigureAwait(false);
			}
			finally
			{
				ThreadPool.SetMinThreads(defaultMinWorker, defaultMinCompletion);
			}

			Assert.AreEqual(RepeatCount, tasks.Select(task => task.Result.Id).Distinct().Count(), "Did not receive expected number of unique user IDs!");
		}
	}
}
