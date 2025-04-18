using System;

using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Security.RightsEvaluation
{
	/// <summary>
	/// An conditional expression of <typeparamref name="TRights"/>.
	/// </summary>
	/// <typeparam name="TRights">The <typeparamref name="TRights"/> to evaluate.</typeparam>
	public abstract class RightsConditional<TRights> : IAuthorizationRequirement
		where TRights : Enum
	{
		/// <summary>
		/// Test if the <see cref="RightsConditional{TRights}"/> is satified for the given <paramref name="rights"/>.
		/// </summary>
		/// <param name="rights">The <typeparamref name="TRights"/> to evaluate the conditional for.</param>
		/// <returns><see langword="true"/> if the <see cref="RightsConditional{TRights}"/> is satisfied by the given <paramref name="rights"/>.</returns>
		public abstract bool Evaluate(TRights rights);
	}
}
