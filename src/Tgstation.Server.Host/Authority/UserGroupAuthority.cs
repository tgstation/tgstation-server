using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
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
		/// The <see cref="IClaimsPrincipalAccessor"/> for the <see cref="UserGroupAuthority"/>.
		/// </summary>
		readonly IClaimsPrincipalAccessor claimsPrincipalAccessor;

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
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="claimsPrincipalAccessor">The value of <see cref="claimsPrincipalAccessor"/>.</param>
		/// <param name="userGroupsDataLoader">The value of <see cref="userGroupsDataLoader"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		public UserGroupAuthority(
			IDatabaseContext databaseContext,
			ILogger<UserGroupAuthority> logger,
			IUserGroupsDataLoader userGroupsDataLoader,
			IClaimsPrincipalAccessor claimsPrincipalAccessor,
			IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
			: base(
				  databaseContext,
				  logger)
		{
			this.userGroupsDataLoader = userGroupsDataLoader ?? throw new ArgumentNullException(nameof(userGroupsDataLoader));
			this.claimsPrincipalAccessor = claimsPrincipalAccessor ?? throw new ArgumentNullException(nameof(claimsPrincipalAccessor));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<UserGroup>> GetId(long id, bool includeJoins, CancellationToken cancellationToken)
			=> new(
				() =>
				{
					if (id != claimsPrincipalAccessor.User.GetTgsUserId())
						return Flag(AdministrationRights.ReadUsers);

					return null;
				},
				async () =>
				{
					UserGroup? userGroup;
					if (includeJoins)
						userGroup = await QueryableImpl(true)
							.Where(x => x.Id == id)
							.FirstOrDefaultAsync(cancellationToken);
					else
						userGroup = await userGroupsDataLoader.LoadAsync(id, cancellationToken);

					if (userGroup == null)
						return Gone<UserGroup>();

					return new AuthorityResponse<UserGroup>(userGroup);
				});

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<UserGroup>> Read(CancellationToken cancellationToken)
			=> new(
				() => (IAuthorizationRequirement?)null,
				async () =>
				{
					var userId = claimsPrincipalAccessor.User.GetTgsUserId();
					var group = await DatabaseContext
						.Users
						.Where(user => user.Id == userId)
						.Select(user => user.Group)
						.FirstOrDefaultAsync(cancellationToken);

					if (group == null)
						return Gone<UserGroup>();

					return new AuthorityResponse<UserGroup>(group);
				});

		/// <inheritdoc />
		public RequirementsGated<IQueryable<UserGroup>> Queryable(bool includeJoins)
			=> new(
				() => Flag(AdministrationRights.ReadUsers),
				() => ValueTask.FromResult(QueryableImpl(includeJoins)));

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<UserGroup>> Create(string name, Models.PermissionSet? permissionSet, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(name);
			return new(
				() => Flag(AdministrationRights.WriteUsers),
				async () =>
				{
					var totalGroups = await DatabaseContext
						.Groups
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
				});
		}

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<UserGroup>> Update(long id, string? newName, Models.PermissionSet? newPermissionSet, CancellationToken cancellationToken)
			=> new(
				() => Flag(AdministrationRights.WriteUsers),
				async () =>
				{
					var currentGroup = await DatabaseContext
						.Groups
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
				});

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse> DeleteEmpty(long id, CancellationToken cancellationToken)
			=> new(
				() => Flag(AdministrationRights.WriteUsers),
				async () =>
				{
					var numDeleted = await DatabaseContext
						.Groups
						.Where(x => x.Id == id && x.Users!.Count == 0)
						.ExecuteDeleteAsync(cancellationToken);

					if (numDeleted > 0)
						return new();

					// find out how we failed
					var groupExists = await DatabaseContext
						.Groups
						.Where(x => x.Id == id)
						.AnyAsync(cancellationToken);

					return new(
						groupExists
							? new ErrorMessageResponse(ErrorCode.UserGroupNotEmpty)
							: new ErrorMessageResponse(),
						groupExists
							? HttpFailureResponse.Conflict
							: HttpFailureResponse.Gone);
				});

		/// <summary>
		/// Get the <see cref="IQueryable{T}"/> <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="includeJoins">If <see cref="UserGroup.Users"/> and <see cref="UserGroup.PermissionSet"/> should be included.</param>
		/// <returns>An <see cref="IQueryable{T}"/> of <see cref="UserGroup"/>s.</returns>
		IQueryable<UserGroup> QueryableImpl(bool includeJoins)
		{
			IQueryable<UserGroup> queryable = DatabaseContext
				.Groups;

			if (includeJoins)
				queryable = queryable
					.Include(x => x.Users)
					.Include(x => x.PermissionSet);

			return queryable;
		}
	}
}
