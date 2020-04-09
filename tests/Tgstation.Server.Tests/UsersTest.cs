using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;

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
			await TestRetrieveCurrentUser(cancellationToken).ConfigureAwait(false);
			await TestSpamCreation(cancellationToken).ConfigureAwait(false);
		}
		
		async Task TestRetrieveCurrentUser(CancellationToken cancellationToken)
		{
			var user = await this.client.Read(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(user);
			Assert.AreEqual("Admin", user.Name);
			Assert.IsNull(user.SystemIdentifier);
			Assert.AreEqual(true, user.Enabled);
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
