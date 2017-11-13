using System.Collections.Generic;
using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Interface for handling chat bot
	/// </summary>
	[ServiceContract]
	public interface ITGChat
	{
		/// <summary>
		/// Sets a chat provider <paramref name="info"/>
		/// </summary>
		/// <param name="info">The info to set</param>
		[OperationContract]
		string SetProviderInfo(ChatSetupInfo info);
		/// <summary>
		/// Returns <see cref="ChatSetupInfo"/> for all <see cref="ChatProvider"/>s
		/// </summary>
		/// <returns>A list of all <see cref="ChatSetupInfo"/>s</returns>
		[OperationContract]
		IList<ChatSetupInfo> ProviderInfos();

		/// <summary>
		/// Checks connection status
		/// </summary>
		/// <param name="providerType">The type of provider to check if connected</param>
		/// <returns><see langword="true"/> if connected, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool Connected(ChatProvider providerType);

		/// <summary>
		/// Reconnect a specific <paramref name="providerType"/> to it's chat service
		/// </summary>
		/// <param name="providerType">The type of provider to reconnect</param>
		/// <returns><see langword="null"/> on success, error message <see cref="string"/> on failure</returns>
		[OperationContract]
		string Reconnect(ChatProvider providerType);
	}
}
