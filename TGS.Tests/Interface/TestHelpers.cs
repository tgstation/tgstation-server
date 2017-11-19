using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TGS.Interface.Tests
{
	/// <summary>
	/// Tests for <see cref="Helpers"/>
	/// </summary>
	[TestClass]
	public class TestHelpers
	{
		const string PlainText = "According to all known laws of aviation, there is no way a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway because bees don't care what humans think is impossible.";
		
		string AssertEncryptData(out string entropy)
		{
			var result = Helpers.EncryptData(PlainText, out entropy);
			Assert.AreNotEqual(PlainText, entropy);
			Assert.AreNotEqual(PlainText, result);
			Assert.AreNotEqual(result, entropy);
			Assert.IsFalse(String.IsNullOrWhiteSpace(result));
			Assert.IsFalse(String.IsNullOrWhiteSpace(entropy));
			return result;
		}
		
		[TestMethod]
		public void TestEncryptDataWorks()
		{
			AssertEncryptData(out string entropy);
		}
		
		[TestMethod]
		public void TestDecryptDataWorks()
		{
			var result = AssertEncryptData(out string entropy);
			var decrypted = Helpers.DecryptData(result, entropy);
			Assert.AreEqual(decrypted, PlainText);
		}
	}
}
