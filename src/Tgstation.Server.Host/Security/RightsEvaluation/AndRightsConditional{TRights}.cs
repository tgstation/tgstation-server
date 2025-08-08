using System;

namespace Tgstation.Server.Host.Security.RightsEvaluation
{
	/// <summary>
	/// Logical AND <see cref="RightsConditional{TRights}"/>.
	/// </summary>
	/// <typeparam name="TRights">The <typeparamref name="TRights"/> to evaluate.</typeparam>
	public sealed class AndRightsConditional<TRights> : RightsConditional<TRights>
		where TRights : Enum
	{
		/// <summary>
		/// The left hand side operand.
		/// </summary>
		readonly RightsConditional<TRights> lhs;

		/// <summary>
		/// The right hand side operand.
		/// </summary>
		readonly RightsConditional<TRights> rhs;

		/// <summary>
		/// Initializes a new instance of the <see cref="AndRightsConditional{TRights}"/> class.
		/// </summary>
		/// <param name="lhs">The value of <see cref="lhs"/>.</param>
		/// <param name="rhs">The value of <see cref="rhs"/>.</param>
		public AndRightsConditional(RightsConditional<TRights> lhs, RightsConditional<TRights> rhs)
		{
			this.lhs = lhs ?? throw new ArgumentNullException(nameof(lhs));
			this.rhs = rhs ?? throw new ArgumentNullException(nameof(rhs));
		}

		/// <inheritdoc />
		public override bool Evaluate(TRights rights)
			=> lhs.Evaluate(rights) && rhs.Evaluate(rights);

		/// <inheritdoc />
		public override string ToString()
			=> $"({lhs} && {rhs})";
	}
}
