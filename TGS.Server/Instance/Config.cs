﻿using System;
using System.Collections.Generic;
using System.IO;
using TGS.Interface.Components;
using TGS.Server.Security;

namespace TGS.Server
{
	//knobs and such
	sealed partial class Instance : ITGConfig
	{
		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object configLock = new object();   //for atomic reads/writes

		/// <inheritdoc />
		public string ReadText(string staticRelativePath, bool repo, out string error, out bool unauthorized)
		{
			string path = null;
			try
			{
				var configDir = RelativePath(repo ? RepoPath : StaticDirs);

				path = configDir + '/' + staticRelativePath;   //do not use path.combine or it will try and take the root
				lock (configLock)
				{
					var di1 = new DirectoryInfo(configDir);
					if (repo)
					{
						//ensure we aren't trying to read anything outside the static dirs
						var Config = GetCachedRepoConfig();
						if (Config == null)
						{
							error = "Unable to load static directory configuration";
							unauthorized = false;
							return null;
						}
						var Found = false;
						foreach (var I in Config.StaticDirectoryPaths)
						{
							if (di1.FullName == new DirectoryInfo(Path.Combine(RelativePath(RepoPath), I)).FullName)
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

					string output;
					using (IdentityManager.Impersonate())
						output = File.ReadAllText(path);
					WriteInfo("Read of " + path, EventID.StaticRead);
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
				WriteWarning(String.Format("Read of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				unauthorized = false;
				return null;
			}
		}

		/// <inheritdoc />
		public string WriteText(string staticRelativePath, string data, out bool unauthorized)
		{
			var path = RelativePath(StaticDirs) + '/' + staticRelativePath;   //do not use path.combine or it will try and take the root
			try
			{
				lock (configLock)
				{
					var di1 = new DirectoryInfo(RelativePath(StaticDirs));
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

					using (IdentityManager.Impersonate())
					{
						Directory.CreateDirectory(destdir);
						File.WriteAllText(path, data);
					}
					WriteInfo("Write to " + path, EventID.StaticWrite);
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
				WriteWarning(String.Format("Write of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}
		/// <inheritdoc />
		public string DeleteFile(string staticRelativePath, out bool unauthorized)
		{
			var path = RelativePath(StaticDirs + '/' + staticRelativePath);   //do not use path.combine or it will try and take the root
			try
			{
				lock (configLock)
				{
					var di1 = new DirectoryInfo(RelativePath(StaticDirs));
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

					using (IdentityManager.Impersonate())
						if (fi.Exists)
							File.Delete(path);
						else if (Directory.Exists(path))
							Helpers.DeleteDirectory(path);

					WriteInfo("Delete of " + path, EventID.StaticDelete);
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
				WriteWarning(String.Format("Delete of {0} failed! Error: {1}", path, e.ToString()), EventID.StaticRead);
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public IList<string> ListStaticDirectory(string subDir, out string error, out bool unauthorized)
		{
			using (IdentityManager.Impersonate())
				try
				{
					if (!Directory.Exists(RelativePath(StaticDirs)))
					{
						error = null;
						unauthorized = false;
						return new List<string>();
					}
					DirectoryInfo dirToEnum = new DirectoryInfo(RelativePath(StaticDirs) + '/' + subDir ?? ""); //do not use path.combine or it will try and take the root
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
