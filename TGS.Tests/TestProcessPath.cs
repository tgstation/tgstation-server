using System;
using System.IO;

namespace TGS.Tests
{
	/// <summary>
	/// Used for providing an executable path for a quick mock <see cref="System.Diagnostics.Process"/>
	/// </summary>
	sealed class TestProcessPath : IDisposable
	{
		public string Path => filePath;

		public int ExitCode {
			set
			{
				File.WriteAllText(filePath, String.Format("EXIT /B {0}", value));
			}
		}

		readonly string filePath;

		int exitCode;
		bool disposedValue;

		public TestProcessPath()
		{
			var temp = System.IO.Path.GetTempFileName();
			filePath = temp + ".bat";
			try
			{
				File.Move(temp, filePath);
			}
			catch
			{
				File.Delete(temp);
				throw;
			}
		}

		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				File.Delete(filePath);
				disposedValue = true;
			}
		}

		~TestProcessPath() {
			Dispose(false);
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
