using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <inheritdoc />
	abstract class AuthorityInvokerBase<TAuthority> : IAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// The <see cref="IAuthority"/> being invoked.
		/// </summary>
		protected TAuthority Authority { get; }

		/// <summary>
		/// The authorization service for the <see cref="AuthorityInvokerBase{TAuthority}"/>.
		/// </summary>
		readonly Security.IAuthorizationService authorizationService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityInvokerBase{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The value of <see cref="Authority"/>.</param>
		/// <param name="authorizationService">The value of <see cref="authorizationService"/>.</param>
		public AuthorityInvokerBase(
			TAuthority authority,
			Security.IAuthorizationService authorizationService)
		{
			Authority = authority ?? throw new ArgumentNullException(nameof(authority));
			this.authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		}

		/// <inheritdoc />
		async ValueTask<IQueryable<TResult>?> IAuthorityInvoker<TAuthority>.InvokeQueryable<TResult>(Func<TAuthority, RequirementsGated<IQueryable<TResult>>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			return await ExecuteIfRequirementsSatisfied(requirementsGate);
		}

		/// <summary>
		/// Unwrap a <see cref="RequirementsGated{TResult}"/> result, returning <see langword="null"/> if the requirements weren't satisfied.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> contained by the <paramref name="requirementsGate"/>.</typeparam>
		/// <param name="requirementsGate">The <see cref="RequirementsGated{TResult}"/> result.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResult"/> if the requirements were met, <see langword="null"/> if the requirments weren't met.</returns>
		protected async ValueTask<TResult?> ExecuteIfRequirementsSatisfied<TResult>(RequirementsGated<TResult> requirementsGate)
			where TResult : class
		{
			var requirements = await requirementsGate.GetRequirements();
			var authorizationResult = await authorizationService.AuthorizeAsync(requirements);

			if (!authorizationResult.Succeeded)
			{
				OnRequirementsFailure(authorizationResult.Failure);
				return null;
			}

			return await requirementsGate.Execute(authorizationService);
		}

		/// <summary>
		/// Called to handle generic behavior when requirements evaluation fails.
		/// </summary>
		/// <param name="authFailure">The <see cref="AuthorizationFailure"/>.</param>
		protected virtual void OnRequirementsFailure(AuthorizationFailure authFailure)
		{
		}
	}
}
