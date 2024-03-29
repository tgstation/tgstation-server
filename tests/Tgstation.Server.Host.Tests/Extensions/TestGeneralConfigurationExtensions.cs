﻿using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Extensions.Tests
{
	[TestClass]
	public sealed class TestGeneralConfigurationExtensions
	{
		[TestMethod]
		public void TestThrowsOnNullParameter()
		{
			Assert.ThrowsException<ArgumentNullException>(() => GeneralConfigurationExtensions.GetCopyDirectoryTaskThrottle(null));
		}
	}
}
