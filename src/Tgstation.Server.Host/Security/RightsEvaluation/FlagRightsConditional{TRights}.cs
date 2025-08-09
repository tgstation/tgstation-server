using System;

namespace Tgstation.Server.Host.Security.RightsEvaluation
{
	/// <summary>
	/// Single flag <see cref="RightsConditional{TRights}"/>.
	/// </summary>
	/// <typeparam name="TRights">The <typeparamref name="TRights"/> to evaluate.</typeparam>
	/// <remarks>An instance-based <typeparamref name="TRights"/> type MUST be evaluated with the <see cref="Api.Models.EntityId.Id"/> of the instance attached as a resource.</remarks>
	public sealed class FlagRightsConditional<TRights> : RightsConditional<TRights>
		where TRights : Enum
	{
		/// <summary>
		/// The single bit flag of the <typeparamref name="TRights"/>.
		/// </summary>
		readonly TRights flag;

		/// <summary>
		/// Initializes a new instance of the <see cref="FlagRightsConditional{TRights}"/> class.
		/// </summary>
		/// <param name="flag">The value of <see cref="flag"/>.</param>
		public FlagRightsConditional(TRights flag)
		{
			var asUlong = (ulong)(object)flag;

			if (asUlong == 0)
				throw new ArgumentOutOfRangeException(nameof(flag), flag, "Flag cannot be zero!");

			// https://stackoverflow.com/a/28303898/3976486
			if ((asUlong & (asUlong - 1)) != 0)
				throw new ArgumentException("Right has more than one bit set!", nameof(flag));

			this.flag = flag;
		}

		/// <inheritdoc />
		public override bool Evaluate(TRights rights)
			=> rights.HasFlag(flag);

		/// <inheritdoc />
		public override string ToString()
			=> $"{typeof(TRights).Name}.{flag}";
	}
}
