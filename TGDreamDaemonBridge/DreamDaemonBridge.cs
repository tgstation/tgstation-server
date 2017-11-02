using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGDreamDaemonBridge
{
	/// <summary>
	/// Holds the proc that DD calls to access <see cref="ITGInterop"/>
	/// </summary>
	public sealed class DreamDaemonBridge
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
				using (var I = new Interface())
					I.GetComponent<ITGInterop>().InteropMessage(String.Join(" ", args));
			}
			catch { }
			return 0;
		}
	}
}
