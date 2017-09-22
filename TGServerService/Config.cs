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

				var path = configDir + '/' + staticRelativePath;   //do not use path.combine or it will try and take the root
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

					var output = File.ReadAllText(path);
					TGServerService.CancelImpersonation();
					TGServerService.WriteInfo("Read of " + path, TGServerService.EventID.StaticRead);
					error = null;
					unauthorized = false;
					return output;
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
				var path = StaticDirs + '/' + staticRelativePath;   //do not use path.combine or it will try and take the root
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
					TGServerService.CancelImpersonation();
					TGServerService.WriteInfo("Write to " + path, TGServerService.EventID.StaticWrite);
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
		public string DeleteFile(string staticRelativePath, out bool unauthorized)
		{
			try
			{
				var path = StaticDirs + '/' + staticRelativePath;   //do not use path.combine or it will try and take the root
				lock (configLock)
				{
					var di1 = new DirectoryInfo(StaticDirs);
					var fi = new FileInfo(path);
					var di2 = new DirectoryInfo(fi.Directory.FullName);

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
						return "Cannot delete above static directories!";
					}

					if (fi.Exists)
						File.Delete(path);
					else if (Directory.Exists(path))
						Program.DeleteDirectory(path);
					TGServerService.CancelImpersonation();
					TGServerService.WriteInfo("Delete of " + path, TGServerService.EventID.StaticDelete);
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
				if (!Directory.Exists(StaticDirs))
				{
					error = null;
					unauthorized = false;
					return new List<string>();
				}
				DirectoryInfo dirToEnum = new DirectoryInfo(StaticDirs + '/' + subDir ?? "");	//do not use path.combine or it will try and take the root
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
