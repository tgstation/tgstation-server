using System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Holder for a given <typeparamref name="TRight"/>.
	/// </summary>
	/// <typeparam name="TRight">The <see cref="Api.Rights.RightsType"/> being held.</typeparam>
	public sealed class RightsHolder<TRight>
		where TRight : struct, Enum
	{
		/// <summary>
		/// Marker property to allow <see cref="Right"/> to be properly selected by the GraphQL system. Should not be used for any other purpose.
		/// </summary>
		public bool HasContextFlag
		{
			get => throw new InvalidOperationException($"{nameof(HasContextFlag)} getter should not be called!");
			set => throw new InvalidOperationException($"{nameof(HasContextFlag)} setter should not be called!");
		}

		/// <summary>
		/// The held <typeparamref name="TRight"/> value.
		/// </summary>
		public required TRight Right { get; init; }
	}
}
