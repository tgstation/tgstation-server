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
		/// <summary>
		/// Sample cleartext
		/// </summary>
		const string PlainText = "According to all known laws of aviation, there is no way a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway because bees don't care what humans think is impossible.";

		/// <summary>
		/// Run assertions for a successful call to <see cref="Helpers.EncryptData(string, out string)"/>
		/// </summary>
		/// <param name="entropy">The out string for the entropy parameter of <see cref="Helpers.EncryptData(string, out string)"/></param>
		/// <returns>The result of <see cref="Helpers.EncryptData(string, out string)"/> with <see cref="PlainText"/> as a parameter</returns>
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

		/// <summary>
		/// Tests that <see cref="Helpers.EncryptData(string, out string)"/> can execute successfully
		/// </summary>
		[TestMethod]
		public void TestEncryptDataWorks()
		{
			AssertEncryptData(out string entropy);
		}

		/// <summary>
		/// Tests that <see cref="Helpers.DecryptData(string, string)"/> can execute successfully
		/// </summary>
		[TestMethod]
		public void TestDecryptDataWorks()
		{
			var result = AssertEncryptData(out string entropy);
			var decrypted = Helpers.DecryptData(result, entropy);
			Assert.AreEqual(decrypted, PlainText);
		}
	}
}
