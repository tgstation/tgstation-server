using System;
using System.Collections.Generic;
using System.Reflection;
using TGS.Interface.Components;

namespace TGS.Interface
{
	/// <summary>
	/// Contains constants for the interface
	/// </summary>
	public static class Definitions
	{
		/// <summary>
		/// The maximum message size to and from a local server 
		/// </summary>
		public const long TransferLimitLocal = Int32.MaxValue;   //2GB can't go higher

		/// <summary>
		/// The maximum message size to and from a remote server
		/// </summary>
		public const long TransferLimitRemote = 10485760;   //10 MB

		/// <summary>
		/// Base name of communication URLs
		/// </summary>
		public const string MasterInterfaceName = "TGStationServerService";
		/// <summary>
		/// Base name of instance URLs
		/// </summary>
		public const string InstanceInterfaceName = MasterInterfaceName + "/Instance";

		/// <summary>
		/// Version of the interface
		/// </summary>
		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
	}
}
