using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Many to many relationship for <see cref="Models.RevisionInformation"/> and <see cref="Models.TestMerge"/>.
	/// </summary>
	public sealed class RevInfoTestMerge
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.TestMerge"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(testMerge))]
		public TestMerge TestMerge
		{
			get => testMerge ?? throw new InvalidOperationException("TestMerge was null!");
			set => testMerge = value;
		}

		/// <summary>
		/// The <see cref="Models.RevisionInformation"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(revisionInformation))]
		public RevisionInformation RevisionInformation
		{
			get => revisionInformation ?? throw new InvalidOperationException("RevisionInformation was null!");
			set => revisionInformation = value;
		}

		/// <summary>
		/// Backing field for <see cref="TestMerge"/>.
		/// </summary>
		TestMerge? testMerge;

		/// <summary>
		/// Backing field for <see cref="TestMerge"/>.
		/// </summary>
		RevisionInformation? revisionInformation;
	}
}
