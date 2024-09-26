namespace Tgstation.Server.Host.GraphQL.Subscriptions
{
	/// <summary>
	/// Implementation of <see cref="HotChocolate.Subscriptions.ITopicEventReceiver"/> that works around the <see cref="global::System.Threading.CancellationToken"/> issue described in https://github.com/ChilliCream/graphql-platform/issues/6698.
	/// </summary>
	public interface ITopicEventReceiver : HotChocolate.Subscriptions.ITopicEventReceiver
	{
	}
}
