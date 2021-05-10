using System;

using Microsoft.Extensions.Hosting;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Component version of <see cref="IInstanceCore"/>.
	/// </summary>
	interface IInstance : IInstanceCore, IHostedService, IAsyncDisposable
	{
	}
}
