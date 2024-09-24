using Elastic.CommonSchema;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StrawberryShake;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.GraphQL;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live
{
	sealed class UsersTest
	{
		readonly IMultiServerClient serverClient;

		public UsersTest(IMultiServerClient serverClient)
		{
			this.serverClient = serverClient ?? throw new ArgumentNullException(nameof(serverClient));
		}

		public async ValueTask Run(CancellationToken cancellationToken)
		{
			await ValueTaskExtensions.WhenAll(
				BasicTests(cancellationToken),
				TestCreateSysUser(cancellationToken),
				TestSpamCreation(cancellationToken));

			await TestPagination(cancellationToken);
		}

		async ValueTask BasicTests(CancellationToken cancellationToken)
		{
			var (restUser, gqlUser) = await serverClient.ExecuteReadOnlyConfirmEquivalence(
				client => client.Users.Read(cancellationToken),
				client => client.ReadCurrentUser.ExecuteAsync(cancellationToken),
				(restResult, graphQLResult) =>
				{
					var gqlUser = graphQLResult.Swarm.Users.Current;
					return restResult.Enabled == gqlUser.Enabled
						&& restResult.Name == gqlUser.Name
						&& (restResult.CreatedAt.Value.Ticks / 1000000) == (gqlUser.CreatedAt.Ticks / 1000000)
						&& restResult.SystemIdentifier == gqlUser.SystemIdentifier
						&& restResult.CreatedBy.Name == gqlUser.CreatedBy.Name;
				},
				cancellationToken);

			Assert.IsNotNull(restUser);
			Assert.AreEqual("Admin", restUser.Name);
			Assert.IsNull(restUser.SystemIdentifier);
			Assert.AreEqual(true, restUser.Enabled);
			Assert.IsNotNull(restUser.OAuthConnections);
			Assert.IsNotNull(restUser.PermissionSet);
			Assert.IsNotNull(restUser.PermissionSet.Id);
			Assert.IsNotNull(restUser.PermissionSet.InstanceManagerRights);
			Assert.IsNotNull(restUser.PermissionSet.AdministrationRights);

			var systemUser = restUser.CreatedBy;
			Assert.IsNotNull(systemUser);
			Assert.AreEqual("TGS", systemUser.Name);

			await serverClient.Execute(
				async client =>
				{
					var users = await client.Users.List(null, cancellationToken);
					Assert.IsTrue(users.Count > 0);
					Assert.IsFalse(users.Any(x => x.Id == systemUser.Id));

					await ApiAssert.ThrowsException<InsufficientPermissionsException, UserResponse>(() => client.Users.GetId(systemUser, cancellationToken));
					await ApiAssert.ThrowsException<InsufficientPermissionsException, UserResponse>(() => client.Users.Update(new UserUpdateRequest
					{
						Id = systemUser.Id
					}, cancellationToken));

					var sampleOAuthConnections = new List<OAuthConnection>
					{
						new()
						{
							ExternalUserId = "asdfasdf",
							Provider = Api.Models.OAuthProvider.Discord
						}
					};

					await ApiAssert.ThrowsException<ApiConflictException, UserResponse>(() => client.Users.Update(new UserUpdateRequest
					{
						Id = restUser.Id,
						OAuthConnections = sampleOAuthConnections
					}, cancellationToken), Api.Models.ErrorCode.AdminUserCannotOAuth);

					var testUser = await client.Users.Create(
						new UserCreateRequest
						{
							Name = $"BasicTestUser",
							Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
						},
						cancellationToken);

					Assert.IsNotNull(testUser.OAuthConnections);
					testUser = await client.Users.Update(
					   new UserUpdateRequest
					   {
						   Id = testUser.Id,
						   OAuthConnections = sampleOAuthConnections
					   },
					   cancellationToken);

					Assert.AreEqual(1, testUser.OAuthConnections.Count);
					Assert.AreEqual(sampleOAuthConnections.First().ExternalUserId, testUser.OAuthConnections.First().ExternalUserId);
					Assert.AreEqual(sampleOAuthConnections.First().Provider, testUser.OAuthConnections.First().Provider);

					var group = await client.Groups.Create(
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

					var group2 = await client.Groups.Create(new UserGroupCreateRequest
					{
						Name = "TestGroup2",
						PermissionSet = new PermissionSet
						{
							InstanceManagerRights = InstanceManagerRights.List
						}
					}, cancellationToken);
					Assert.AreEqual(AdministrationRights.None, group2.PermissionSet.AdministrationRights);
					Assert.AreEqual(InstanceManagerRights.List, group2.PermissionSet.InstanceManagerRights);

					var groups = await client.Groups.List(null, cancellationToken);
					Assert.AreEqual(2, groups.Count);

					foreach (var igroup in groups)
					{
						Assert.IsNotNull(igroup.Users);
						Assert.IsNotNull(igroup.PermissionSet);
					}

					await client.Groups.Delete(group2, cancellationToken);

					groups = await client.Groups.List(null, cancellationToken);
					Assert.AreEqual(1, groups.Count);

					group = await client.Groups.Update(new UserGroupUpdateRequest
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

					await ApiAssert.ThrowsException<ApiConflictException, UserResponse>(() => client.Users.Create((UserCreateRequest)testUserUpdate, cancellationToken), Api.Models.ErrorCode.UserPasswordLength);

					testUserUpdate.OAuthConnections =
					[
						new()
						{
							ExternalUserId = "asdf",
							Provider = Api.Models.OAuthProvider.GitHub
						}
					];

					var testUser2 = await client.Users.Create((UserCreateRequest)testUserUpdate, cancellationToken);

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
						() => client.Users.Update(
							testUserUpdate,
							cancellationToken),
						Api.Models.ErrorCode.UserGroupAndPermissionSet);

					testUserUpdate.PermissionSet = null;

					testUser2 = await client.Users.Update(testUserUpdate, cancellationToken);

					Assert.IsNull(testUser2.PermissionSet);
					Assert.IsNotNull(testUser2.Group);
					Assert.AreEqual(group.Id, testUser2.Group.Id);

					var group4 = await client.Groups.GetId(group, cancellationToken);
					Assert.IsNotNull(group4.Users);
					Assert.AreEqual(1, group4.Users.Count);
					Assert.AreEqual(testUser2.Id, group4.Users.First().Id);
					Assert.IsNotNull(group4.PermissionSet);

					testUserUpdate.Group = null;
					testUserUpdate.PermissionSet = new PermissionSet
					{
						AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
						InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
					};

					testUser2 = await client.Users.Update(testUserUpdate, cancellationToken);
					Assert.IsNull(testUser2.Group);
					Assert.IsNotNull(testUser2.PermissionSet);
				},
				async client =>
				{
					var result = await client.RunOperation(gql => gql.ListUsers.ExecuteAsync(cancellationToken), cancellationToken);
					result.EnsureNoErrors();
					var users = result.Data.Swarm.Users.QueryableUsers;
					Assert.IsTrue(users.TotalCount > 0);
					Assert.AreEqual(Math.Min(ApiController.DefaultPageSize, users.TotalCount), users.Nodes.Count);

					var tgsUserResult = await client.RunOperation(gql => gql.GetUserNameByNodeId.ExecuteAsync(gqlUser.Swarm.Users.Current.CreatedBy.Id, cancellationToken), cancellationToken);
					tgsUserResult.EnsureNoErrors();
					var tgsUserResult2 = await client.RunOperation(gql => gql.GetUserById.ExecuteAsync(gqlUser.Swarm.Users.Current.CreatedBy.Id, cancellationToken), cancellationToken);
					Assert.IsTrue(tgsUserResult2.IsErrorResult());

					var sampleOAuthConnections = new List<OAuthConnectionInput>
					{
						new()
						{
							ExternalUserId = "asdfasdf",
							Provider = Client.GraphQL.OAuthProvider.Discord,
						}
					};

					await ApiAssert.OperationFails(
						client,
						gql => gql.SetUserOAuthConnections.ExecuteAsync(
							gqlUser.Swarm.Users.Current.Id,
							sampleOAuthConnections,
							cancellationToken),
						data => data.UpdateUser,
						Client.GraphQL.ErrorCode.AdminUserCannotOAuth,
						cancellationToken);

					var testUserResult = await client.RunMutationEnsureNoErrors(
						gql => gql.CreateUserWithPasswordSelectOAuthConnections.ExecuteAsync("BasicTestUser", "asdfasdjfhauwiehruiy273894234jhndjkwh", cancellationToken),
						data => data.CreateUserByPasswordAndPermissionSet,
						cancellationToken);

					var testUserResult2 = await client.RunMutationEnsureNoErrors(
						gql => gql.UpdateUserOAuthConnections.ExecuteAsync(
							testUserResult.User.Id,
							sampleOAuthConnections,
							cancellationToken),
						data => data.UpdateUser,
						cancellationToken);

					var testUser = testUserResult2.User;
					Assert.IsNotNull(testUser.OAuthConnections);
					Assert.AreEqual(1, testUser.OAuthConnections.Count);
					Assert.AreEqual(sampleOAuthConnections.First().ExternalUserId, testUser.OAuthConnections[0].ExternalUserId);
					Assert.AreEqual(sampleOAuthConnections.First().Provider, testUser.OAuthConnections[0].Provider);

					var groupResult = await client.RunMutationEnsureNoErrors(
						gql => gql.CreateUserGroup.ExecuteAsync("TestGroup", cancellationToken),
						data => data.CreateUserGroup,
						cancellationToken);

					var group = groupResult.UserGroup;
					Assert.AreEqual(group.Name, "TestGroup");
					Assert.IsNotNull(group.PermissionSet);

					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanSetOnline);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanRelocate);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanRename);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanSetConfiguration);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanRead);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanCreate);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanDelete);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanGrantPermissions);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanList);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanSetAutoUpdate);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanSetChatBotLimit);
					Assert.IsFalse(group.PermissionSet.InstanceManagerRights.CanSetConfiguration);

					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanChangeVersion);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanDownloadLogs);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanEditOwnOAuthConnections);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanEditOwnPassword);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanReadUsers);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanRestartHost);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanUploadVersion);
					Assert.IsFalse(group.PermissionSet.AdministrationRights.CanWriteUsers);

					var group2Result = await client.RunMutationEnsureNoErrors(
						gql => gql.CreateUserGroupWithInstanceListPerm.ExecuteAsync("TestGroup2", cancellationToken),
						data => data.CreateUserGroup,
						cancellationToken);

					var group2 = group2Result.UserGroup;

					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanSetOnline);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanRelocate);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanRename);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanSetConfiguration);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanRead);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanCreate);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanDelete);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanGrantPermissions);
					Assert.IsTrue(group2.PermissionSet.InstanceManagerRights.CanList);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanSetAutoUpdate);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanSetChatBotLimit);
					Assert.IsFalse(group2.PermissionSet.InstanceManagerRights.CanSetConfiguration);

					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanChangeVersion);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanDownloadLogs);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanEditOwnOAuthConnections);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanEditOwnPassword);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanReadUsers);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanRestartHost);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanUploadVersion);
					Assert.IsFalse(group2.PermissionSet.AdministrationRights.CanWriteUsers);

					var groupsResult = await client.RunQueryEnsureNoErrors(
						gql => gql.ListUserGroups.ExecuteAsync(cancellationToken),
						cancellationToken);

					var groups = groupsResult.Swarm.Users.Groups.QueryableGroups;
					Assert.AreEqual(2, groups.TotalCount);

					foreach (var igroup in groups.Nodes)
						Assert.IsNotNull(igroup.Id);

					var deleteResult = await client.RunMutationEnsureNoErrors(
						gql => gql.DeleteUserGroup.ExecuteAsync(group2.Id, cancellationToken),
						data => data.DeleteEmptyUserGroup,
						cancellationToken);

					groupsResult = await client.RunQueryEnsureNoErrors(
						gql => gql.ListUserGroups.ExecuteAsync(cancellationToken),
						cancellationToken);

					groups = groupsResult.Swarm.Users.Groups.QueryableGroups;
					Assert.AreEqual(1, groups.TotalCount);

					foreach (var igroup in groups.Nodes)
						Assert.IsNotNull(igroup.Id);

					var group3Result = await client.RunMutationEnsureNoErrors(
						gql => gql.SetFullPermsOnUserGroup.ExecuteAsync(group.Id, cancellationToken),
						data => data.UpdateUserGroup,
						cancellationToken);

					var group3 = group3Result.UserGroup;
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanSetOnline);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanRelocate);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanRename);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanSetConfiguration);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanRead);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanCreate);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanDelete);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanGrantPermissions);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanList);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanSetAutoUpdate);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanSetChatBotLimit);
					Assert.IsTrue(group3.PermissionSet.InstanceManagerRights.CanSetConfiguration);

					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanChangeVersion);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanDownloadLogs);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanEditOwnOAuthConnections);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanEditOwnPassword);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanReadUsers);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanRestartHost);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanUploadVersion);
					Assert.IsTrue(group3.PermissionSet.AdministrationRights.CanWriteUsers);

					await ApiAssert.OperationFails(
						client,
						gql => gql.CreateUserWithPassword.ExecuteAsync("TestUserWithNoPassword", String.Empty, cancellationToken),
						data => data.CreateUserByPasswordAndPermissionSet,
						Client.GraphQL.ErrorCode.ModelValidationFailure,
						cancellationToken);

					await ApiAssert.OperationFails(
						client,
						gql => gql.CreateUserWithPassword.ExecuteAsync("TestUserWithShortPassword", "a", cancellationToken),
						data => data.CreateUserByPasswordAndPermissionSet,
						Client.GraphQL.ErrorCode.UserPasswordLength,
						cancellationToken);

					var oAuthCreateResult = await client.RunMutationEnsureNoErrors(
						gql => gql.CreateUserFromOAuthConnection.ExecuteAsync(
							"TestUserWithNoPassword",
							[
								new()
								{
									ExternalUserId = "asdf",
									Provider = Client.GraphQL.OAuthProvider.GitHub,
								}
							],
							cancellationToken),
						data => data.CreateUserByOAuthAndPermissionSet,
						cancellationToken);

					var testUser2 = oAuthCreateResult.User;

					var testUser22Result = await client.RunMutationEnsureNoErrors(
						gql => gql.SetUserGroup.ExecuteAsync(testUser2.Id, group.Id, cancellationToken),
						data => data.UpdateUserSetGroup,
						cancellationToken);

					var testUser22 = testUser22Result.User;

					Assert.IsNull(testUser22.OwnedPermissionSet);
					Assert.IsNotNull(testUser22.Group);
					Assert.AreEqual(group.Id, testUser22.Group.Id);

					var group4Result = await client.RunQueryEnsureNoErrors(
						gql => gql.GetSomeGroupInfo.ExecuteAsync(group.Id, cancellationToken),
						cancellationToken);
					var group4 = group4Result.Swarm.Users.Groups.ById;

					Assert.IsNotNull(group4.QueryableUsersByGroup.Nodes);
					Assert.AreEqual(1, group4.QueryableUsersByGroup.TotalCount);
					Assert.AreEqual(testUser2.Id, group4.QueryableUsersByGroup.Nodes[0].Id);
					Assert.IsNotNull(group4.PermissionSet);

					var testUser4Result = await client.RunMutationEnsureNoErrors(
						gql => gql.SetUserPermissionSet.ExecuteAsync(
							testUser2.Id,
							new PermissionSetInput
							{
								AdministrationRights = new AdministrationRightsFlagsInput
								{
									CanChangeVersion = true,
									CanDownloadLogs = true,
									CanEditOwnOAuthConnections = true,
									CanEditOwnPassword = true,
									CanReadUsers = true,
									CanRestartHost = true,
									CanUploadVersion = true,
									CanWriteUsers = true,
								},
								InstanceManagerRights = new InstanceManagerRightsFlagsInput
								{
									CanCreate = true,
									CanDelete = true,
									CanGrantPermissions = true,
									CanList = true,
									CanRead = true,
									CanRelocate = true,
									CanRename = true,
									CanSetAutoUpdate = true,
									CanSetChatBotLimit = true,
									CanSetConfiguration = true,
									CanSetOnline = true,
								}
							},
							cancellationToken),
						data => data.UpdateUserSetOwnedPermissionSet,
						cancellationToken);

					var testUser4 = testUser4Result.User;
					Assert.IsNull(testUser4.Group);
					Assert.IsNotNull(testUser4.OwnedPermissionSet);
				});
		}

		ValueTask TestCreateSysUser(CancellationToken cancellationToken)
		{
			var sysId = Environment.UserName;

			return serverClient.Execute(
				async restClient =>
				{
					var update = new UserCreateRequest
					{
						SystemIdentifier = sysId
					};
					if (new PlatformIdentifier().IsWindows)
						await restClient.Users.Create(update, cancellationToken);
					else
						await ApiAssert.ThrowsException<MethodNotSupportedException, UserResponse>(() => restClient.Users.Create(update, cancellationToken), Api.Models.ErrorCode.RequiresPosixSystemIdentity);
				},
				async graphQLClient =>
				{
					if (new PlatformIdentifier().IsWindows)
					{
						await graphQLClient.RunMutationEnsureNoErrors(
							gql => gql.CreateSystemUserWithPermissionSet.ExecuteAsync(sysId, cancellationToken),
							data => data.CreateUserBySystemIDAndPermissionSet,
							cancellationToken);
					}
					else
						await ApiAssert.OperationFails(
							graphQLClient,
							gql => gql.CreateSystemUserWithPermissionSet.ExecuteAsync(sysId, cancellationToken),
							data => data.CreateUserBySystemIDAndPermissionSet,
							Client.GraphQL.ErrorCode.RequiresPosixSystemIdentity,
							cancellationToken);
				});
		}

		async ValueTask TestSpamCreation(CancellationToken cancellationToken)
		{
			// Careful with this, very easy to overload the thread pool
			const int RepeatCount = 100;
			var tasks = new List<ValueTask>(RepeatCount);

			ThreadPool.GetMaxThreads(out var defaultMaxWorker, out var defaultMaxCompletion);
			ThreadPool.GetMinThreads(out var defaultMinWorker, out var defaultMinCompletion);
			ConcurrentBag<object> ids = new ConcurrentBag<object>();
			try
			{
				ThreadPool.SetMinThreads(Math.Min(RepeatCount * 4, defaultMaxWorker), Math.Min(RepeatCount * 4, defaultMaxCompletion));
				for (int i = 0; i < RepeatCount; ++i)
				{
					var iLocal = i;
					ValueTask CreateSpamUser()
						=> serverClient.Execute(
							async restClient =>
							{
								var user = await restClient.Users.Create(
									new UserCreateRequest
									{
										Name = $"SpamTestUser_{iLocal}",
										Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
									},
									cancellationToken);

								ids.Add(user.Id);
							},
							async graphQLClient =>
							{
								var result = await graphQLClient.RunMutationEnsureNoErrors(
									gql => gql.CreateUserWithPassword.ExecuteAsync($"SpamTestUser_{iLocal}", "asdfasdjfhauwiehruiy273894234jhndjkwh", cancellationToken),
									data => data.CreateUserByPasswordAndPermissionSet,
									cancellationToken);

								ids.Add(result.User.Id);
							});

					tasks.Add(CreateSpamUser());
				}

				await ValueTaskExtensions.WhenAll(tasks);
			}
			finally
			{
				ThreadPool.SetMinThreads(defaultMinWorker, defaultMinCompletion);
			}

			Assert.AreEqual(RepeatCount, ids.Distinct().Count(), "Did not receive expected number of unique user IDs!");
		}

		ValueTask TestPagination(CancellationToken cancellationToken)
		{
			var expectedCount = new PlatformIdentifier().IsWindows ? 106 : 105; // system user
			return serverClient.Execute(
				async restClient =>
				{
					// we test pagination here b/c it's the only spot we have a decent amount of entities
					var nullSettings = await restClient.Users.List(null, cancellationToken);
					var emptySettings = await restClient.Users.List(
						new PaginationSettings
						{
						}, cancellationToken);

					Assert.AreEqual(nullSettings.Count, emptySettings.Count);
					Assert.AreEqual(expectedCount, nullSettings.Count);
					Assert.IsTrue(nullSettings.All(x => emptySettings.SingleOrDefault(y => x.Id == y.Id) != null));

					await ApiAssert.ThrowsException<ApiConflictException, List<UserResponse>>(() => restClient.Users.List(
						new PaginationSettings
						{
							PageSize = -2143
						}, cancellationToken), Api.Models.ErrorCode.ApiInvalidPageOrPageSize);
					await ApiAssert.ThrowsException<ApiConflictException, List<UserResponse>>(() => restClient.Users.List(
						new PaginationSettings
						{
							PageSize = ApiController.MaximumPageSize + 1,
						}, cancellationToken), Api.Models.ErrorCode.ApiPageTooLarge);

					var users = await restClient.Users.List(
						new PaginationSettings
						{
							PageSize = ApiController.MaximumPageSize,
							RetrieveCount = ApiController.MaximumPageSize,
						},
						cancellationToken);

					Assert.AreEqual(ApiController.MaximumPageSize, users.Count);

					var skipped = await restClient.Users.List(new PaginationSettings
					{
						Offset = 50,
						RetrieveCount = 5
					}, cancellationToken);
					Assert.AreEqual(5, skipped.Count);

					var allAfterSkipped = await restClient.Users.List(new PaginationSettings
					{
						Offset = 50,
					}, cancellationToken);
					Assert.IsTrue(5 < allAfterSkipped.Count);

					var limited = await restClient.Users.List(new PaginationSettings
					{
						RetrieveCount = 12,
					}, cancellationToken);
					Assert.AreEqual(12, limited.Count);
				},
				graphQLClient =>
				{
					async ValueTask TestPageSize(int? inputPageSize)
					{
						var outputPageSize = inputPageSize ?? ApiController.DefaultPageSize;

						string cursor = null;

						var exactMatch = (expectedCount % outputPageSize) == 0;
						var expectedIterations = (expectedCount / outputPageSize) + (exactMatch ? 0 : 1);
						for (int i = 0; i < expectedIterations; ++i)
						{
							var isLastIteration = i == expectedIterations - 1;
							var queryable = (await graphQLClient.RunQueryEnsureNoErrors(
								gql => gql.PageUserIds.ExecuteAsync(inputPageSize, cursor, cancellationToken),
								cancellationToken))
								.Swarm
								.Users
								.QueryableUsers;

							if (!isLastIteration || exactMatch)
								Assert.AreEqual(outputPageSize, queryable.Nodes.Count);
							else
								Assert.AreEqual(expectedCount % outputPageSize, queryable.Nodes.Count);
							Assert.AreEqual(!isLastIteration, queryable.PageInfo.HasNextPage);
							Assert.IsNotNull(queryable.PageInfo.EndCursor);

							cursor = queryable.PageInfo.EndCursor;
						}
					}

					async ValueTask TestBadPageSize(int size)
					{
						var result = await graphQLClient.RunOperation(
							gql => gql.PageUserIds.ExecuteAsync(size, null, cancellationToken),
							cancellationToken);

						if (size == 0)
						{
							// special case
							result.EnsureNoErrors();
							Assert.AreEqual(0, result.Data.Swarm.Users.QueryableUsers.Nodes.Count);
							return;
						}

						var errored = result.IsErrorResult();
						Assert.IsTrue(errored);
					}

					return ValueTaskExtensions.WhenAll(
						TestPageSize(null),
						TestPageSize(ApiController.DefaultPageSize),
						TestPageSize(ApiController.DefaultPageSize / 2),
						TestPageSize(ApiController.MaximumPageSize),
						TestPageSize(1),
						TestBadPageSize(-1),
						TestBadPageSize(0),
						TestBadPageSize(ApiController.MaximumPageSize + 1),
						TestBadPageSize(Int32.MinValue),
						TestBadPageSize(Int32.MaxValue));
				});
		}
	}
}
