using System;
using System.Collections.Generic;

namespace Tgstation.Server.Host.Core
{
	interface ISystemIdentity : IDisposable
	{
		string Uid { get; }
		string Username { get; }
		IEnumerable<string> Groups { get; }

        ISystemIdentity Clone();
    }
}