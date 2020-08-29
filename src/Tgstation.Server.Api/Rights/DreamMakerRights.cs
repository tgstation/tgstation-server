using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.DreamMaker"/>
	/// </summary>
	[Flags]
	public enum DreamMakerRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may read all properties of <see cref="Models.DreamMaker"/>.
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
		/// User may modify <see cref="Models.DreamMaker.ProjectName"/>.
		/// </summary>
		SetDme = 8,

		/// <summary>
		/// User may modify <see cref="Models.DreamMaker.ApiValidationPort"/>.
		/// </summary>
		SetApiValidationPort = 16,

		/// <summary>
		/// User may list and read all <see cref="Models.CompileJob"/>s.
		/// </summary>
		CompileJobs = 32,

		/// <summary>
		/// User may modify <see cref="Models.DreamMaker.ApiValidationSecurityLevel"/>.
		/// </summary>
		SetSecurityLevel = 64,

		/// <summary>
		/// User may modify <see cref="Models.DreamMaker.RequireDMApiValidation"/>.
		/// </summary>
		SetApiValidationRequirement = 128,
	}
}
