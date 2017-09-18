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
	public interface ITGServiceBridge
	{
		/// <summary>
		/// Called from /world/ExportService(command)
		/// </summary>
		/// <param name="command">The command to run</param>
		/// <returns>true on success, false on failure</returns>
		[OperationContract]
		bool InteropMessage(string command);
	}

	/// <summary>
	/// Holds the proc that DD calls to access <see cref="ITGServiceBridge"/>
	/// </summary>
	public class DDInteropCallHolder
	{
		/// <summary>
		/// The proc that DD calls to access <see cref="ITGServiceBridge"/>
		/// </summary>
		/// <param name="args">The arguments passed</param>
		/// <returns>0 success, -1 on a WCF, error, 1 on an operation error</returns>
		[DllExport("DDEntryPoint", CallingConvention = CallingConvention.Cdecl)]
		public static int DDEntryPoint(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 0)]string[] args)
		{
			try
			{
                ChannelFactory<ITGServiceBridge> channel = null;
                try
                {
                    Server.GetComponentAndChannel(out channel).InteropMessage(String.Join(" ", args));
                    channel.Close();
                }
                catch
                {
                    if(channel != null)
                        channel.Abort();
                }
			}
			catch { }
			return 0;
		}
	}
}
