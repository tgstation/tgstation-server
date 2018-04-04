using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for the <see cref="Models.Compiler"/>
	/// </summary>
	[Flags]
	public enum CompilerRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User may read <see cref="Models.Compiler"/> status
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
		/// User may modify <see cref="Models.Compiler.AutoCompileInterval"/>
		/// </summary>
		SetAutoCompile = 8,
	}
}
