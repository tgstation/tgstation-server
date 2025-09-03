using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Routes to a server actions.
	/// </summary>
	public static class Routes
	{
		/// <summary>
		/// The root of API methods.
		/// </summary>
		public const string ApiRoot = "/api/";

		/// <summary>
		/// The GraphQL route.
		/// </summary>
		public const string GraphQL = ApiRoot + "graphql";

		/// <summary>
		/// The root route of all hubs.
		/// </summary>
		public const string HubsRoot = ApiRoot + "hubs";

		/// <summary>
		/// The server administration controller.
		/// </summary>
		public const string Administration = ApiRoot + "Administration";

		/// <summary>
		/// The endpoint to download server logs.
		/// </summary>
		public const string Logs = Administration + "/Logs";

		/// <summary>
		/// The user controller.
		/// </summary>
		public const string User = ApiRoot + "User";

		/// <summary>
		/// The user group controller.
		/// </summary>
		public const string UserGroup = ApiRoot + "UserGroup";

		/// <summary>
		/// The <see cref="Models.Instance"/> controller.
		/// </summary>
		public const string InstanceManager = ApiRoot + "Instance";

		/// <summary>
		/// The engine controller.
		/// </summary>
		public const string Engine = ApiRoot + "Engine";

		/// <summary>
		/// The git repository controller.
		/// </summary>
		public const string Repository = ApiRoot + "Repository";

		/// <summary>
		/// The DreamDaemon controller.
		/// </summary>
		public const string DreamDaemon = ApiRoot + "DreamDaemon";

		/// <summary>
		/// For accessing DD diagnostics.
		/// </summary>
		public const string Diagnostics = DreamDaemon + "/Diagnostics";

		/// <summary>
		/// The configuration controller.
		/// </summary>
		public const string Configuration = ApiRoot + "Config";

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
		public const string InstancePermissionSet = ApiRoot + "InstancePermissionSet";

		/// <summary>
		/// The chat bot controller.
		/// </summary>
		public const string Chat = ApiRoot + "Chat";

		/// <summary>
		/// The deployment controller.
		/// </summary>
		public const string DreamMaker = ApiRoot + "DreamMaker";

		/// <summary>
		/// The jobs controller.
		/// </summary>
		public const string Jobs = ApiRoot + "Job";

		/// <summary>
		/// The transfer controller.
		/// </summary>
		public const string Transfer = ApiRoot + "Transfer";

		/// <summary>
		/// The postfix for list operations.
		/// </summary>
		public const string List = "List";

		/// <summary>
		/// The postfix for deploy operations.
		/// </summary>
		public const string Deploy = "Deploy";

		/// <summary>
		/// The postfix for launch operations.
		/// </summary>
		public const string Launch = "Launch";

		/// <summary>
		/// The postfix for create operations.
		/// </summary>
		public const string Create = "Create";

		/// <summary>
		/// The postfix for delete operations.
		/// </summary>
		public const string Delete = "Delete";

		/// <summary>
		/// The postfix for reclone operations.
		/// </summary>
		public const string Reclone = "Reclone";

		/// <summary>
		/// The postfix for restart operations.
		/// </summary>
		public const string Restart = "Restart";

		/// <summary>
		/// The postfix for grant operations.
		/// </summary>
		public const string Grant = "Grant";

		/// <summary>
		/// The root route of all hubs.
		/// </summary>
		public const string JobsHub = HubsRoot + "/jobs";

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
