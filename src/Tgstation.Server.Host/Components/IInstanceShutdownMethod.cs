using System;
using System.Collections.Generic;
using System.Text;

namespace Tgstation.Server.Host.Components
{
    interface IInstanceShutdownMethod
	{
		bool GracefulShutdown { get; set; }
	}
}
