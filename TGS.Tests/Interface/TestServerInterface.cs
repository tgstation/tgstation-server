﻿using System;
using System.Net;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGS.Interface.Components;

namespace TGS.Interface.Tests
{
	/// <summary>
	/// Tests for <see cref="Interface"/>
	/// </summary>
	[TestClass]
	public class TestInterface
	{
		/// <summary>
		/// Name to use for testing <see cref="ITGInstance"/>s
		/// </summary>
		const string TestInstanceName = "TestInstance";

		[TestMethod]
		public void TestSetBadCertificateHandler()
		{
			Func<string, bool> func = (message) =>
			{
				Assert.IsFalse(String.IsNullOrWhiteSpace(message));
				return true;
			};
			Client.SetBadCertificateHandler(func);
		}

		[TestMethod]
		public void TestBadCertificateHandler()
		{
			var ran = 0;
			Client.SetBadCertificateHandler(_ =>
			{
				++ran;
				return true;
			});
			foreach (System.Net.Security.SslPolicyErrors error in Enum.GetValues(typeof(System.Net.Security.SslPolicyErrors)))
				ServicePointManager.ServerCertificateValidationCallback(this, new System.Security.Cryptography.X509Certificates.X509Certificate(), new System.Security.Cryptography.X509Certificates.X509Chain(), error);
			//-1 for the None error
			Assert.AreEqual(Enum.GetValues(typeof(System.Net.Security.SslPolicyErrors)).Length - 1, ran);
		}

		/// <summary>
		/// Creates a remote configured <see cref="Interface"/> pointing at an invalid address
		/// </summary>
		/// <returns>The created <see cref="Interface"/></returns>
		Client CreateFakeRemoteInterface()
		{
			return new Client(new RemoteLoginInfo("some.fake.url.420", 34752, "user", "password"));
		}
		
		[TestMethod]
		public void TestLocalInstantiation()
		{
			Assert.IsFalse(new Client().IsRemoteConnection);
		}

		[TestMethod]
		public void TestRemoteInstatiation()
		{
			Assert.IsTrue(CreateFakeRemoteInterface().IsRemoteConnection);
		}

		[TestMethod]
		public void TestCopyRemoteInterface()
		{
			var first = CreateFakeRemoteInterface();
			var second = new Client(first.LoginInfo);
			Assert.AreEqual(first.LoginInfo.IP, second.LoginInfo.IP);
			Assert.AreEqual(first.LoginInfo.Port, second.LoginInfo.Port);
			Assert.AreEqual(first.LoginInfo.Username, second.LoginInfo.Username);
			Assert.AreEqual(first.LoginInfo.Password, second.LoginInfo.Password);
			Assert.IsTrue(second.IsRemoteConnection);
		}

		[TestMethod]
		public void TestLocalAccessInterfaceAllowsWindowsImpersonation()
		{
			var inter = new Client();
			var po = new PrivateObject(inter);
			var cf = (ChannelFactory<ITGStatic>)po.Invoke("CreateChannel", new Type[] { typeof(string) }, new object[] { TestInstanceName }, new Type[] { typeof(ITGStatic) });
			Assert.AreEqual(cf.Credentials.Windows.AllowedImpersonationLevel, System.Security.Principal.TokenImpersonationLevel.Impersonation);
		}
	}
}
