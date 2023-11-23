using System;

#nullable disable

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Component version of <see cref="IInstanceCore"/>.
	/// </summary>
	interface IInstance : IInstanceCore, IComponentService, IAsyncDisposable
	{
	}
}
