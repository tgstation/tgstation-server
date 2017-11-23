using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TGS.Server.IO;

namespace TGS.Server.Configuration
{
	/// <summary>
	/// Repository specific information for an <see cref="Components.Instance"/>
	/// </summary>
	sealed class RepoConfig : IRepoConfig
	{
		/// <summary>
		/// Name of the backing file
		/// </summary>
		const string JSONFilename = "TGS3.json";

		/// <inheritdoc />
		public bool ChangelogSupport { get; private set; }
		/// <inheritdoc />
		public string PathToChangelogPy { get; private set; }
		/// <inheritdoc />
		public string ChangelogPyArguments { get; private set; }
		/// <inheritdoc />
		public IReadOnlyList<string> PipDependancies { get; private set; }
		/// <inheritdoc />
		public IReadOnlyList<string> PathsToStage { get; private set; }
		/// <inheritdoc />
		public IReadOnlyList<string> StaticDirectoryPaths { get; private set; }
		/// <inheritdoc />
		public IReadOnlyList<string> DLLPaths { get; private set; }
		
		/// <summary>
		/// Copies the <see cref="RepoConfig"/> at <paramref name="source"/> to <paramref name="dest"/> overwriting any previous <see cref="RepoConfig"/> at <paramref name="dest"/>
		/// </summary>
		/// <param name="source">The source directory</param>
		/// <param name="dest">The destination directory</param>
		/// <param name="io">The <see cref="IIOManager"/> to use</param>
		public static void Copy(string source, string dest, IIOManager io)
		{
			io.CopyFile(Path.Combine(source, JSONFilename), Path.Combine(dest, JSONFilename), true);
		}

		/// <summary>
		/// Construct a <see cref="RepoConfig"/>
		/// </summary>
		/// <param name="path">Directory that contains the config JSON to use</param>
		/// <param name="io">The <see cref="IIOManager"/> to use for reading <paramref name="path"/></param>
		public RepoConfig(string path, IIOManager io)
		{
			path = Path.Combine(path, JSONFilename);
			if (!io.FileExists(path))
				return;
			var rawdata = io.ReadAllText(path).Result;
			var json = JsonConvert.DeserializeObject<IDictionary<string, object>>(rawdata);
			try
			{
				var details = ((JObject)json["changelog"]).ToObject<IDictionary<string, object>>();
				PathToChangelogPy = (string)details["script"];
				ChangelogPyArguments = (string)details["arguments"];
				ChangelogSupport = true;
				PipDependancies = LoadArray(details["pip_dependancies"]);
			}
			catch
			{
				ChangelogSupport = false;
				PipDependancies = new List<string>();
			}
			try
			{
				PathsToStage = LoadArray(json["synchronize_paths"]);
			}
			catch
			{
				PathsToStage = new List<string>();
			}
			try
			{
				StaticDirectoryPaths = LoadArray(json["static_directories"]);
			}
			catch
			{
				StaticDirectoryPaths = new List<string>();
			}
			try
			{
				DLLPaths = LoadArray(json["dlls"]);
			}
			catch
			{
				DLLPaths = new List<string>();
			}
		}

		/// <summary>
		/// Convert an array of <see cref="string"/>s to a <see cref="IList{T}"/> of <see cref="string"/>s
		/// </summary>
		/// <param name="o">The <see cref="string"/> array to convert</param>
		/// <returns></returns>
		private static IReadOnlyList<string> LoadArray(object o)
		{
			var res = new List<string>();
			foreach (var I in ((JArray)o))
				res.Add(I.ToObject<string>());
			return res;
		}

		/// <summary>
		/// Check if two <see cref="string"/> <see cref="IList{T}"/>s have the same contents
		/// </summary>
		/// <param name="A">The first <see cref="string"/> <see cref="IList{T}"/></param>
		/// <param name="B">The second <see cref="string"/> <see cref="IList{T}"/></param>
		/// <returns><see langword="true"/> if the <see cref="string"/> <see cref="IList{T}"/>s match, <see langword="false"/> otherwise</returns>

		static bool ListEquals(IReadOnlyList<string> A, IReadOnlyList<string> B)
		{
			return A.All(B.Contains) && A.Count == B.Count;
		}

		/// <summary>
		/// Check if <paramref name="other"/> matches <see langword="this"/> <see cref="RepoConfig"/>
		/// </summary>
		/// <param name="other">The other <see cref="IRepoConfig"/> to compare with</param>
		/// <returns><see langword="true"/> if <paramref name="other"/> == <see langword="this"/>, <see langword="false"/> otherwise</returns>
		public bool Equals(IRepoConfig other)
		{
			if (other == null)
				return false;
			return ChangelogSupport == other.ChangelogSupport
				&& PathToChangelogPy == other.PathToChangelogPy
				&& ChangelogPyArguments == other.ChangelogPyArguments
				&& ListEquals(PipDependancies, other.PipDependancies)
				&& ListEquals(PathsToStage, other.PathsToStage)
				&& ListEquals(StaticDirectoryPaths, other.StaticDirectoryPaths)
				&& ListEquals(DLLPaths, other.DLLPaths);
		}
	}
}
