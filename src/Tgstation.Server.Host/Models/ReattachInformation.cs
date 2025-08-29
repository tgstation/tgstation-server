using System;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Session.ReattachInformation"/>.
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The <see cref="Models.CompileJob"/> for the <see cref="Components.Session.ReattachInformation.Dmb"/>.
		/// </summary>
		public CompileJob CompileJob { get; set; } = null!; // recommended by EF

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="CompileJob"/>.
		/// </summary>
		public long CompileJobId { get; set; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> the server was initially launched with in the case of Windows.
		/// </summary>
		public CompileJob? InitialCompileJob { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="InitialCompileJob"/>.
		/// </summary>
		public long? InitialCompileJobId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class.
		/// </summary>
		[Obsolete("For use by EFCore only", true)]
		public ReattachInformation()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class.
		/// </summary>
		/// <param name="accessIdentifier">The access identifier for the <see cref="ReattachInformationBase"/>.</param>
		public ReattachInformation(string accessIdentifier)
			: base(accessIdentifier)
		{
		}
	}
}
