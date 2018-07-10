namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Creates <see cref="IInteropContext"/>s for <see cref="IInteropConsumer"/>s
	/// </summary>
    interface IInteropRegistrar
    {
		/// <summary>
		/// Register a <paramref name="consumer"/>
		/// </summary>
		/// <param name="accessIdentifier">The access identifier for the entry</param>
		/// <param name="consumer">The <see cref="IInteropConsumer"/> being registered</param>
		/// <returns>A new <see cref="IInteropContext"/> representing the registration</returns>
		IInteropContext Register(string accessIdentifier, IInteropConsumer consumer);
    }
}
