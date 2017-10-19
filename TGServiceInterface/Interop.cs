using System;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.ServiceModel;

namespace TGServiceInterface
{

	/// <summary>
	/// Used by DD to access the interop API with call()()
	/// </summary>
	[ServiceContract]
	public interface ITGInterop
	{
		/// <summary>
		/// Called from /world/ExportService(command)
		/// </summary>
		/// <param name="command">The command to run</param>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		[OperationContract]
		bool InteropMessage(string command);
	}

	/// <summary>
	/// Holds the proc that DD calls to access <see cref="ITGInterop"/>
	/// </summary>
	public class DDInteropCallHolder
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
				var channel = Server.CreateChannel<ITGInterop>();
                try
                {
                    channel.CreateChannel().InteropMessage(String.Join(" ", args));
                }
                catch { }
				Server.CloseChannel(channel);
			}
			catch { }
			return 0;
		}
	}
}
