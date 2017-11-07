using System;
using System.Collections.Generic;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGCommandLine
{
	/// <summary>
	/// Used for managing the <see cref="ITGSService"/> components
	/// </summary>
	class ServiceCommand : RootCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceCommand"/>
		/// </summary>
		public ServiceCommand()
		{
			Keyword = "service";
			Children = new Command[] { new ServiceCreateInstanceCommand(), new ServiceDetachInstanceCommand(), new ServiceEnableInstanceCommand(), new ServiceImportInstanceCommand(), new ServiceListInstancesCommand(), new ServicePythonPathCommand(), new ServiceSetPythonPathCommand(), new ServiceSetRemoteAccessPortCommand(), new ServiceRemoteAccessPortCommand(), new ServiceDisableInstanceCommand(), new ServiceRenameInstanceCommand() };
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Manage service wide settings";
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.CreateInstance(string, string)"/>
	/// </summary>
	class ServiceCreateInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceCreateInstanceCommand"/>
		/// </summary>
		public ServiceCreateInstanceCommand()
		{
			Keyword = "create-instance";
			RequiredParameters = 2;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Creates a new instance at the given path";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<name> <path>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().CreateInstance(parameters[0], parameters[1]);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGLanding.ListInstances"/>
	/// </summary>
	class ServiceListInstancesCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceListInstancesCommand"/>
		/// </summary>
		public ServiceListInstancesCommand()
		{
			Keyword = "list-instances";
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Lists all instances";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			foreach (var I in Interface.GetServiceComponent<ITGLanding>().ListInstances())
				OutputProc(String.Format("{0} ({1}):\t{2}{3}", I.Name, I.Path, I.Enabled ? "Online" : "Offline", I.Enabled ? String.Format(" ({0})", I.LoggingID) : ""));
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.DetachInstance(string)"/>
	/// </summary>
	class ServiceDetachInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceDetachInstanceCommand"/>
		/// </summary>
		public ServiceDetachInstanceCommand()
		{
			Keyword = "detach-instance";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Detaches an instance";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<name>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().DetachInstance(parameters[0]);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.ImportInstance(string)"/>
	/// </summary>
	class ServiceImportInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceImportInstanceCommand"/>
		/// </summary>
		public ServiceImportInstanceCommand()
		{
			Keyword = "import-instance";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Imports an instance (Chat settings will be lost if they are from a different windows installation)";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<path>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().ImportInstance(parameters[0]);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGSService.PythonPath"/>
	/// </summary>
	class ServicePythonPathCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServicePythonPathCommand"/>
		/// </summary>
		public ServicePythonPathCommand()
		{
			Keyword = "python";
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Displays configured path the service uses for python";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGSService>().PythonPath();
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGSService.SetPythonPath(string)"/>
	/// </summary>
	class ServiceSetPythonPathCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceSetPythonPathCommand"/>
		/// </summary>
		public ServiceSetPythonPathCommand()
		{
			Keyword = "set-python";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Sets the path to the python installation";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<path>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			Interface.GetServiceComponent<ITGSService>().SetPythonPath(parameters[0]);
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.SetInstanceEnabled(string, bool)"/> with a <see langword="true"/> parameter
	/// </summary>
	class ServiceEnableInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceEnableInstanceCommand"/>
		/// </summary>
		public ServiceEnableInstanceCommand()
		{
			Keyword = "enable-instance";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Enables the specified instance";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<name>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().SetInstanceEnabled(parameters[0], true);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.SetInstanceEnabled(string, bool)"/> with a <see langword="false"/> parameter
	/// </summary>
	class ServiceDisableInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceDisableInstanceCommand"/>
		/// </summary>
		public ServiceDisableInstanceCommand()
		{
			Keyword = "disable-instance";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Disables the specified instance";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<name>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().SetInstanceEnabled(parameters[0], false);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="TGServiceInterface.Components.ITGSService.RemoteAccessPort"/>
	/// </summary>
	class ServiceRemoteAccessPortCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceRemoteAccessPortCommand"/>
		/// </summary>
		public ServiceRemoteAccessPortCommand()
		{
			Keyword = "port";
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Displays the service's remote access port";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			OutputProc(Interface.GetServiceComponent<ITGSService>().RemoteAccessPort().ToString());
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="TGServiceInterface.Components.ITGSService.SetRemoteAccessPort(ushort)"/>
	/// </summary>
	class ServiceSetRemoteAccessPortCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceSetRemoteAccessPortCommand"/>
		/// </summary>
		public ServiceSetRemoteAccessPortCommand()
		{
			Keyword = "set-port";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Sets the service's remote access port";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<port>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			ushort port;
			try
			{
				port = Convert.ToUInt16(parameters[0]);
			}
			catch
			{
				OutputProc("Invalid port number!");
				return ExitCode.BadCommand;
			}

			var res = Interface.GetServiceComponent<ITGSService>().SetRemoteAccessPort(port);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			OutputProc("Change will be applied after service restart");
			return ExitCode.Normal;
		}
	}

	/// <summary>
	/// Command for calling <see cref="ITGInstanceManager.RenameInstance(string, string)"/>
	/// </summary>
	class ServiceRenameInstanceCommand : ConsoleCommand
	{
		/// <summary>
		/// Construct a <see cref="ServiceRenameInstanceCommand"/>
		/// </summary>
		public ServiceRenameInstanceCommand()
		{
			Keyword = "rename-instance";
			RequiredParameters = 1;
		}

		/// <inheritdoc />
		public override string GetHelpText()
		{
			return "Renames an instance. Will temporarily disable the instance if it is active";
		}

		/// <inheritdoc />
		public override string GetArgumentString()
		{
			return "<name> <new_name>";
		}

		/// <inheritdoc />
		protected override ExitCode Run(IList<string> parameters)
		{
			var res = Interface.GetServiceComponent<ITGInstanceManager>().RenameInstance(parameters[0], parameters[1]);
			if (res != null)
			{
				OutputProc(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
}
