using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for deployment.
	/// </summary>
	[Flags]
	public enum DreamMakerRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may read all properties of <see cref="Models.Internal.DreamMakerSettings"/>.
		/// </summary>
		Read = 1,

		/// <summary>
		/// User may trigger deployments.
		/// </summary>
		Compile = 2,

		/// <summary>
		/// User may cancel deployment jobs.
		/// </summary>
		CancelCompile = 4,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ProjectName"/>.
		/// </summary>
		SetDme = 8,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ApiValidationPort"/>.
		/// </summary>
		SetApiValidationPort = 16,

		/// <summary>
		/// User may list and read all <see cref="Models.Internal.CompileJob"/>s.
		/// </summary>
		CompileJobs = 32,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ApiValidationSecurityLevel"/>.
		/// </summary>
		SetSecurityLevel = 64,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.RequireDMApiValidation"/>.
		/// </summary>
		SetApiValidationRequirement = 128,
	}
}
