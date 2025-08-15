using System;

using HotChocolate.Types;

using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Metadata
{
	/// <summary>
	/// <see cref="ObjectType{T}"/> for <see cref="RightsHolder{TRight}"/>s to give the flags proper GraphQL semantics.
	/// </summary>
	/// <typeparam name="TRight">The <see cref="Api.Rights.RightsType"/> of the <see cref="RightsHolder{TRight}"/>.</typeparam>
	public sealed class RightsHolderType<TRight> : ObjectType<RightsHolder<TRight>>
		where TRight : struct, Enum
	{
		/// <inheritdoc />
		protected override void Configure(IObjectTypeDescriptor<RightsHolder<TRight>> descriptor)
		{
			ArgumentNullException.ThrowIfNull(descriptor);

			descriptor
				.BindFieldsExplicitly()
				.Name(typeof(TRight).Name);

			foreach (var individualRightValue in Enum.GetValues<TRight>())
			{
				var rightName = Enum.GetName(individualRightValue);

				descriptor
					.Field($"can{rightName}")
					.ResolveWith<RightsHolder<TRight>>((holder) => holder.HasContextFlag)
					.Extend()
					.OnBeforeCompletion(
						(context, definition) =>
						{
							definition.Member = typeof(RightsHolder<TRight>).GetProperty(nameof(RightsHolder<TRight>.Right));
							definition.PureResolver = context =>
							{
								var holder = context.Parent<RightsHolder<TRight>>();
								return holder.Right.HasFlag(individualRightValue);
							};
						});
			}
		}
	}
}
