using System;
using System.IO;

namespace TGS.Tests
{
	/// <summary>
	/// Used for providing an executable path for a quick, unending, mock <see cref="System.Diagnostics.Process"/>
	/// </summary>
	sealed class TestInfiniteProcessPath : TestProcessPath
	{
		public TestInfiniteProcessPath() : base()
		{
			try
			{
				File.WriteAllText(filePath, "@echo off" + Environment.NewLine + ":loop" + Environment.NewLine + "goto loop");
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void SetExitCode(int value)
		{
			throw new NotImplementedException();
		}
	}
}
