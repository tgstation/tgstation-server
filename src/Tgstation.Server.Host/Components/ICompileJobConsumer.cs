using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	interface ICompileJobConsumer : IHostedService, IDisposable
	{
		Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken);
	}
}