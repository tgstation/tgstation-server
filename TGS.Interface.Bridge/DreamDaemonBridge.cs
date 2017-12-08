using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TGS.Interface.Bridge
{
	/// <summary>
	/// Holds the proc that DD calls to access <see cref="ITGInterop"/>
	/// </summary>
	public static class DreamDaemonBridge
	{
		/// <summary>
		/// The proc that DD calls to access <see cref="ITGInterop"/>
		/// </summary>
		/// <param name="argc">The number of arguments passed</param>
		/// <param name="args">The arguments passed</param>
		/// <returns>0</returns>
		[DllExport("DDEntryPoint", CallingConvention = CallingConvention.Cdecl)]
		public static int DDEntryPoint(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 0)]string[] args)
		{
			try
			{
				var parsedArgs = new List<string>();
				parsedArgs.AddRange(args);
				var instance = parsedArgs[0];
				parsedArgs.RemoveAt(0);
				using (var I = new Client())
					I.Server.GetInstance(instance).Interop.InteropMessage(String.Join(" ", parsedArgs));
			}
			catch { }
			return 0;
		}
	}
}
