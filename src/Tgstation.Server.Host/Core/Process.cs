using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class Process : IProcess
	{

		public int Id { get; }

		public Task Startup { get; }

		public Task<int> Lifetime { get; }

		readonly System.Diagnostics.Process handle;

		readonly StringBuilder outputStringBuilder;
		readonly StringBuilder errorStringBuilder;
		readonly StringBuilder combinedStringBuilder;

		public Process(System.Diagnostics.Process handle, Task<int> lifetime, StringBuilder outputStringBuilder, StringBuilder errorStringBuilder, StringBuilder combinedStringBuilder)
		{
			this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
			Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

			this.outputStringBuilder = outputStringBuilder;
			this.errorStringBuilder = errorStringBuilder;
			this.combinedStringBuilder = combinedStringBuilder;

			Id = handle.Id;
			Startup = Task.Factory.StartNew(() =>
			{
				try
				{
					handle.WaitForInputIdle();
				}
				catch (InvalidOperationException) { }
			}, default, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		public void Dispose() => handle.Dispose();

		public string GetCombinedOutput()
		{
			if (combinedStringBuilder == null)
				throw new InvalidOperationException("Output/Error reading was not enabled!");
			return combinedStringBuilder.ToString();
		}

		public string GetErrorOutput()
		{
			if (errorStringBuilder == null)
				throw new InvalidOperationException("Error reading was not enabled!");
			return errorStringBuilder.ToString();
		}

		public string GetStandardOutput()
		{
			if (outputStringBuilder == null)
				throw new InvalidOperationException("Output reading was not enabled!");
			return errorStringBuilder.ToString();
		}

		public void Terminate()
		{
			try
			{
				handle.Kill();
				handle.WaitForExit();
			}
			catch (InvalidOperationException) { }
		}
	}
}
