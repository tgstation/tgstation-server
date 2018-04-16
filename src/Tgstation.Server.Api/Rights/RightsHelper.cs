using System;
using System.Collections.Generic;

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
		static readonly IReadOnlyDictionary<RightsType, Type> typeMap = new Dictionary<RightsType, Type>
		{
			{ RightsType.Administration, typeof(AdministrationRights) },
			{ RightsType.InstanceManager, typeof(InstanceManagerRights) },

			{ RightsType.Repository, typeof(RepositoryRights) },
			{ RightsType.Byond, typeof(ByondRights) },
			{ RightsType.DreamMaker, typeof(DreamMakerRights) },
			{ RightsType.DreamDaemon, typeof(DreamDaemonRights) },
			{ RightsType.Chat, typeof(ChatSettingsRights) },
			{ RightsType.Configuration, typeof(ConfigurationRights) },
			{ RightsType.InstanceUser, typeof(InstanceUserRights) }
		};

		static readonly IReadOnlyDictionary<Type, RightsType> rightMap = CreateRightsMap();

		static IReadOnlyDictionary<Type, RightsType> CreateRightsMap()
		{
			var dic = new Dictionary<Type, RightsType>();
			foreach (var I in typeMap)
				dic.Add(I.Value, I.Key);
			return dic;
		}

		/// <summary>
		/// Map a given <paramref name="rightsType"/> to its respective <see cref="Enum"/> <see cref="Type"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> to lookup</param>
		/// <returns>The <see cref="Enum"/> <see cref="Type"/> of the given <paramref name="rightsType"/></returns>
		public static Type RightToType(RightsType rightsType) => typeMap[rightsType];

		public static string RoleName<TRight>(TRight right) => String.Concat(typeof(TRight).Name, '.', right.ToString());
		public static string RoleName(RightsType rightsType, int right)
		{
			var enumType = typeMap[rightsType];
			return String.Concat(enumType.Name, '.', Enum.GetName(enumType, right));
		}

		/// <summary>
		/// Check if a given <paramref name="rightsType"/> is meant for an <see cref="Models.Instance"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> to check</param>
		/// <returns><see langword="true"/> if <paramref name="rightsType"/> is an instance right, <see langword="false"/> otherwise</returns>
		public static bool IsInstanceRight(RightsType rightsType) => !(rightsType == RightsType.Administration || rightsType == RightsType.InstanceManager);
	}
}
