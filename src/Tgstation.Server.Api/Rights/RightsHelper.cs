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
			{ RightsType.Token, typeof(TokenRights) },

			{ RightsType.Repository, typeof(RepositoryRights) },
			{ RightsType.Byond, typeof(ByondRights) },
			{ RightsType.DreamMaker, typeof(DreamMakerRights) },
			{ RightsType.DreamDaemon, typeof(DreamDaemonRights) },
			{ RightsType.Chat, typeof(ChatRights) },
			{ RightsType.Configuration, typeof(ConfigurationRights) },
			{ RightsType.InstanceUser, typeof(InstanceUserRights) }
		};

		/// <summary>
		/// Map a given <paramref name="rightsType"/> to its respective <see cref="Enum"/> <see cref="Type"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> to lookup</param>
		/// <returns>The <see cref="Enum"/> <see cref="Type"/> of the given <paramref name="rightsType"/></returns>
		public static Type TypeToRight(RightsType rightsType) => typeMap[rightsType];
	}
}
