using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Tests.Instance;

namespace Tgstation.Server.Tests
{
	sealed class InstanceManagerTest
	{
		readonly IInstanceManagerClient instanceManagerClient;
		readonly string testRootPath;

		long counter;

		public InstanceManagerTest(IInstanceManagerClient instanceManagerClient, string testRootPath)
		{
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
			this.testRootPath = testRootPath ?? throw new ArgumentNullException(nameof(testRootPath));

			counter = 0;
		}

		Task<Api.Models.Instance> CreateTestInstance(CancellationToken cancellationToken) => instanceManagerClient.CreateOrAttach(new Api.Models.Instance
		{
			Name = "TestInstance-" + ++counter,
			Path = Path.Combine(testRootPath, Guid.NewGuid().ToString()),
			Online = true
		}, cancellationToken);

		public async Task Run(CancellationToken cancellationToken)
		{
			var firstTest = await CreateTestInstance(cancellationToken).ConfigureAwait(false);
			//instances always start offline
			Assert.AreEqual(false, firstTest.Online);
			//check it exists
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			//cant create instances in existent directories
			var testNonEmpty = Path.Combine(testRootPath, Guid.NewGuid().ToString());
			Directory.CreateDirectory(testNonEmpty);
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.CreateOrAttach(new Api.Models.Instance
			{
				Path = testNonEmpty,
				Name = "NonEmptyTest"
			}, cancellationToken)).ConfigureAwait(false);
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.CreateOrAttach(firstTest, cancellationToken)).ConfigureAwait(false);

			//can't move to existent directories
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.Update(new Api.Models.Instance
			{
				Id = firstTest.Id,
				Path = testNonEmpty
			}, cancellationToken)).ConfigureAwait(false);

			//test basic move
			Directory.Delete(testNonEmpty);
			var initialPath = firstTest.Path;
			firstTest = await instanceManagerClient.Update(new Api.Models.Instance
			{
				Id = firstTest.Id,
				Path = testNonEmpty
			}, cancellationToken).ConfigureAwait(false);

			Assert.IsNotNull(firstTest.MoveJob);

			do
			{
				firstTest = await instanceManagerClient.GetId(firstTest, cancellationToken).ConfigureAwait(false);
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
			} while (firstTest.MoveJob != null);

			
			//online it for real for component tests
			firstTest.Online = true;
			firstTest.ConfigurationType = ConfigurationType.HostWrite;
			firstTest = await instanceManagerClient.Update(firstTest, cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(true, firstTest.Online);
			Assert.AreEqual(ConfigurationType.HostWrite, firstTest.ConfigurationType);

			//can't move online instance
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.Update(new Api.Models.Instance
			{
				Id = firstTest.Id,
				Path = initialPath
			}, cancellationToken)).ConfigureAwait(false);

			var testSuite1 = new InstanceTest(instanceManagerClient.CreateClient(firstTest));
			await testSuite1.RunTests(cancellationToken).ConfigureAwait(false);

			//can't detach online instance
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.Detach(firstTest, cancellationToken)).ConfigureAwait(false);

			firstTest.Online = false;
			firstTest = await instanceManagerClient.Update(firstTest, cancellationToken).ConfigureAwait(false);
			await instanceManagerClient.Detach(firstTest, cancellationToken).ConfigureAwait(false);

			var instanceAttachFileName = (string)typeof(InstanceController).GetField("InstanceAttachFileName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			var attachPath = Path.Combine(firstTest.Path, instanceAttachFileName);
			Assert.IsTrue(File.Exists(attachPath));
			
			//can recreate detached instance
			firstTest = await instanceManagerClient.CreateOrAttach(firstTest, cancellationToken).ConfigureAwait(false);

			//but only if the attach file exists
			await instanceManagerClient.Detach(firstTest, cancellationToken).ConfigureAwait(false);
			File.Delete(attachPath);
			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.CreateOrAttach(firstTest, cancellationToken)).ConfigureAwait(false);
		}
	}
}
