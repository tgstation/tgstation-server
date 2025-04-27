using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security.RightsEvaluation;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="IAuthorizationHandler"/> for <see cref="RightsConditional{TRights}"/>s and <see cref="UserSessionValidRequirement"/>s.
	/// </summary>
	public sealed class AuthorizationHandler : IAuthorizationHandler
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="AuthorizationHandler"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IApiHeadersProvider"/> for the <see cref="AuthorizationHandler"/>.
		/// </summary>
		readonly IApiHeadersProvider apiHeadersProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorizationHandler"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="apiHeadersProvider">The value of <see cref="apiHeadersProvider"/>.</param>
		public AuthorizationHandler(IDatabaseContextFactory databaseContextFactory, IApiHeadersProvider apiHeadersProvider)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.apiHeadersProvider = apiHeadersProvider ?? throw new ArgumentNullException(nameof(apiHeadersProvider));
		}

		/// <inheritdoc />
		public Task HandleAsync(AuthorizationHandlerContext context)
		{
			// https://github.com/dotnet/aspnetcore/issues/56272
			CancellationToken cancellationToken = CancellationToken.None;

			ArgumentNullException.ThrowIfNull(context);

			// all the requirements we process require authentication
			if (context.User.Identity?.IsAuthenticated != true)
			{
				context.Fail(
					new AuthorizationFailureReason(this, "User is not authenticated!"));
				return Task.CompletedTask;
			}

			List<ValueTask> processingRequirements = new List<ValueTask>();

			foreach (var req in context.Requirements.OfType<UserSessionValidRequirement>())
				processingRequirements.Add(
					HandleSessionValidRequirement(context, req, cancellationToken));

			var method = GetType().GetMethod(nameof(InvokeHandleRightsConditionalRequirement), BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("Failed to locate rights handler function!");
			foreach (var rightType in RightsHelper.AllRightTypes())
			{
				var genericMethod = method.MakeGenericMethod(rightType);
				processingRequirements.AddRange((IEnumerable<ValueTask>)genericMethod.Invoke(this, [context, cancellationToken])!);
			}

			return ValueTaskExtensions.WhenAll(processingRequirements).AsTask();
		}

		/// <summary>
		/// Handle invoking <see cref="HandleRightsConditionalRequirement{TRights}(AuthorizationHandlerContext, RightsConditional{TRights}, CancellationToken)"/> for given <typeparamref name="TRights"/>.
		/// </summary>
		/// <typeparam name="TRights">The <see cref="Type"/> of right to invoke the requirement handler for.</typeparam>
		/// <param name="context">The shared <see cref="AuthorizationHandlerContext"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ValueTask"/>s representing the running operation.</returns>
		IEnumerable<ValueTask> InvokeHandleRightsConditionalRequirement<TRights>(AuthorizationHandlerContext context, CancellationToken cancellationToken)
			where TRights : Enum
			=> context.Requirements.OfType<RightsConditional<TRights>>().Select(requirement => HandleRightsConditionalRequirement(context, requirement, cancellationToken));

		/// <summary>
		/// Handle <see cref="UserSessionValidRequirement"/> authorization requirements.
		/// </summary>
		/// <param name="context">The shared <see cref="AuthorizationHandlerContext"/>.</param>
		/// <param name="requirement">The <see cref="UserSessionValidRequirement"/> requirment to evaluate.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask HandleSessionValidRequirement(AuthorizationHandlerContext context, UserSessionValidRequirement requirement, CancellationToken cancellationToken)
		{
			var userId = context.User.GetTgsUserId();

			var nbf = context.User.ParseTime(JwtRegisteredClaimNames.Nbf);

			return databaseContextFactory.UseContext(async databaseContext =>
			{
				var sessionData = await databaseContext
					.Users
					.AsQueryable()
					.Where(user => user.Id == userId)
					.Select(user => new
					{
						Enabled = user.Enabled!.Value,
						user.LastPasswordUpdate,
					})
					.TagWith("user_session_validation")
					.FirstOrDefaultAsync(cancellationToken);

				lock (context)
				{
					if (sessionData == null)
						context.Fail(
							new AuthorizationFailureReason(this, $"Unable to retrieve user {userId}!"));
					else if (!sessionData.Enabled)
						context.Fail(
							new AuthorizationFailureReason(this, "User is disabled!"));
					else if (sessionData.LastPasswordUpdate >= nbf)
						context.Fail(
							new AuthorizationFailureReason(this, "User has been modified since logging in!"));
					else
						context.Succeed(requirement);
				}
			});
		}

		/// <summary>
		/// Handle <see cref="RightsConditional{TRights}"/> authorization requirements.
		/// </summary>
		/// <typeparam name="TRights">The <see cref="Type"/> of right to invoke the requirement handler for.</typeparam>
		/// <param name="context">The shared <see cref="AuthorizationHandlerContext"/>.</param>
		/// <param name="requirement">The <see cref="RightsConditional{TRights}"/> requirment to evaluate.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask HandleRightsConditionalRequirement<TRights>(AuthorizationHandlerContext context, RightsConditional<TRights> requirement, CancellationToken cancellationToken)
			where TRights : Enum
		{
			var rightsType = RightsHelper.TypeToRight<TRights>();
			var isInstance = RightsHelper.IsInstanceRight(rightsType);
			var userId = context.User.GetTgsUserId();

			return databaseContextFactory.UseContext(async databaseContext =>
			{
				var queryableUsers = databaseContext
				.Users
				.AsQueryable();

				var matchingUniquePermissionSetIds = queryableUsers
					.Where(user => user.Id == userId && user.PermissionSet != null)
					.Select(user => user.PermissionSet!.Id);

				var matchingGroupPermissionSetIds = queryableUsers
					.Where(user => user.Id == userId && user.Group != null)
					.Select(user => user.Group!.PermissionSet!.Id);

				object? permissionSet;
				if (isInstance)
				{
					if (context.Resource is not Instance instance)
						throw new InvalidOperationException("Instance should have been passed in as authorization resource!");

					var instanceId = instance.Require(i => i.Id);

					permissionSet = await databaseContext
						.InstancePermissionSets
						.AsQueryable()
						.Where(ips => ips.InstanceId == instanceId
							&& (matchingUniquePermissionSetIds.Contains(ips.PermissionSetId) || matchingGroupPermissionSetIds.Contains(ips.PermissionSetId)))
						.TagWith("rights_authorization_handler_instance_permission_set")
						.FirstOrDefaultAsync(cancellationToken);
				}
				else
					permissionSet = await databaseContext
						.PermissionSets
						.AsQueryable()
						.Where(permissionSet => matchingUniquePermissionSetIds.Contains(permissionSet.Id) || matchingGroupPermissionSetIds.Contains(permissionSet.Id))
						.TagWith("rights_authorization_handler_permission_set")
						.FirstOrDefaultAsync(cancellationToken);

				if (permissionSet == null)
				{
					context.Fail(
						new AuthorizationFailureReason(this, $"Unable to find {(isInstance ? "instance " : String.Empty)}permission set for user."));
					return;
				}

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

				var result = requirement.Evaluate((TRights)right);

				lock (context)
				{
					if (result)
						context.Succeed(requirement);
					else
						context.Fail(
							new AuthorizationFailureReason(this, $"Failed to successfully evaluate rights requirement: {requirement}"));
				}
			});
		}
	}
}
