namespace Tgstation.Server.Api
{
	/// <summary>
	/// Routes to a server actions
	/// </summary>
	public static class Routes
	{
		/// <summary>
		/// The root controller
		/// </summary>
		public const string Root = "/";

		/// <summary>
		/// The <see cref="Models.Administration"/> controller
		/// </summary>
		public const string Administration = Root + nameof(Models.Administration);
	}
}
