using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TGServiceTests
{
	/// <summary>
	/// To be the parent of test classes that required a temporary directory
	/// </summary>
	[TestClass]
	public class TempDirectoryRequiredTest
	{
		/// <summary>
		/// The path to the temporary directory
		/// </summary>
		protected string TempPath;

		/// <summary>
		/// Construct a <see cref="TempDirectoryRequiredTest"/>
		/// </summary>
		internal TempDirectoryRequiredTest() { }

		/// <summary>
		/// Setup <see cref="TempPath"/>
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			TempPath = Path.GetTempFileName();
			File.Delete(TempPath);
			Directory.CreateDirectory(TempPath);
		}

		/// <summary>
		/// Cleanup <see cref="TempPath"/>
		/// </summary>
		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(TempPath, true);
		}
	}
}
