using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security.Tests
{
	/// <summary>
	/// Tests for <see cref="AuthenticationContext"/>
	/// </summary>
	[TestClass]
	public sealed class TestAuthenticationContext
	{
		[TestMethod]
		public void TestGetRightsGeneric()
		{
			var user = new User()
			{
				PermissionSet = new PermissionSet()
			};
			var instanceUser = new InstancePermissionSet();
			var authContext = new AuthenticationContext();
			authContext.Initialize(user, DateTimeOffset.UtcNow, "asdf", instanceUser, null);

			user.PermissionSet.AdministrationRights = AdministrationRights.WriteUsers;
			instanceUser.EngineRights = EngineRights.InstallOfficialOrChangeActiveByondVersion | EngineRights.ReadActive;
			Assert.AreEqual((ulong)user.PermissionSet.AdministrationRights, authContext.GetRight(RightsType.Administration));
			Assert.AreEqual((ulong)instanceUser.EngineRights, authContext.GetRight(RightsType.Engine));
		}
	}
}
