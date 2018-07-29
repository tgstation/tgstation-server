using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <inheritdoc />
	sealed class ScriptExecutor : IScriptExecutor
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ScriptExecutor"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ScriptExecutor"/>
		/// </summary>
		readonly ILogger<ScriptExecutor> logger;

		/// <summary>
		/// Construct a <see cref="ScriptExecutor"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public ScriptExecutor(IIOManager ioManager, ILogger<ScriptExecutor> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<int?> ExecuteScript(string scriptPath, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			try
			{
				using (var process = new Process())
				{
					process.StartInfo.FileName = scriptPath;
					process.StartInfo.WorkingDirectory = ioManager.GetDirectoryName(scriptPath);
					process.StartInfo.Arguments = String.Join(" ", parameters);
					process.EnableRaisingEvents = true;

					var tcs = new TaskCompletionSource<object>();
					process.Exited += (a, b) => tcs.SetResult(null);
					try
					{
						process.Start();
						using (cancellationToken.Register(() => tcs.SetCanceled()))
							await tcs.Task.ConfigureAwait(false);
					}
					catch (InvalidOperationException)
					{
						try
						{
							process.Kill();
							process.WaitForExit();
						}
						catch (InvalidOperationException) { }
					}

					return process.ExitCode;
				}
			}
			catch (Exception e)
			{
				logger.LogWarning("Error running shell script {0}! Exception: {1}", scriptPath, e);
				return null;
			}
		}
	}
}
