using System.Collections.Generic;

namespace Tgstation.Server.ControlPanel
{
	sealed class UserSettings
	{
		public List<Connection> Connections { get; set; } = new List<Connection>();
	}
}
