namespace TGS.Server.Components
{
	/// <summary>
	/// Framework for managing events that can be triggered at various points in the <see cref="Instance"/> workflow
	/// </summary>
	interface IActionEventManager
	{
		/// <summary>
		/// Runs an <see cref="ActionEvent"/> named <paramref name="eventName"/> if it exists
		/// </summary>
		/// <param name="eventName">One of the <see cref="ActionEvent"/>s</param>
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		bool HandleEvent(string eventName);
	}
}
