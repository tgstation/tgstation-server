using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Session.ReattachInformation"/>.
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> for the <see cref="Components.Session.ReattachInformation.Dmb"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(compileJob))]
		public CompileJob CompileJob
		{
			get => compileJob ?? throw new InvalidOperationException("CompileJob not set!");
			set => compileJob = value;
		}

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="CompileJob"/>.
		/// </summary>
		public long CompileJobId { get; set; }

		/// <inheritdoc />
		[Required]
		[BackingField(nameof(compileJob))]
		public override string AccessIdentifier
		{
			get => accessIdentifier ?? throw new InvalidOperationException("AccessIdentifier not set!");
			set => accessIdentifier = value;
		}

		/// <summary>
		/// Backing field for <see cref="CompileJob"/>.
		/// </summary>
		CompileJob? compileJob;

		/// <summary>
		/// Backing field for <see cref="AccessIdentifier"/>.
		/// </summary>
		string? accessIdentifier;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class.
		/// </summary>
		/// <param name="copy">The copy <see cref="ReattachInformationBase"/>.</param>
		/// <param name="compileJobId">The value of <see cref="CompileJobId"/>.</param>
		public ReattachInformation(ReattachInformationBase copy, long compileJobId)
			: base(copy)
		{
			CompileJobId = compileJobId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformation"/> class.
		/// </summary>
		[Obsolete("For EFCore initialization.", true)]
		public ReattachInformation()
		{
		}
	}
}
