using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tgstation.Server.Api.Models.Tests
{
	/// <summary>
	/// Tests for the <see cref="ErrorMessageResponse"/> class.
	/// </summary>
	[TestClass]
	public sealed class TestErrorCodeExtensions
	{
		[TestMethod]
		public void TestAllErrorCodesHaveDescriptions()
		{
			foreach (var I in Enum.GetValues(typeof(ErrorCode)).Cast<ErrorCode>())
			{
				var message = I.Describe();
				Assert.IsNotNull(message, $"Error code {I} has no message!");
			}
		}
	}
}
