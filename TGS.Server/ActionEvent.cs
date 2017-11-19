namespace TGS.Server
{
	/// <summary>
	/// Events available to be used with an <see cref="Components.IActionEventManager"/>
	/// </summary>
	static class ActionEvent
	{
		/// <summary>
		/// Run before dm.exe is run on the .dme
		/// </summary>
		public const string Precompile = "precompile";
		/// <summary>
		/// Run after dm.exe is run on the .dme
		/// </summary>
		public const string Postcompile = "postcompile";
	}
}
