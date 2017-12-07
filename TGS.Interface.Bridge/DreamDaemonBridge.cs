using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TGS.Interface.Components;

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
				var invocationString = String.Join(" ", parsedArgs);
				using (var I = new ServerInterface())
					if (I.ConnectToInstance(instance, true))
						I.GetComponent<ITGInterop>().InteropMessage(invocationString).ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
			}
			catch { }
			return 0;
		}
	}
}
