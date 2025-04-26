using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Security.RightsEvaluation;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="AuthorizationHandler{TRequirement}"/> for <see cref="RightsConditional{TRights}"/>s.
	/// </summary>
	/// <typeparam name="TRights">The <typeparamref name="TRights"/> to evaluate.</typeparam>
	public sealed class RightsAuthorizationHandler<TRights> : AuthorizationHandler<RightsConditional<TRights>>
		where TRights : Enum
	{
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="RightsAuthorizationHandler{TRights}"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="IApiHeadersProvider"/> for the <see cref="RightsAuthorizationHandler{TRights}"/>.
		/// </summary>
		readonly IApiHeadersProvider apiHeadersProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="RightsAuthorizationHandler{TRights}"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="apiHeadersProvider">The value of <see cref="apiHeadersProvider"/>.</param>
		public RightsAuthorizationHandler(IDatabaseContext databaseContext, IApiHeadersProvider apiHeadersProvider)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.apiHeadersProvider = apiHeadersProvider ?? throw new ArgumentNullException(nameof(apiHeadersProvider));
		}

		/// <inheritdoc />
		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RightsConditional<TRights> requirement)
		{
			// https://github.com/dotnet/aspnetcore/issues/56272
			CancellationToken cancellationToken = CancellationToken.None;

			ArgumentNullException.ThrowIfNull(context);
			ArgumentNullException.ThrowIfNull(requirement);

			var rightsType = RightsHelper.TypeToRight<TRights>();
			var isInstance = RightsHelper.IsInstanceRight(rightsType);
			var userId = context.User.GetTgsUserId();

			object? permissionSet;
			if (isInstance)
			{
				var apiHeaders = apiHeadersProvider.ApiHeaders;
				if (apiHeaders == null)
					throw new InvalidOperationException("API headers should have been validated at this point!");

				if (!apiHeaders.InstanceId.HasValue)
					throw new InvalidOperationException("Instance ID header should have been validated at this point!");

				var queryableUsers = databaseContext
					.Users
					.AsQueryable();

				var matchingUniquePermissionSetIds = queryableUsers
					.Where(user => user.Id == userId && user.PermissionSet != null)
					.Select(user => user.PermissionSet!.Id);

				var matchingGroupPermissionSetIds = queryableUsers
					.Where(user => user.Id == userId && user.Group != null)
					.Select(user => user.Group!.PermissionSet!.Id);

				permissionSet = await databaseContext
					.InstancePermissionSets
					.AsQueryable()
					.Where(ips => ips.InstanceId == apiHeaders.InstanceId.Value
						&& (matchingUniquePermissionSetIds.Contains(ips.PermissionSetId) || matchingGroupPermissionSetIds.Contains(ips.PermissionSetId)))
					.TagWith("rights_authorization_handler_instance_permission_set")
					.FirstOrDefaultAsync(cancellationToken);
			}
			else
				permissionSet = await databaseContext
					.PermissionSets
					.AsQueryable()
					.Where(permissionSet => permissionSet.UserId == userId)
					.TagWith("rights_authorization_handler_permission_set")
					.FirstOrDefaultAsync(cancellationToken);

			if (permissionSet == null)
				return; // fail

			// use the api versions because they're the ones that contain the actual properties
			var requiredPermissionSetType = isInstance ? typeof(InstancePermissionSet) : typeof(PermissionSet);

			var rightsClrType = typeof(TRights);
			var nullableRightsType = typeof(Nullable<>).MakeGenericType(rightsClrType);

			var rightPropertyInfo = requiredPermissionSetType
				.GetProperties()
				.Where(propertyInfo => propertyInfo.PropertyType == nullableRightsType && propertyInfo.CanRead)
				.Single();

			var rightPropertyGetMethod = rightPropertyInfo.GetMethod;
			if (rightPropertyGetMethod == null)
				throw new InvalidOperationException($"Rights property {rightPropertyInfo.Name} on {rightsClrType.FullName} has no getter!");

			var right = rightPropertyGetMethod.Invoke(
				permissionSet,
				Array.Empty<object>())
				?? throw new InvalidOperationException("A user right was null!");

			if (requirement.Evaluate((TRights)right))
				context.Succeed(requirement);
			else
				context.Fail(
					new AuthorizationFailureReason(this, $"Failed to successfully evaluate rights requirement: {requirement}"));
		}
	}
}
