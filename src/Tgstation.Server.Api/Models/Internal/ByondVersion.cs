using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Information about a Byond installation.
	/// </summary>
	public class ByondVersion : IEquatable<ByondVersion>
	{
		/// <summary>
		/// The <see cref="EngineType"/>.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public EngineType? Engine { get; set; }

		/// <summary>
		/// The <see cref="System.Version"/> of the engine. Currently only valid when <see cref="Engine"/> is <see cref="EngineType.Byond"/>.
		/// </summary>
		[ResponseOptions]
		public Version? Version { get; set; }

		/// <summary>
		/// The git committish of the engine. On response, this will always be a commit SHA. Currently only valid when <see cref="Engine"/> is <see cref="EngineType.Byond"/>.
		/// </summary>
		[ResponseOptions]
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? SourceCommittish { get; set; }

		/// <summary>
		/// Parses a stringified <see cref="ByondVersion"/>.
		/// </summary>
		/// <param name="input">The input <see cref="string"/>.</param>
		/// <param name="byondVersion">The output <see cref="ByondVersion"/>.</param>
		/// <returns><see langword="true"/> if parsing was successful, <see langword="false"/> otherwise.</returns>
		public static bool TryParse(string input, out ByondVersion? byondVersion)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var splits = input.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
			byondVersion = null;

			if (splits.Length > 2)
				return false;

			EngineType engine;
			if (splits.Length > 1)
			{
				if (!Enum.TryParse(splits[0], out engine))
					return false;
			}
			else
				engine = EngineType.Byond;

			if (!Version.TryParse(splits.Last(), out var version))
				return false;

			byondVersion = new ByondVersion
			{
				Engine = engine,
				Version = version,
			};
			return true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondVersion"/> class.
		/// </summary>
		public ByondVersion()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondVersion"/> class.
		/// </summary>
		/// <param name="other">The <see cref="ByondVersion"/> to copy.</param>
		public ByondVersion(ByondVersion other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			Version = other.Version;
			Engine = other.Engine;
			SourceCommittish = other.SourceCommittish;
		}

		/// <inheritdoc />
		public bool Equals(ByondVersion other)
		{
			// https://github.com/dotnet/roslyn-analyzers/issues/2875
#pragma warning disable CA1062 // Validate arguments of public methods
			return other!.Version == Version
				&& other.Engine == Engine;
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
			=> obj is ByondVersion other && Equals(other);

		/// <inheritdoc />
		public override string ToString() => $"{(Engine != EngineType.Byond ? $"{Engine}-" : String.Empty)}{Version}"; // BYOND isn't display for backwards compatibility. SourceCommittish is not included

		/// <inheritdoc />
		public override int GetHashCode() => ToString().GetHashCode();
	}
}
