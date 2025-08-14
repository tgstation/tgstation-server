namespace Tgstation.Server.Host.Authority.Core
{
	public class Projected<TQueried, TResult>
	{
		public TQueried Queried { get; init; }

		public TResult Result { get; init; }
	}
}
