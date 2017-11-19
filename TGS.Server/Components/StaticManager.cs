using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class StaticManager : IStaticManager
	{
		/// <summary>
		/// Directory containing static file for the <see cref="Instance"/>
		/// </summary>
		const string StaticDirs = "Static";
		/// <summary>
		/// Backup directory for <see cref="StaticDirs"/>
		/// </summary>
		const string StaticBackupDir = "Static_BACKUP";
		/// <summary>
		/// Standard parent directory relative path;
		/// </summary>
		const string ParentDirectory = "..";

		/// <summary>
		/// Error message shown when <see cref="ParentDirectory"/> use is attempted 
		/// </summary>
		static readonly string ParentAccessError = String.Format("Unable to process paths containing \"{0}\"!", ParentDirectory);

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="StaticManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="StaticManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IRepoConfigProvider"/> for the <see cref="StaticManager"/>
		/// </summary>
		readonly IRepoConfigProvider RepoConfigProvider;
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="StaticManager"/>
		/// </summary>
		readonly IRepositoryManager Repo;

		/// <summary>
		/// Cancels WCF's user impersonation to allow clean access to writing log files
		/// </summary>
		static void CancelImpersonation()
		{
			WindowsIdentity.Impersonate(IntPtr.Zero);
		}

		/// <summary>
		/// Construct a <see cref="StaticManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		/// <param name="repoConfigProvider">The value of <see cref="RepoConfigProvider"/></param>
		/// <param name="repo">The value of <see cref="Repo"/></param>
		public StaticManager(IInstanceLogger logger, IIOManager io, IRepoConfigProvider repoConfigProvider, IRepositoryManager repo)
		{
			Logger = logger;
			IO = io;
			RepoConfigProvider = repoConfigProvider;
			Repo = repo;

			IO.CreateDirectory(StaticDirs);
		}

		/// <inheritdoc />
		public void Recreate()
		{
			lock (this)
			{
				if (IO.DirectoryExists(StaticDirs))
				{
					var count = 0;
					var newFullPath = StaticBackupDir;

					while (IO.FileExists(newFullPath) || IO.DirectoryExists(newFullPath))
						newFullPath = String.Format("{0}({1})", StaticBackupDir, ++count);

					IO.MoveDirectory(StaticDirs, newFullPath).Wait();
				}
				var repo_config = RepoConfigProvider.GetRepoConfig();
				var copyPaths = new List<string>();
				copyPaths.AddRange(repo_config.DLLPaths);
				copyPaths.AddRange(repo_config.StaticDirectoryPaths);
				IO.CreateDirectory(StaticDirs);
				Repo.CopyToRestricted(StaticDirs, copyPaths).Wait();
			}
		}

		/// <inheritdoc />
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string ReadText(string staticRelativePath, bool repo, out string error, out bool unauthorized)
		{
			string path = null;
			try
			{
				if (staticRelativePath.Contains(ParentDirectory))
				{
					error = ParentAccessError;
					unauthorized = false;
					return null;
				}

				var configDir = repo ? RepositoryManager.RepoPath : StaticDirs;

				path = Path.Combine(configDir, staticRelativePath);

				string output;
				lock (this)
					output = IO.ReadAllText(path).Result;

				CancelImpersonation();
				Logger.WriteInfo("Read of " + path, EventID.StaticRead);
				error = null;
				unauthorized = false;
				return output;
			}
			catch (UnauthorizedAccessException e)
			{
				//no need for the full stacktrace
				error = e.Message;
				unauthorized = true;
				return null;
			}
			catch (Exception e)
			{
				error = e.ToString();
				CancelImpersonation();
				Logger.WriteWarning(String.Format("Read of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				unauthorized = false;
				return null;
			}
		}

		/// <inheritdoc />
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string WriteText(string staticRelativePath, string data, out bool unauthorized)
		{
			var path = Path.Combine(StaticDirs, staticRelativePath);
			try
			{
				if (staticRelativePath.Contains(ParentDirectory))
				{
					unauthorized = false;
					return ParentAccessError;
				}

				lock (this)
				{
					IO.CreateDirectory(path);
					IO.WriteAllText(path, data).Wait();
				}

				CancelImpersonation();
				Logger.WriteInfo("Write to " + path, EventID.StaticWrite);
				unauthorized = false;
				return null;
			}
			catch (UnauthorizedAccessException e)
			{
				//no need for the full stacktrace
				unauthorized = true;
				return e.Message;
			}
			catch (Exception e)
			{
				unauthorized = false;
				CancelImpersonation();
				Logger.WriteWarning(String.Format("Write of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}
		/// <inheritdoc />
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string DeleteFile(string staticRelativePath, out bool unauthorized)
		{
			var path = Path.Combine(StaticDirs, staticRelativePath);
			try
			{
				if (staticRelativePath.Contains(ParentDirectory))
				{
					unauthorized = false;
					return ParentAccessError;
				}

				lock (this)
				{
					if (IO.FileExists(path))
						IO.DeleteFile(path).Wait();
					else if (IO.DirectoryExists(path))
						IO.DeleteDirectory(path).Wait();
				}
				CancelImpersonation();
				Logger.WriteInfo("Delete of " + path, EventID.StaticDelete);
				unauthorized = false;
				return null;
			}
			catch (UnauthorizedAccessException e)
			{
				//no need for the full stacktrace
				unauthorized = true;
				return e.Message;
			}
			catch (Exception e)
			{
				unauthorized = false;
				CancelImpersonation();
				Logger.WriteWarning(String.Format("Delete of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}

		/// <inheritdoc />
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public IList<string> ListStaticDirectory(string subDir, out string error, out bool unauthorized)
		{
			try
			{
				if (subDir.Contains(ParentDirectory))
				{
					unauthorized = false;
					error = ParentAccessError;
					return null;
				}

				var dirToEnum = new DirectoryInfo(IO.ResolvePath(Path.Combine(StaticDirs, subDir ?? ""))); //do not use path.combine or it will try and take the root
				var result = new List<string>();
				foreach (var I in dirToEnum.GetFiles())
					result.Add(I.Name);
				foreach (var I in dirToEnum.GetDirectories())
					result.Add('/' + I.Name);
				error = null;
				unauthorized = false;
				return result;
			}
			catch (UnauthorizedAccessException e)
			{
				//no need for the full stacktrace
				error = e.Message;
				unauthorized = true;
				return null;
			}
			catch (Exception e)
			{
				error = e.ToString();
				unauthorized = false;
				return null;
			}
		}

		/// <inheritdoc />
		public void SymlinkTo(string path)
		{
			lock (this)
			{
				var DI = new DirectoryInfo(IO.ResolvePath(StaticDirs));
				foreach (var I in DI.GetFiles())
				{
					var targetPath = Path.Combine(path, I.Name);
					if (!IO.FileExists(targetPath))
						IO.CreateSymlink(targetPath, I.FullName);
				}
				foreach (var I in DI.GetDirectories())
				{
					var targetPath = Path.Combine(path, I.Name);
					if (!IO.DirectoryExists(targetPath))
						IO.CreateSymlink(targetPath, I.FullName);
				}
			}
		}
	}
}
