using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IUserGroupAuthority" />
	sealed class UserGroupAuthority : AuthorityBase, IUserGroupAuthority
	{
		/// <summary>
		/// The <see cref="IUserGroupsDataLoader"/> for the <see cref="UserGroupAuthority"/>.
		/// </summary>
		readonly IUserGroupsDataLoader userGroupsDataLoader;

		/// <summary>
		/// The <see cref="IOptionsSnapshot{TOptions}"/> of the <see cref="GeneralConfiguration"/>.
		/// </summary>
		readonly IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// Implements the <see cref="userGroupsDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyList{T}"/> of <see cref="UserGroup"/> <see cref="EntityId.Id"/>s to load.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="UserGroup"/>s.</returns>
		[DataLoader]
		public static Task<Dictionary<long, UserGroup>> GetUserGroups(
			IReadOnlyList<long> ids,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(databaseContext);

			return databaseContext
				.Groups
				.Where(group => ids.Contains(group.Id!.Value))
				.ToDictionaryAsync(userGroup => userGroup.Id!.Value, cancellationToken);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupAuthority"/> class.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to use.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="userGroupsDataLoader">The value of <see cref="userGroupsDataLoader"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		public UserGroupAuthority(
			IAuthenticationContext authenticationContext,
			IDatabaseContext databaseContext,
			ILogger<UserGroupAuthority> logger,
			IUserGroupsDataLoader userGroupsDataLoader,
			IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
			: base(
				  authenticationContext,
				  databaseContext,
				  logger)
		{
			this.userGroupsDataLoader = userGroupsDataLoader ?? throw new ArgumentNullException(nameof(userGroupsDataLoader));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<UserGroup>> GetId(long id, bool includeJoins, CancellationToken cancellationToken)
		{
			if (id != AuthenticationContext.User.GroupId && !((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid<UserGroup>();

			UserGroup? userGroup;
			if (includeJoins)
				userGroup = await Queryable(true)
					.Where(x => x.Id == id)
					.FirstOrDefaultAsync(cancellationToken);
			else
				userGroup = await userGroupsDataLoader.LoadAsync(id, cancellationToken);

			if (userGroup == null)
				return Gone<UserGroup>();

			return new AuthorityResponse<UserGroup>(userGroup);
		}

		/// <inheritdoc />
		public ValueTask<AuthorityResponse<UserGroup>> Read()
		{
			var group = AuthenticationContext.User!.Group;
			if (group == null)
				return ValueTask.FromResult(Gone<UserGroup>());

			return ValueTask.FromResult(new AuthorityResponse<UserGroup>(group));
		}

		/// <inheritdoc />
		public IQueryable<UserGroup> Queryable(bool includeJoins)
		{
			var queryable = DatabaseContext
				.Groups
				.AsQueryable();

			if (includeJoins)
				queryable = queryable
					.Include(x => x.Users)
					.Include(x => x.PermissionSet);

			return queryable;
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<UserGroup>> Create(string name, Models.PermissionSet? permissionSet, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(name);

			var totalGroups = await DatabaseContext
				.Groups
				.AsQueryable()
				.CountAsync(cancellationToken);
			if (totalGroups >= generalConfigurationOptions.Value.UserGroupLimit)
				return Conflict<UserGroup>(ErrorCode.UserGroupLimitReached);

			var modelPermissionSet = new Models.PermissionSet
			{
				AdministrationRights = permissionSet?.AdministrationRights ?? AdministrationRights.None,
				InstanceManagerRights = permissionSet?.InstanceManagerRights ?? InstanceManagerRights.None,
			};

			var dbGroup = new UserGroup
			{
				Name = name,
				PermissionSet = modelPermissionSet,
			};

			DatabaseContext.Groups.Add(dbGroup);
			await DatabaseContext.Save(cancellationToken);
			Logger.LogInformation("Created new user group {groupName} ({groupId})", dbGroup.Name, dbGroup.Id);

			return new AuthorityResponse<UserGroup>(
				dbGroup,
				HttpSuccessResponse.Created);
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<UserGroup>> Update(long id, string? newName, Models.PermissionSet? newPermissionSet, CancellationToken cancellationToken)
		{
			var currentGroup = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.Include(x => x.PermissionSet)
				.FirstOrDefaultAsync(cancellationToken);

			if (currentGroup == default)
				return Gone<UserGroup>();

			if (newPermissionSet != null)
			{
				currentGroup.PermissionSet!.AdministrationRights = newPermissionSet.AdministrationRights ?? currentGroup.PermissionSet.AdministrationRights;
				currentGroup.PermissionSet.InstanceManagerRights = newPermissionSet.InstanceManagerRights ?? currentGroup.PermissionSet.InstanceManagerRights;
			}

			currentGroup.Name = newName ?? currentGroup.Name;

			await DatabaseContext.Save(cancellationToken);

			return new AuthorityResponse<UserGroup>(currentGroup);
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse> DeleteEmpty(long id, CancellationToken cancellationToken)
		{
			var numDeleted = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id && x.Users!.Count == 0)
				.ExecuteDeleteAsync(cancellationToken);

			if (numDeleted > 0)
				return new();

			// find out how we failed
			var groupExists = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.AnyAsync(cancellationToken);

			return new(
				groupExists
					? new ErrorMessageResponse(ErrorCode.UserGroupNotEmpty)
					: new ErrorMessageResponse(),
				groupExists
					? HttpFailureResponse.Conflict
					: HttpFailureResponse.Gone);
		}
	}
}
