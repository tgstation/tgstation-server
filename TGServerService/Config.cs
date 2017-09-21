using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	//knobs and such
	partial class TGStationServer : ITGConfig
	{
		object configLock = new object();	//for atomic reads/writes
		//public api
		public string ServerDirectory()
		{
			return Environment.CurrentDirectory;
		}

		//public api
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string ReadText(string staticRelativePath, bool repo, out string error)
		{
			try
			{
				var configDir = repo ? RepoPath : StaticDirs;

				var path = Path.Combine(configDir, staticRelativePath);
				lock (configLock)
				{
					var di1 = new DirectoryInfo(configDir);
					if (repo)
					{
						//ensure we aren't trying to read anything outside the static dirs
						var Config = LoadRepoConfig();
						if (Config == null)
						{
							error = "Unable to load static directory configuration";
							return null;
						}
						var Found = false;
						foreach (var I in Config.StaticDirectoryPaths)
						{
							if (di1.FullName == new DirectoryInfo(Path.Combine(RepoPath, I)).FullName)
							{
								Found = true;
								break;
							}
						}
						if (!Found)
						{
							error = "File is not in a configured static directory!";
							return null;
						}
					}

					var di2 = new DirectoryInfo(new FileInfo(path).Directory.FullName);

					var good = false;
					while (di2 != null)
					{
						if (di2.FullName == di1.FullName)
						{
							good = true;
							break;
						}
						else di2 = di2.Parent;
					}

					if (!good)
					{
						error = "Cannot read above static directories!";
						return null;
					}

					error = null;
					return File.ReadAllText(path);
				}
			}
			catch (Exception e)
			{
				error = e.ToString();
				return null;
			}
		}
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string WriteText(string staticRelativePath, string data)
		{
			try
			{
				var path = Path.Combine(StaticDirs, staticRelativePath);
				lock (configLock)
				{
					var di1 = new DirectoryInfo(StaticDirs);
					var destdir = new FileInfo(path).Directory.FullName;
					var di2 = new DirectoryInfo(destdir);

					var good = false;
					while (di2 != null)
					{
						if (di2.FullName == di1.FullName)
						{
							good = true;
							break;
						}
						else di2 = di2.Parent;
					}

					if (!good)
						return "Cannot write above static directories!";
					Directory.CreateDirectory(destdir);
					File.WriteAllText(path, data);
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
	}
}
