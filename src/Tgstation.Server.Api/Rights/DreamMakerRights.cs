using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.DreamMaker"/>
	/// </summary>
	[Flags]
	public enum DreamMakerRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User may read <see cref="Models.DreamMaker"/> status
		/// </summary>
		Read = 1,
		/// <summary>
		/// User may trigger compiles
		/// </summary>
		Compile = 2,
		/// <summary>
		/// User may cancel compiles
		/// </summary>
		CancelCompile = 4,
		/// <summary>
		/// User may modify <see cref="Models.DreamMaker.AutoCompileInterval"/>
		/// </summary>
		SetAutoCompile = 8,
	}
}
