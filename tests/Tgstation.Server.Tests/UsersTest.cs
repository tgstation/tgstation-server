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
		}

		async Task BasicTests(CancellationToken cancellationToken)
		{
			var user = await client.Read(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(user);
			Assert.AreEqual("Admin", user.Name);
			Assert.IsNull(user.SystemIdentifier);
			Assert.AreEqual(true, user.Enabled);

			var systemUser = user.CreatedBy;
			Assert.IsNotNull(systemUser);
			Assert.AreEqual("TGS", systemUser.Name);
			Assert.AreEqual(false, systemUser.Enabled);

			var users = await client.List(cancellationToken);
			Assert.IsTrue(users.Count > 0);
			Assert.IsFalse(users.Any(x => x.Id == systemUser.Id));

			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => client.GetId(systemUser, cancellationToken), null);
			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => client.Update(new UserUpdate
			{
				Id = systemUser.Id
			}, cancellationToken), null);
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
	}
}
