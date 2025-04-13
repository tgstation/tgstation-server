using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IPermissionSetAuthority" />
	sealed class PermissionSetAuthority : AuthorityBase, IPermissionSetAuthority
	{
		/// <summary>
		/// The <see cref="IPermissionSetsDataLoader"/> for the <see cref="PermissionSetAuthority"/>.
		/// </summary>
		readonly IPermissionSetsDataLoader permissionSetsDataLoader;

		/// <summary>
		/// Implements <see cref="permissionSetsDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyList{T}"/> of IDs and their <see cref="PermissionSetLookupType"/>s to load.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="PermissionSet"/>s.</returns>
		[DataLoader]
		public static async ValueTask<Dictionary<(long Id, PermissionSetLookupType LookupType), PermissionSet>> GetPermissionSets(
			IReadOnlyList<(long Id, PermissionSetLookupType LookupType)> ids,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(databaseContext);

			var idLookups = new List<long>(ids.Count);
			var userIdLookups = new List<long>(ids.Count);
			var groupIdLookups = new List<long>(ids.Count);

			foreach (var (id, lookupType) in ids)
				switch (lookupType)
				{
					case PermissionSetLookupType.Id:
						idLookups.Add(id);
						break;
					case PermissionSetLookupType.UserId:
						userIdLookups.Add(id);
						break;
					case PermissionSetLookupType.GroupId:
						groupIdLookups.Add(id);
						break;
					default:
						throw new InvalidOperationException($"Invalid {nameof(PermissionSetLookupType)}: {lookupType}");
				}

			var selectedPermissionSets = await databaseContext
				.PermissionSets
				.AsQueryable()
				.Where(dbModel => idLookups.Contains(dbModel.Id!.Value)
					|| (dbModel.UserId.HasValue && userIdLookups.Contains(dbModel.UserId.Value))
					|| (dbModel.GroupId.HasValue && groupIdLookups.Contains(dbModel.GroupId.Value)))
				.ToListAsync(cancellationToken);

			var results = new Dictionary<(long Id, PermissionSetLookupType LookupType), PermissionSet>(selectedPermissionSets.Count * 2);
			foreach (var permissionSet in selectedPermissionSets)
			{
				results.Add((permissionSet.Id!.Value, PermissionSetLookupType.Id), permissionSet);
				if (permissionSet.GroupId.HasValue)
					results.Add((permissionSet.GroupId.Value, PermissionSetLookupType.GroupId), permissionSet);
				if (permissionSet.UserId.HasValue)
					results.Add((permissionSet.UserId.Value, PermissionSetLookupType.UserId), permissionSet);
			}

			return results;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionSetAuthority"/> class.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to use.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="permissionSetsDataLoader">The value of <see cref="permissionSetsDataLoader"/>.</param>
		public PermissionSetAuthority(
			IAuthenticationContext authenticationContext,
			IDatabaseContext databaseContext,
			ILogger<AuthorityBase> logger,
			IPermissionSetsDataLoader permissionSetsDataLoader)
			: base(
				  authenticationContext,
				  databaseContext,
				  logger)
		{
			this.permissionSetsDataLoader = permissionSetsDataLoader ?? throw new ArgumentNullException(nameof(permissionSetsDataLoader));
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<PermissionSet>> GetId(long id, PermissionSetLookupType lookupType, CancellationToken cancellationToken)
		{
			if (id != AuthenticationContext.PermissionSet.Id && !((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid<PermissionSet>();

			var permissionSet = await permissionSetsDataLoader.LoadAsync((Id: id, LookupType: lookupType), cancellationToken);
			if (permissionSet == null)
				return NotFound<PermissionSet>();

			return new AuthorityResponse<PermissionSet>(permissionSet);
		}
	}
}
