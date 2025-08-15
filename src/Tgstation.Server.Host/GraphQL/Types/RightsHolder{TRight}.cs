using System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	public sealed class RightsHolder<TRight>
		where TRight : struct, Enum
	{
		bool? contextFlag;

		public bool HasContextFlag
		{
			get => throw new InvalidOperationException($"{nameof(HasContextFlag)} getter should not be called!");
			set => throw new InvalidOperationException($"{nameof(HasContextFlag)} setter should not be called!");
		}

		public required TRight Right { get; init; }

		public void SetContextFlag(TRight flag)
			=> contextFlag = Right.HasFlag(flag);
	}
}
