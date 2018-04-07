using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	interface IAuthenticationContext : IDisposable
	{
        User User { get; }

        InstanceUser InstanceUser { get; }

        IAuthenticationContext Clone();
	}
}