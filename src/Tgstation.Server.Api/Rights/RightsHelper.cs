using System;
using System.Collections.Generic;
using System.Globalization;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Helper for <see cref="RightsType"/>s
	/// </summary>
	public static class RightsHelper
	{
		/// <summary>
		/// Map of <see cref="RightsType"/>s to their respective flag <see cref="Enum"/>s
		/// </summary>
		static readonly IReadOnlyDictionary<RightsType, Type> TypeMap = new Dictionary<RightsType, Type>
		{
			{ RightsType.Administration, typeof(AdministrationRights) },
			{ RightsType.InstanceManager, typeof(InstanceManagerRights) },
			{ RightsType.Repository, typeof(RepositoryRights) },
			{ RightsType.Byond, typeof(ByondRights) },
			{ RightsType.DreamMaker, typeof(DreamMakerRights) },
			{ RightsType.DreamDaemon, typeof(DreamDaemonRights) },
			{ RightsType.ChatBots, typeof(ChatBotRights) },
			{ RightsType.Configuration, typeof(ConfigurationRights) },
			{ RightsType.InstancePermissionSet, typeof(InstancePermissionSetRights) }
		};

		/// <summary>
		/// Map a given <paramref name="rightsType"/> to its respective <see cref="Enum"/> <see cref="Type"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> to lookup</param>
		/// <returns>The <see cref="Enum"/> <see cref="Type"/> of the given <paramref name="rightsType"/></returns>
		public static Type RightToType(RightsType rightsType) => TypeMap[rightsType];

		/// <summary>
		/// Gets the role claim name used for a given <paramref name="right"/>
		/// </summary>
		/// <typeparam name="TRight">The <see cref="RightsType"/></typeparam>
		/// <param name="right">The <typeparamref name="TRight"/></param>
		/// <returns>A <see cref="string"/> representing the claim role name</returns>
		public static string RoleNames<TRight>(TRight right) where TRight : Enum
		{
			IEnumerable<string> GetRoleNames()
			{
				foreach (Enum J in Enum.GetValues(right.GetType()))
					if (Convert.ToInt32(J, CultureInfo.InvariantCulture) != 0 && right.HasFlag(J))
						yield return String.Concat(typeof(TRight).Name, '.', J.ToString());
			}

			var names = GetRoleNames();
			return String.Join(",", names);
		}

		/// <summary>
		/// Gets the role claim name used for a given <paramref name="rightsType"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/></param>
		/// <param name="right">The right value</param>
		/// <returns>A <see cref="string"/> representing the claim role name</returns>
		public static string RoleName(RightsType rightsType, Enum right)
		{
			var enumType = RightToType(rightsType);
			return String.Concat(enumType.Name, '.', Enum.GetName(enumType, right));
		}

		/// <summary>
		/// Check if a given <paramref name="rightsType"/> is meant for an <see cref="Models.Instance"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> to check</param>
		/// <returns><see langword="true"/> if <paramref name="rightsType"/> is an instance right, <see langword="false"/> otherwise</returns>
		public static bool IsInstanceRight(RightsType rightsType) => !(rightsType == RightsType.Administration || rightsType == RightsType.InstanceManager);

		/// <summary>
		/// Get all rights for a given <typeparamref name="TRight"/>.
		/// </summary>
		/// <typeparam name="TRight">The <see cref="RightsType"/>.</typeparam>
		/// <returns>All rights for the given <typeparamref name="TRight"/>.</returns>
		public static TRight AllRights<TRight>() where TRight : Enum
		{
			ulong rights = 0;
			foreach (Enum J in Enum.GetValues(typeof(TRight)))
				rights |= Convert.ToUInt64(J, CultureInfo.InvariantCulture);

			// cri evertim
			return (TRight)(object)rights;
		}

		/// <summary>
		/// Clamps a given set of <paramref name="rights"/> to their valid range.
		/// </summary>
		/// <typeparam name="TRight">The <see cref="RightsType"/>.</typeparam>
		/// <param name="rights">The <typeparamref name="TRight"/>s to clamp.</param>
		/// <returns>The clamped <paramref name="rights"/>.</returns>
		public static TRight Clamp<TRight>(TRight rights) where TRight : Enum
		{
			var allRights = AllRights<TRight>();

			var allAsUlong = Convert.ToUInt64(allRights, CultureInfo.InvariantCulture);
			var rightsAsUlong = Convert.ToUInt64(rights, CultureInfo.InvariantCulture);

			return (TRight)(object)(allAsUlong & rightsAsUlong);
		}
	}
}
