using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading.Tasks;
using TGS.Server.Configuration;
using TGS.Server.IO;
using TGS.Server.Logging;

using DirectoryInfo = System.IO.DirectoryInfo;

namespace TGS.Server.Components
{
	/// <inheritdoc />
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
		/// Begins user impersonation to allow proper restricted file access
		/// </summary>
		static WindowsImpersonationContext BeginImpersonation()
		{
			return OperationContext.Current.ServiceSecurityContext.WindowsIdentity.Impersonate();
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

			IO.CreateDirectory(StaticDirs).Wait();
		}

		/// <inheritdoc />
		public void Recreate()
		{
			lock (this)
			{
				if (IO.DirectoryExists(StaticDirs).Result)
				{
					var count = 0;
					var newFullPath = StaticBackupDir;

					while (IO.FileExists(newFullPath).Result || IO.DirectoryExists(newFullPath).Result)
						newFullPath = String.Format("{0}({1})", StaticBackupDir, ++count);

					IO.MoveDirectory(StaticDirs, newFullPath).Wait();
				}
				var repo_config = RepoConfigProvider.GetRepoConfig();
				var copyPaths = new List<string>();
				copyPaths.AddRange(repo_config.DLLPaths);
				copyPaths.AddRange(repo_config.StaticDirectoryPaths);
				IO.CreateDirectory(StaticDirs).Wait();
				var task = Repo.CopyToRestricted(StaticDirs, copyPaths);
				foreach (var I in repo_config.StaticDirectoryPaths)
					IO.CreateDirectory(IOManager.ConcatPath(StaticDirs, I)).Wait();
				task.Wait();
			}
		}

		/// <inheritdoc />
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

				path = IOManager.ConcatPath(configDir, staticRelativePath);

				string output;
				lock (this)
					using (var ctx = BeginImpersonation())
						output = IO.ReadAllText(path).Result;

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
				Logger.WriteWarning(String.Format("Read of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				unauthorized = false;
				return null;
			}
		}

		/// <inheritdoc />
		public string WriteText(string staticRelativePath, string data, out bool unauthorized)
		{
			BeginImpersonation();
			var path = IOManager.ConcatPath(StaticDirs, staticRelativePath);
			try
			{
				if (staticRelativePath.Contains(ParentDirectory))
				{
					unauthorized = false;
					return ParentAccessError;
				}

				using (var ctx = BeginImpersonation())
					lock (this)
					{
						IO.CreateDirectory(path).Wait();
						IO.WriteAllText(path, data).Wait();
					}

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
				Logger.WriteWarning(String.Format("Write of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}
		/// <inheritdoc />
		public string DeleteFile(string staticRelativePath, out bool unauthorized)
		{
			var path = IOManager.ConcatPath(StaticDirs, staticRelativePath);
			try
			{
				if (staticRelativePath.Contains(ParentDirectory))
				{
					unauthorized = false;
					return ParentAccessError;
				}

				using (var ctx = BeginImpersonation())
					lock (this)
					{
						if (IO.FileExists(path).Result)
							IO.DeleteFile(path).Wait();
						else if (IO.DirectoryExists(path).Result)
							IO.DeleteDirectory(path).Wait();
					}

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
				Logger.WriteWarning(String.Format("Delete of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}

		/// <inheritdoc />
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

				subDir = subDir.TrimStart(new char[] { '/', '\\' });
				var result = new List<string>();
				using (var ctx = BeginImpersonation())
				{
					var dirToEnum = new DirectoryInfo(IO.ResolvePath(IOManager.ConcatPath(StaticDirs, subDir ?? ""))); //do not use path.combine or it will try and take the root
					foreach (var I in dirToEnum.GetFiles())
						result.Add(I.Name);
					foreach (var I in dirToEnum.GetDirectories())
						result.Add('/' + I.Name);
				}
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
				var tasks = new List<Task>();
				foreach (var I in DI.GetFiles())
				{
					var targetPath = IOManager.ConcatPath(path, I.Name);
					if (!IO.FileExists(targetPath).Result)
						tasks.Add(IO.CreateSymlink(targetPath, I.FullName));				}
				foreach (var I in DI.GetDirectories())
				{
					var targetPath = IOManager.ConcatPath(path, I.Name);
					if (!IO.DirectoryExists(targetPath).Result)
						tasks.Add(IO.CreateSymlink(targetPath, I.FullName));
				}
				Task.WaitAll(tasks.ToArray());
			}
		}

		/// <inheritdoc />
		public async Task<List<string>> CopyDMFilesTo(string destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			var baseURI = new Uri(destination);
			var res = new List<string>();
			var tasks = new List<Task>();
			foreach (var I in await IO.GetFilesWithExtensionInDirectory(StaticDirs, ".dm"))
			{
				var fileURI = new Uri(I);
				var fileName = IO.GetFileName(I);
				tasks.Add(IO.CopyFile(I, IOManager.ConcatPath(destination, fileName), false, false));
				res.Add(String.Format(CultureInfo.InvariantCulture, "#include \"{0}\"", fileName));
			}
			await Task.WhenAll(tasks);
			return res;
		}
	}
}
