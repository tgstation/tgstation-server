namespace Tgstation.Server.Host.Core
{
    interface IServerUpdateConsumer
    {
		void ApplyUpdate(string updatePath);
    }
}
