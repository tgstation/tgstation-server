using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Information about an engine installation.
	/// </summary>
	public sealed class EngineVersion : IEquatable<EngineVersion>
	{
		/// <summary>
		/// An array of a single '-' <see cref="char"/>.
		/// </summary>
		static readonly char[] DashChar = ['-'];

		/// <summary>
		/// The <see cref="EngineType"/>.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public EngineType? Engine { get; set; }

		/// <summary>
		/// The <see cref="System.Version"/> of the engine. Currently only valid when <see cref="Engine"/> is <see cref="EngineType.Byond"/>.
		/// </summary>
		/// <example>516.1651.0</example>
		[ResponseOptions]
		public Version? Version { get; set; }

		/// <summary>
		/// The git commit SHA of the engine. Currently only valid when <see cref="Engine"/> is <see cref="EngineType.OpenDream"/>.
		/// </summary>
		/// <example>caa1e1f400c8b6a535e03cff28cf57f919e9378c</example>
		[ResponseOptions]
		[StringLength(Limits.MaximumCommitShaLength, MinimumLength = Limits.MaximumCommitShaLength)]
		public string? SourceSHA { get; set; }

		/// <summary>
		/// The revision of the custom build.
		/// </summary>
		[ResponseOptions]
		public int? CustomIteration { get; set; }

		/// <summary>
		/// Attempts to parse a stringified <see cref="EngineVersion"/>.
		/// </summary>
		/// <param name="input">The input <see cref="string"/>.</param>
		/// <param name="engineVersion">The output <see cref="EngineVersion"/>.</param>
		/// <returns><see langword="true"/> if parsing was successful, <see langword="false"/> otherwise.</returns>
		public static bool TryParse(string input, out EngineVersion? engineVersion)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var splits = input.Split(DashChar, StringSplitOptions.RemoveEmptyEntries);
			engineVersion = null;

			var length = splits.Length;
			if (length == 0 || length > 3)
				return false;

			EngineType engine;
			var hasPrefix = splits.Length > 1;
			if (hasPrefix)
			{
				if (!Enum.TryParse(splits[0], out engine))
					return false;
			}
			else
				engine = EngineType.Byond;

			Version? version;
			string? sha;
			int? customRev = null;
			if (engine == EngineType.Byond)
			{
				if (!Version.TryParse(splits.Last(), out version))
					return false;

				if (version.Build > 0)
				{
					customRev = version.Build;
					version = new Version(version.Major, version.Minor);
				}

				sha = null;
			}
			else
			{
				Debug.Assert(engine == EngineType.OpenDream, "This does not support whatever ungodly new engine you've added");

				var shaIndex = hasPrefix ? 1 : 0;
				sha = splits[shaIndex];
				if (sha.Length != Limits.MaximumCommitShaLength)
					return false;

				version = null;

				if (splits.Length - 1 > shaIndex)
				{
					if (!Int32.TryParse(splits.Last(), out var customRevResult))
						return false;

					customRev = customRevResult;
				}
			}

			engineVersion = new EngineVersion
			{
				Engine = engine,
				Version = version,
				SourceSHA = sha,
				CustomIteration = customRev,
			};
			return true;
		}

		/// <summary>
		/// Parses a stringified <see cref="EngineVersion"/>.
		/// </summary>
		/// <param name="input">The input <see cref="string"/>.</param>
		/// <returns>The output <see cref="EngineVersion"/>.</returns>
		/// <exception cref="InvalidOperationException">If the <paramref name="input"/> is not a valid stringified <see cref="EngineVersion"/>.</exception>
		public static EngineVersion Parse(string input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			if (TryParse(input, out var engineVersion))
				return engineVersion!;

			throw new InvalidOperationException($"Invalid engine version: {input}");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineVersion"/> class.
		/// </summary>
		public EngineVersion()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineVersion"/> class.
		/// </summary>
		/// <param name="other">The <see cref="EngineVersion"/> to copy.</param>
		public EngineVersion(EngineVersion other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			Version = other.Version;
			Engine = other.Engine;
			SourceSHA = other.SourceSHA;
			CustomIteration = other.CustomIteration;
		}

		/// <inheritdoc />
		public bool Equals(EngineVersion other)
		{
			// https://github.com/dotnet/roslyn-analyzers/issues/2875
#pragma warning disable CA1062 // Validate arguments of public methods
			return other!.Version?.Semver() == Version?.Semver()
				&& other.Engine == Engine
				&& (other.SourceSHA == SourceSHA
				|| (other.SourceSHA != null
					&& SourceSHA != null
					&& other.SourceSHA.Equals(SourceSHA, StringComparison.OrdinalIgnoreCase)))
				&& other.CustomIteration == CustomIteration;
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
			=> obj is EngineVersion other && Equals(other);

		/// <inheritdoc />
		public override string ToString()
		{
			var isByond = Engine == EngineType.Byond;

			// BYOND encodes differently for backwards compatibility
			var enginePrefix = !isByond
				? $"{Engine}-"
				: String.Empty;
			var displayedVersion = isByond
				? (CustomIteration.HasValue
					? new Version(Version!.Major, Version.Minor, CustomIteration.Value)
					: Version!).ToString()
				: SourceSHA;
			var displayedCustomIteration = !isByond && CustomIteration.HasValue
				? $"-{CustomIteration}"
				: String.Empty;
			return $"{enginePrefix}{displayedVersion}{displayedCustomIteration}";
		}

		/// <inheritdoc />
		public override int GetHashCode() => ToString().GetHashCode();
	}
}
