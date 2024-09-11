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
		Read = 1 << 0,

		/// <summary>
		/// User may trigger deployments.
		/// </summary>
		Compile = 1 << 1,

		/// <summary>
		/// User may cancel deployment jobs.
		/// </summary>
		CancelCompile = 1 << 2,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ProjectName"/>.
		/// </summary>
		SetDme = 1 << 3,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ApiValidationPort"/>.
		/// </summary>
		SetApiValidationPort = 1 << 4,

		/// <summary>
		/// User may list and read all <see cref="Models.Internal.CompileJob"/>s.
		/// </summary>
		CompileJobs = 1 << 5,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.ApiValidationSecurityLevel"/>.
		/// </summary>
		SetSecurityLevel = 1 << 6,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.DMApiValidationMode"/> and <see cref="Models.Internal.DreamMakerSettings.RequireDMApiValidation"/>.
		/// </summary>
		SetApiValidationRequirement = 1 << 7,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.Timeout"/>.
		/// </summary>
		SetTimeout = 1 << 8,

		/// <summary>
		/// User may modify <see cref="Models.Internal.DreamMakerSettings.CompilerAdditionalArguments"/>.
		/// </summary>
		SetCompilerArguments = 1 << 9,
	}
}
