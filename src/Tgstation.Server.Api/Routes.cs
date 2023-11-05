using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Routes to a server actions.
	/// </summary>
	public static class Routes
	{
		/// <summary>
		/// The root controller.
		/// </summary>
		public const string Root = "/";

		/// <summary>
		/// The server administration controller.
		/// </summary>
		public const string Administration = Root + "Administration";

		/// <summary>
		/// The endpoint to download server logs.
		/// </summary>
		public const string Logs = Administration + "/Logs";

		/// <summary>
		/// The user controller.
		/// </summary>
		public const string User = Root + "User";

		/// <summary>
		/// The user group controller.
		/// </summary>
		public const string UserGroup = Root + "UserGroup";

		/// <summary>
		/// The <see cref="Models.Instance"/> controller.
		/// </summary>
		public const string InstanceManager = Root + "Instance";

		/// <summary>
		/// The engine controller.
		/// </summary>
		public const string Engine = Root + "Engine";

		/// <summary>
		/// The git repository controller.
		/// </summary>
		public const string Repository = Root + "Repository";

		/// <summary>
		/// The DreamDaemon controller.
		/// </summary>
		public const string DreamDaemon = Root + "DreamDaemon";

		/// <summary>
		/// For accessing DD diagnostics.
		/// </summary>
		public const string Diagnostics = DreamDaemon + "/Diagnostics";

		/// <summary>
		/// The configuration controller.
		/// </summary>
		public const string Configuration = Root + "Config";

		/// <summary>
		/// To be paired with <see cref="Configuration"/> for accessing <see cref="Models.IConfigurationFile"/>s.
		/// </summary>
		public const string File = "File";

		/// <summary>
		/// Full combination of <see cref="Configuration"/> and <see cref="File"/>.
		/// </summary>
		public const string ConfigurationFile = Configuration + "/" + File;

		/// <summary>
		/// The instance permission set controller.
		/// </summary>
		public const string InstancePermissionSet = Root + "InstancePermissionSet";

		/// <summary>
		/// The chat bot controller.
		/// </summary>
		public const string Chat = Root + "Chat";

		/// <summary>
		/// The deployment controller.
		/// </summary>
		public const string DreamMaker = Root + "DreamMaker";

		/// <summary>
		/// The jobs controller.
		/// </summary>
		public const string Jobs = Root + "Job";

		/// <summary>
		/// The transfer controller.
		/// </summary>
		public const string Transfer = Root + "Transfer";

		/// <summary>
		/// The postfix for list operations.
		/// </summary>
		public const string List = "List";

		/// <summary>
		/// Apply an <paramref name="id"/> postfix to a <paramref name="route"/>.
		/// </summary>
		/// <param name="route">The route.</param>
		/// <param name="id">The ID.</param>
		/// <returns>The <paramref name="route"/> with <paramref name="id"/> appended.</returns>
		public static string SetID(string route, long id) => $"{route}/{id}";

		/// <summary>
		/// Get the /List postfix for a <paramref name="route"/>.
		/// </summary>
		/// <param name="route">The route.</param>
		/// <returns>The <paramref name="route"/> with /List appended.</returns>
		public static string ListRoute(string route) => $"{route}/{List}";

		/// <summary>
		/// Sanitize a <see cref="Models.Response.FileTicketResponse"/> path for use in a GET <see cref="Uri"/>.
		/// </summary>
		/// <param name="path">The path to sanitize.</param>
		/// <returns>The sanitized path.</returns>
		public static string SanitizeGetPath(string path)
		{
			path ??= String.Empty;
			if (path.Length == 0 || path[0] != '/')
				path = '/' + path;
			return path;
		}
	}
}
