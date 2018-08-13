using System;
using System.Globalization;

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

		/// <summary>
		/// The <see cref="Models.User"/> controller
		/// </summary>
		public const string User = Root + nameof(Models.User);

		/// <summary>
		/// The <see cref="Models.Instance"/> controller
		/// </summary>
		public const string InstanceManager = Root + nameof(Models.Instance);

		/// <summary>
		/// Apply an <paramref name="id"/> postfix to a <paramref name="route"/>
		/// </summary>
		/// <param name="route">The route</param>
		/// <param name="id">The ID</param>
		/// <returns>The <paramref name="route"/> with <paramref name="id"/> appended</returns>
		public static string SetID(string route, long id) => String.Format(CultureInfo.InvariantCulture, "{0}/{1}", route, id);

		/// <summary>
		/// Get the /List postfix for a <paramref name="route"/>
		/// </summary>
		/// <param name="route">The route</param>
		/// <returns>The <paramref name="route"/> with /List appended</returns>
		public static string List(string route) => String.Format(CultureInfo.InvariantCulture, "{0}/List", route);
	}
}
