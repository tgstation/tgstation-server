using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Host.Controllers;

namespace Tgstation.Server.Tests
{
	sealed class InstanceManagerTest
	{
		public const string TestInstanceName = "IntegrationTestInstance";

		readonly IInstanceManagerClient instanceManagerClient;
		readonly IUsersClient usersClient;
		readonly string testRootPath;

		public InstanceManagerTest(IInstanceManagerClient instanceManagerClient, IUsersClient usersClient, string testRootPath)
		{
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
			this.usersClient = usersClient ?? throw new ArgumentNullException(nameof(usersClient));
			this.testRootPath = testRootPath ?? throw new ArgumentNullException(nameof(testRootPath));
		}

		public Task<Api.Models.InstanceResponse> CreateTestInstance(CancellationToken cancellationToken) => instanceManagerClient.CreateOrAttach(new InstanceCreateRequest
		{
			Name = TestInstanceName,
			Path = Path.Combine(testRootPath, Guid.NewGuid().ToString()),
			Online = true,
			ChatBotLimit = 2
		}, cancellationToken);

		static InstanceCreateRequest FromResponse(InstanceResponse response) => new InstanceCreateRequest
		{
			Id = response.Id,
			Path = response.Path,
			AutoUpdateInterval = response.AutoUpdateInterval,
			ChatBotLimit = response.ChatBotLimit,
			ConfigurationType = response.ConfigurationType,
			Name = response.Name,
			Online = response.Online
		};

		public async Task<Api.Models.Instance> RunPreInstanceTest(CancellationToken cancellationToken)
		{
			var firstTest = await CreateTestInstance(cancellationToken).ConfigureAwait(false);
			//instances always start offline
			Assert.AreEqual(false, firstTest.Online);
			//check it exists
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			var firstClient = instanceManagerClient.CreateClient(firstTest);
			await ApiAssert.ThrowsException<ConflictException>(() => firstClient.DreamDaemon.Start(cancellationToken), ErrorCode.InstanceOffline);

			//cant create instances in existent directories
			var testNonEmpty = Path.Combine(testRootPath, Guid.NewGuid().ToString());
			Directory.CreateDirectory(testNonEmpty);
			var testFile = Path.Combine(testNonEmpty, "asdf");
			await File.WriteAllBytesAsync(testFile, Array.Empty<byte>(), cancellationToken).ConfigureAwait(false);
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.CreateOrAttach(new InstanceCreateRequest
			{
				Path = testNonEmpty,
				Name = "NonEmptyTest"
			}, cancellationToken), ErrorCode.InstanceAtExistingPath).ConfigureAwait(false);

			//check it works for truly empty directories
			File.Delete(testFile);
			var secondTry = await instanceManagerClient.CreateOrAttach(new InstanceCreateRequest
			{
				Path = Path.Combine(testRootPath, Guid.NewGuid().ToString()),
				Name = "NonEmptyTest"
			}, cancellationToken).ConfigureAwait(false);

			await Assert.ThrowsExceptionAsync<ConflictException>(() => instanceManagerClient.CreateOrAttach(FromResponse(firstTest), cancellationToken)).ConfigureAwait(false);
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			//can't create instances in installation directory
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.CreateOrAttach(new InstanceCreateRequest
			{
				Path = "./A/Local/Path",
				Name = "NoInstallDirTest"
			}, cancellationToken), ErrorCode.InstanceAtConflictingPath).ConfigureAwait(false);

			//can't create instances as children of other instances
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.CreateOrAttach(new Api.Models.InstanceCreateRequest
			{
				Path = Path.Combine(firstTest.Path, "subdir"),
				Name = "NoOtherInstanceDirTest"
			}, cancellationToken), ErrorCode.InstanceAtConflictingPath).ConfigureAwait(false);
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			//can't move to existent directories
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.Update(new Api.Models.InstanceUpdateRequest
			{
				Id = firstTest.Id,
				Path = testNonEmpty
			}, cancellationToken), ErrorCode.InstanceAtExistingPath).ConfigureAwait(false);

			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.GrantPermissions(new Api.Models.InstanceUpdateRequest
			{
				Id = 3482974,
			}, cancellationToken), ErrorCode.ResourceNotPresent).ConfigureAwait(false);

			// test can't create instance outside of whitelist
			await ApiAssert.ThrowsException<ApiConflictException>(() => instanceManagerClient.CreateOrAttach(new Api.Models.InstanceCreateRequest
			{
				Name = "TestInstanceOutsideOfWhitelist",
				Path = Path.Combine(testRootPath, "..", Guid.NewGuid().ToString()),
				Online = true,
				ChatBotLimit = 1
			}, cancellationToken), ErrorCode.InstanceNotAtWhitelistedPath);

			//test basic move
			Directory.Delete(testNonEmpty);
			var initialPath = firstTest.Path;
			firstTest = await instanceManagerClient.Update(new InstanceUpdateRequest
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
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			//online it for real for component tests
			firstTest.Online = true;
			firstTest.ConfigurationType = ConfigurationType.HostWrite;
			firstTest = await instanceManagerClient.Update(FromResponse(firstTest), cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(true, firstTest.Online);
			Assert.AreEqual(ConfigurationType.HostWrite, firstTest.ConfigurationType);
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			//can't move online instance
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.Update(new Api.Models.InstanceUpdateRequest
			{
				Id = firstTest.Id,
				Path = initialPath
			}, cancellationToken), ErrorCode.InstanceRelocateOnline).ConfigureAwait(false);
			Assert.IsTrue(Directory.Exists(firstTest.Path));

			return firstTest;
		}

		public async Task RunPostTest(CancellationToken cancellationToken)
		{
			var instances = await instanceManagerClient.List(null, cancellationToken);
			var firstTest = instances.Single(x => x.Name == TestInstanceName);
			var instanceClient = instanceManagerClient.CreateClient(firstTest);

			//can regain permissions on instance without instance user
			var ourInstanceUser = await instanceClient.PermissionSets.Read(cancellationToken).ConfigureAwait(false);
			await instanceClient.PermissionSets.Delete(ourInstanceUser, cancellationToken).ConfigureAwait(false);

			await Assert.ThrowsExceptionAsync<InsufficientPermissionsException>(() => instanceClient.PermissionSets.Read(cancellationToken)).ConfigureAwait(false);

			await instanceManagerClient.GrantPermissions(new Api.Models.InstanceUpdateRequest
			{
				Id = firstTest.Id
			}, cancellationToken).ConfigureAwait(false);
			ourInstanceUser = await instanceClient.PermissionSets.Read(cancellationToken).ConfigureAwait(false);

			Assert.AreEqual(RightsHelper.AllRights<DreamDaemonRights>(), ourInstanceUser.DreamDaemonRights.Value);

			//can't detach online instance
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.Detach(firstTest, cancellationToken), ErrorCode.InstanceDetachOnline).ConfigureAwait(false);

			firstTest.Online = false;
			firstTest = await instanceManagerClient.Update(FromResponse(firstTest), cancellationToken).ConfigureAwait(false);

			await instanceManagerClient.Detach(firstTest, cancellationToken).ConfigureAwait(false);

			var attachPath = Path.Combine(firstTest.Path, InstanceController.InstanceAttachFileName);
			Assert.IsTrue(File.Exists(attachPath));

			//can recreate detached instance
			firstTest = await instanceManagerClient.CreateOrAttach(FromResponse(firstTest), cancellationToken).ConfigureAwait(false);

			// Test updating only with SetChatBotLimit works
			var current = await usersClient.Read(cancellationToken);
			var update = new UserUpdateRequest
			{
				Id = current.Id,
				PermissionSet = new PermissionSet
				{
					InstanceManagerRights = InstanceManagerRights.SetChatBotLimit,
					AdministrationRights = RightsHelper.AllRights<AdministrationRights>()
				}
			};
			await usersClient.Update(update, cancellationToken);
			var update2 = new InstanceUpdateRequest
			{
				Id = firstTest.Id,
				ChatBotLimit = 77
			};
			var newThing = await instanceManagerClient.Update(update2, cancellationToken);

			update.PermissionSet.InstanceManagerRights |= InstanceManagerRights.Delete | InstanceManagerRights.Create | InstanceManagerRights.List;
			await usersClient.Update(update, cancellationToken);

			//but only if the attach file exists
			await instanceManagerClient.Detach(firstTest, cancellationToken).ConfigureAwait(false);
			File.Delete(attachPath);
			await ApiAssert.ThrowsException<ConflictException>(() => instanceManagerClient.CreateOrAttach(FromResponse(firstTest), cancellationToken), ErrorCode.InstanceAtExistingPath).ConfigureAwait(false);
		}
	}
}
