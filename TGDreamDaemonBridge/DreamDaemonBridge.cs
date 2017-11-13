using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGDreamDaemonBridge
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
				using (var I = new ServerInterface())
					if(I.ConnectToInstance(instance, true).HasFlag(ConnectivityLevel.Connected))
						I.GetComponent<ITGInterop>().InteropMessage(String.Join(" ", parsedArgs));
			}
			catch { }
			return 0;
		}
	}
}
