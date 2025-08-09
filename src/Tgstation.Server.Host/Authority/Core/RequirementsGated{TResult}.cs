using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Evaluates a set of <see cref="IAuthorizationRequirement"/>s to be checked before executing a response.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> of object the response generates.</typeparam>
	public sealed class RequirementsGated<TResult>
	{
		/// <summary>
		/// <see cref="Api.Models.EntityId.Id"/> of the relevant instance.
		/// </summary>
		public long? InstanceId { get; }

		/// <summary>
		/// The <see cref="IAuthorizationRequirement"/> retrieval function. <see cref="UserSessionValidRequirement"/> is included automatically.
		/// </summary>
		readonly Func<ValueTask<IEnumerable<IAuthorizationRequirement>>> getRequirements;

		/// <summary>
		/// The response generation function.
		/// </summary>
		readonly Func<Security.IAuthorizationService, ValueTask<TResult>> getResponse;

		/// <summary>
		/// If the <see cref="UserSessionValidRequirement"/> should not be added.
		/// </summary>
		readonly bool doNotAddUserSessionValidRequirement;

		/// <summary>
		/// Convert a given <paramref name="result"/> into a <see cref="RequirementsGated{TResult}"/>.
		/// </summary>
		/// <param name="result">The <typeparamref name="TResult"/> to convert.</param>
		/// <returns>A new <see cref="RequirementsGated{TResult}"/> based on <paramref name="result"/>.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types
		public static RequirementsGated<TResult> FromResult(TResult result)
#pragma warning restore CA1000 // Do not declare static members on generic types
			=> new(
				() => (IAuthorizationRequirement?)null,
				() => ValueTask.FromResult(result));

		/// <summary>
		/// Initializes a new instance of the <see cref="RequirementsGated{TResult}"/> class.
		/// </summary>
		/// <param name="getRequirement">The value of <see cref="getRequirements"/>. Resulting in a <see langword="null"/> value is eqivalent to returning an empty <see cref="IEnumerable{T}"/> of <see cref="IAuthorizationRequirement"/>s.</param>
		/// <param name="getResponse">The value of <see cref="getResponse"/>.</param>
		public RequirementsGated(
			Func<ValueTask<IAuthorizationRequirement?>> getRequirement,
			Func<ValueTask<TResult>> getResponse)
		{
			ArgumentNullException.ThrowIfNull(getRequirement);
			ArgumentNullException.ThrowIfNull(getResponse);
			getRequirements = async () =>
			{
				var requirement = await getRequirement();
				if (requirement == null)
					return Enumerable.Empty<IAuthorizationRequirement>();

				return new List<IAuthorizationRequirement>
				{
					requirement,
				};
			};
			this.getResponse = _ => getResponse();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequirementsGated{TResult}"/> class.
		/// </summary>
		/// <param name="getRequirements">The value of <see cref="getRequirements"/>.</param>
		/// <param name="getResponse">The value of <see cref="getResponse"/>.</param>
		public RequirementsGated(
			Func<IEnumerable<IAuthorizationRequirement>> getRequirements,
			Func<ValueTask<TResult>> getResponse)
		{
			ArgumentNullException.ThrowIfNull(getRequirements);
			ArgumentNullException.ThrowIfNull(getResponse);
			this.getRequirements = () => ValueTask.FromResult(getRequirements());
			this.getResponse = _ => getResponse();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequirementsGated{TResult}"/> class.
		/// </summary>
		/// <param name="getRequirement">The value of <see cref="getRequirements"/>. Resulting in a <see langword="null"/> value is eqivalent to returning an empty <see cref="IEnumerable{T}"/> of <see cref="IAuthorizationRequirement"/>s.</param>
		/// <param name="getResponse">The value of <see cref="getResponse"/>.</param>
		/// <param name="instanceId">The value of <see cref="InstanceId"/>.</param>
		/// <param name="doNotAddUserSessionValidRequirement">The value of <see cref="doNotAddUserSessionValidRequirement"/>.</param>
		public RequirementsGated(
			Func<IAuthorizationRequirement?> getRequirement,
			Func<ValueTask<TResult>> getResponse,
			long? instanceId = null,
			bool doNotAddUserSessionValidRequirement = false)
		{
			ArgumentNullException.ThrowIfNull(getRequirement);
			ArgumentNullException.ThrowIfNull(getResponse);
			getRequirements = () =>
			{
				var requirement = getRequirement();
				if (requirement == null)
					return ValueTask.FromResult(Enumerable.Empty<IAuthorizationRequirement>());

				return ValueTask.FromResult<IEnumerable<IAuthorizationRequirement>>(
					new List<IAuthorizationRequirement>
					{
						requirement,
					});
			};

			this.getResponse = _ => getResponse();

			this.doNotAddUserSessionValidRequirement = doNotAddUserSessionValidRequirement;
			InstanceId = instanceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequirementsGated{TResult}"/> class.
		/// </summary>
		/// <param name="getRequirement">The value of <see cref="getRequirements"/>. Resulting in a <see langword="null"/> value is eqivalent to returning an empty <see cref="IEnumerable{T}"/> of <see cref="IAuthorizationRequirement"/>s.</param>
		/// <param name="getResponse">The value of <see cref="getResponse"/>.</param>
		public RequirementsGated(
			Func<IAuthorizationRequirement?> getRequirement,
			Func<Security.IAuthorizationService, ValueTask<TResult>> getResponse)
		{
			ArgumentNullException.ThrowIfNull(getRequirement);
			getRequirements = () =>
			{
				var requirement = getRequirement();
				if (requirement == null)
					return ValueTask.FromResult(Enumerable.Empty<IAuthorizationRequirement>());

				return ValueTask.FromResult<IEnumerable<IAuthorizationRequirement>>(
					new List<IAuthorizationRequirement>
					{
						requirement,
					});
			};

			this.getResponse = getResponse ?? throw new ArgumentNullException(nameof(getResponse));
		}

		/// <summary>
		/// Evaluates the <see cref="IAuthorizationRequirement"/>s of the request.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IAuthorizationRequirement"/>s for the request.</returns>
		public async ValueTask<IEnumerable<IAuthorizationRequirement>> GetRequirements()
		{
			var requirements = await getRequirements();
			if (!doNotAddUserSessionValidRequirement)
				requirements = UserSessionValidRequirement.InstanceAsEnumerable.Concat(requirements);

			return requirements;
		}

		/// <summary>
		/// Executes the request.
		/// </summary>
		/// <param name="authorizationService">The authorization service to use.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the request <typeparamref name="TResult"/>.</returns>
		public ValueTask<TResult> Execute(Security.IAuthorizationService authorizationService)
			=> getResponse(authorizationService);
	}
}
