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
		public string ReadText(string staticRelativePath, bool repo, out string error, out bool unauthorized)
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
							unauthorized = false;
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
							unauthorized = false;
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
						unauthorized = false;
						return null;
					}

					error = null;
					unauthorized = false;
					return File.ReadAllText(path);
				}
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
		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public string WriteText(string staticRelativePath, string data, out bool unauthorized)
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
					{
						unauthorized = false;
						return "Cannot write above static directories!";
					}

					Directory.CreateDirectory(destdir);
					File.WriteAllText(path, data);
					unauthorized = false;
					return null;
				}
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
				return e.ToString();
			}
		}

		[OperationBehavior(Impersonation = ImpersonationOption.Required)]
		public IList<string> ListStaticDirectory(string subDir, out string error, out bool unauthorized)
		{
			try
			{
				DirectoryInfo dirToEnum = new DirectoryInfo(Path.Combine(StaticDirs, subDir ?? ""));
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
	}
}
