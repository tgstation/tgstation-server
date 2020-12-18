using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests
{
	sealed class UsersTest
	{
		readonly IUsersClient client;

		public UsersTest(IUsersClient client)
		{
			this.client = client ?? throw new ArgumentNullException(nameof(client));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await Task.WhenAll(
				BasicTests(cancellationToken),
				TestCreateSysUser(cancellationToken),
				TestSpamCreation(cancellationToken)).ConfigureAwait(false);

			await TestPagination(cancellationToken);
		}

		async Task BasicTests(CancellationToken cancellationToken)
		{
			var user = await client.Read(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(user);
			Assert.AreEqual("Admin", user.Name);
			Assert.IsNull(user.SystemIdentifier);
			Assert.AreEqual(true, user.Enabled);
			Assert.IsNotNull(user.OAuthConnections);

			var systemUser = user.CreatedBy;
			Assert.IsNotNull(systemUser);
			Assert.AreEqual("TGS", systemUser.Name);
			Assert.AreEqual(false, systemUser.Enabled);

			var users = await client.List(null, cancellationToken);
			Assert.IsTrue(users.Count > 0);
			Assert.IsFalse(users.Any(x => x.Id == systemUser.Id));

			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => client.GetId(systemUser, cancellationToken), null);
			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => client.Update(new UserUpdate
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
			await ApiAssert.ThrowsException<ApiConflictException>(() => client.Update(new UserUpdate
			{
				Id = user.Id,
				OAuthConnections = sampleOAuthConnections
			}, cancellationToken), ErrorCode.AdminUserCannotOAuth);

			var testUser = await client.Create(
				new UserUpdate
				{
					Name = $"BasicTestUser",
					Password = "asdfasdjfhauwiehruiy273894234jhndjkwh"
				},
				cancellationToken).ConfigureAwait(false);

			Assert.IsNotNull(testUser.OAuthConnections);
			testUser = await client.Update(
			   new UserUpdate
			   {
				   Id = testUser.Id,
				   OAuthConnections = sampleOAuthConnections
			   },
			   cancellationToken).ConfigureAwait(false);

			Assert.AreEqual(1, testUser.OAuthConnections.Count);
			Assert.AreEqual(sampleOAuthConnections.First().ExternalUserId, testUser.OAuthConnections.First().ExternalUserId);
			Assert.AreEqual(sampleOAuthConnections.First().Provider, testUser.OAuthConnections.First().Provider);
		}

		async Task TestCreateSysUser(CancellationToken cancellationToken)
		{
			var sysId = Environment.UserName;
			var update = new UserUpdate
			{
				SystemIdentifier = sysId
			};
			if (new PlatformIdentifier().IsWindows)
				await client.Create(update, cancellationToken);
			else
				await ApiAssert.ThrowsException<MethodNotSupportedException>(() => client.Create(update, cancellationToken), ErrorCode.RequiresPosixSystemIdentity);
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
						client.Create(
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

		async Task TestPagination(CancellationToken cancellationToken)
		{
			// we test pagination here b/c it's the only spot we have a decent amount of entities
			var nullSettings = await client.List(null, cancellationToken);
			var emptySettings = await client.List(
				new PaginationSettings
				{
				}, cancellationToken);

			Assert.AreEqual(nullSettings.Count, emptySettings.Count);
			Assert.IsTrue(nullSettings.All(x => emptySettings.SingleOrDefault(y => x.Id == y.Id) != null));

			await ApiAssert.ThrowsException<ApiConflictException>(() => client.List(
				new PaginationSettings
				{
					PageSize = -2143
				}, cancellationToken), ErrorCode.ApiInvalidPageOrPageSize);
			await ApiAssert.ThrowsException<ApiConflictException>(() => client.List(
				new PaginationSettings
				{
					PageSize = Int32.MaxValue
				}, cancellationToken), ErrorCode.ApiPageTooLarge);

			await client.List(
				new PaginationSettings
				{
					PageSize = 50
				},
				cancellationToken);

			var skipped = await client.List(new PaginationSettings
			{
				Offset = 50,
				RetrieveCount = 5
			}, cancellationToken);
			Assert.AreEqual(5, skipped.Count);

			var allAfterSkipped = await client.List(new PaginationSettings
			{
				Offset = 50,
			}, cancellationToken);
			Assert.IsTrue(5 < allAfterSkipped.Count);

			var limited = await client.List(new PaginationSettings
			{
				RetrieveCount = 12,
			}, cancellationToken);
			Assert.AreEqual(12, limited.Count);
		}
	}
}
