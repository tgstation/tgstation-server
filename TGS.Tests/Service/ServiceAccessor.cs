

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// For accessing service control methods of <see cref="Service"/>
	/// </summary>
	class ServiceAccessor : Service
	{
		/// <summary>
		/// Fake a <see cref="Service"/> start up
		/// </summary>
		/// <param name="args">Fake commandline parameters passed to <see cref="OnStart"/></param>
		public void FakeStart(string[] args)
		{
			OnStart(args);
		}
		
		/// <summary>
		/// Fake a <see cref="Service"/> shutdown
		/// </summary>
		public void FakeStop()
		{
			OnStop();
		}
	}
}
