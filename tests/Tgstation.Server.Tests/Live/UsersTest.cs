using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live
{
	sealed class UsersTest
	{
		readonly IRestServerClient serverClient;

		public UsersTest(IRestServerClient serverClient)
		{
			this.serverClient = serverClient ?? throw new ArgumentNullException(nameof(serverClient));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await Task.WhenAll(
				BasicTests(cancellationToken),
				TestCreateSysUser(cancellationToken),
				TestSpamCreation(cancellationToken));

			await TestPagination(cancellationToken);
		}

		async Task BasicTests(CancellationToken cancellationToken)
		{
			var user = await serverClient.Users.Read(cancellationToken);
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

			var users = await serverClient.Users.List(null, cancellationToken);
			Assert.IsTrue(users.Count > 0);
			Assert.IsFalse(users.Any(x => x.Id == systemUser.Id));

			await ApiAssert.ThrowsException<InsufficientPermissionsException, UserResponse>(() => serverClient.Users.GetId(systemUser, cancellationToken));
			await ApiAssert.ThrowsException<InsufficientPermissionsException, UserResponse>(() => serverClient.Users.Update(new UserUpdateRequest
			{
				Id = systemUser.Id
			}, cancellationToken));

			var sampleOAuthConnections = new List<OAuthConnection>
			{
				new OAuthConnection
				{
					ExternalUserId = "asdfasdf",
					Provider = OAuthProvider.Discord
				}
			};
			await ApiAssert.ThrowsException<ApiConflictException, UserResponse>(() => serverClient.Users.Update(new UserUpdateRequest
			{
				Id = user.Id,
				OAuthConnections = sampleOAuthConnections
			}, cancellationToken), ErrorCode.AdminUserCannotOAuth);

			var testUser = await serverClient.Users.Create(
				new UserCreateRequest
				{
					Name = $"BasicTestUser",
					Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
				},
				cancellationToken);

			Assert.IsNotNull(testUser.OAuthConnections);
			testUser = await serverClient.Users.Update(
			   new UserUpdateRequest
			   {
				   Id = testUser.Id,
				   OAuthConnections = sampleOAuthConnections
			   },
			   cancellationToken);

			Assert.AreEqual(1, testUser.OAuthConnections.Count);
			Assert.AreEqual(sampleOAuthConnections.First().ExternalUserId, testUser.OAuthConnections.First().ExternalUserId);
			Assert.AreEqual(sampleOAuthConnections.First().Provider, testUser.OAuthConnections.First().Provider);


			var group = await serverClient.Groups.Create(
				new UserGroupCreateRequest
				{
					Name = "TestGroup"
				},
				cancellationToken);
			Assert.AreEqual(group.Name, "TestGroup");
			Assert.IsNotNull(group.PermissionSet);
			Assert.IsNotNull(group.PermissionSet.Id);
			Assert.AreEqual(AdministrationRights.None, group.PermissionSet.AdministrationRights);
			Assert.AreEqual(InstanceManagerRights.None, group.PermissionSet.InstanceManagerRights);

			var group2 = await serverClient.Groups.Create(new UserGroupCreateRequest
			{
				Name = "TestGroup2",
				PermissionSet = new PermissionSet
				{
					InstanceManagerRights = InstanceManagerRights.List
				}
			}, cancellationToken);
			Assert.AreEqual(AdministrationRights.None, group2.PermissionSet.AdministrationRights);
			Assert.AreEqual(InstanceManagerRights.List, group2.PermissionSet.InstanceManagerRights);

			var groups = await serverClient.Groups.List(null, cancellationToken);
			Assert.AreEqual(2, groups.Count);

			foreach (var igroup in groups)
			{
				Assert.IsNotNull(igroup.Users);
				Assert.IsNotNull(igroup.PermissionSet);
			}

			await serverClient.Groups.Delete(group2, cancellationToken);

			groups = await serverClient.Groups.List(null, cancellationToken);
			Assert.AreEqual(1, groups.Count);

			group = await serverClient.Groups.Update(new UserGroupUpdateRequest
			{
				Id = groups[0].Id,
				PermissionSet = new PermissionSet
				{
					InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
					AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
				}
			}, cancellationToken);

			Assert.AreEqual(RightsHelper.AllRights<AdministrationRights>(), group.PermissionSet.AdministrationRights);
			Assert.AreEqual(RightsHelper.AllRights<InstanceManagerRights>(), group.PermissionSet.InstanceManagerRights);

			UserUpdateRequest testUserUpdate = new UserCreateRequest
			{
				Name = "TestUserWithNoPassword",
				Password = string.Empty
			};

			await ApiAssert.ThrowsException<ApiConflictException, UserResponse>(() => serverClient.Users.Create((UserCreateRequest)testUserUpdate, cancellationToken), ErrorCode.UserPasswordLength);

			testUserUpdate.OAuthConnections = new List<OAuthConnection>
			{
				new OAuthConnection
				{
					ExternalUserId = "asdf",
					Provider = OAuthProvider.GitHub
				}
			};

			var testUser2 = await serverClient.Users.Create((UserCreateRequest)testUserUpdate, cancellationToken);

			testUserUpdate = new UserUpdateRequest
			{
				Id = testUser2.Id,
				PermissionSet = testUser2.PermissionSet,
				Group = new Api.Models.Internal.UserGroup
				{
					Id = group.Id
				},
			};
			await ApiAssert.ThrowsException<ApiConflictException, UserResponse>(
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

			testUserUpdate.Group = null;
			testUserUpdate.PermissionSet = new PermissionSet
			{
				AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
				InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
			};

			testUser2 = await serverClient.Users.Update(testUserUpdate, cancellationToken);
			Assert.IsNull(testUser2.Group);
			Assert.IsNotNull(testUser2.PermissionSet);
		}

		async Task TestCreateSysUser(CancellationToken cancellationToken)
		{
			var sysId = Environment.UserName;
			var update = new UserCreateRequest
			{
				SystemIdentifier = sysId
			};
			if (new PlatformIdentifier().IsWindows)
				await serverClient.Users.Create(update, cancellationToken);
			else
				await ApiAssert.ThrowsException<MethodNotSupportedException, UserResponse>(() => serverClient.Users.Create(update, cancellationToken), ErrorCode.RequiresPosixSystemIdentity);
		}

		async Task TestSpamCreation(CancellationToken cancellationToken)
		{
			// Careful with this, very easy to overload the thread pool
			const int RepeatCount = 100;
			var tasks = new List<ValueTask<UserResponse>>(RepeatCount);

			ThreadPool.GetMaxThreads(out var defaultMaxWorker, out var defaultMaxCompletion);
			ThreadPool.GetMinThreads(out var defaultMinWorker, out var defaultMinCompletion);
			try
			{
				ThreadPool.SetMinThreads(Math.Min(RepeatCount * 4, defaultMaxWorker), Math.Min(RepeatCount * 4, defaultMaxCompletion));
				for (int i = 0; i < RepeatCount; ++i)
				{
					var task =
						serverClient.Users.Create(
							new UserCreateRequest
							{
								Name = $"SpamTestUser_{i}",
								Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
							},
							cancellationToken);
					tasks.Add(task);
				}

				await ValueTaskExtensions.WhenAll(tasks);
			}
			finally
			{
				ThreadPool.SetMinThreads(defaultMinWorker, defaultMinCompletion);
			}

			Assert.AreEqual(RepeatCount, tasks.Select(task => task.Result.Id).Distinct().Count(), "Did not receive expected number of unique user IDs!");
		}

		async Task TestPagination(CancellationToken cancellationToken)
		{
			// we test pagination here b/c it's the only spot we have a decent amount of entities
			var nullSettings = await serverClient.Users.List(null, cancellationToken);
			var emptySettings = await serverClient.Users.List(
				new PaginationSettings
				{
				}, cancellationToken);

			Assert.AreEqual(nullSettings.Count, emptySettings.Count);
			Assert.IsTrue(nullSettings.All(x => emptySettings.SingleOrDefault(y => x.Id == y.Id) != null));

			await ApiAssert.ThrowsException<ApiConflictException, List<UserResponse>>(() => serverClient.Users.List(
				new PaginationSettings
				{
					PageSize = -2143
				}, cancellationToken), ErrorCode.ApiInvalidPageOrPageSize);
			await ApiAssert.ThrowsException<ApiConflictException, List<UserResponse>>(() => serverClient.Users.List(
				new PaginationSettings
				{
					PageSize = int.MaxValue
				}, cancellationToken), ErrorCode.ApiPageTooLarge);

			await serverClient.Users.List(
				new PaginationSettings
				{
					PageSize = 50
				},
				cancellationToken);

			var skipped = await serverClient.Users.List(new PaginationSettings
			{
				Offset = 50,
				RetrieveCount = 5
			}, cancellationToken);
			Assert.AreEqual(5, skipped.Count);

			var allAfterSkipped = await serverClient.Users.List(new PaginationSettings
			{
				Offset = 50,
			}, cancellationToken);
			Assert.IsTrue(5 < allAfterSkipped.Count);

			var limited = await serverClient.Users.List(new PaginationSettings
			{
				RetrieveCount = 12,
			}, cancellationToken);
			Assert.AreEqual(12, limited.Count);
		}
	}
}
