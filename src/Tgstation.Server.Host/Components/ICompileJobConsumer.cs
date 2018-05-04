using Microsoft.Extensions.Hosting;
using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	interface ICompileJobConsumer : IHostedService, IDisposable
	{
		void LoadCompileJob(CompileJob job);
	}
}