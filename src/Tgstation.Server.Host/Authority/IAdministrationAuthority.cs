using System;
using System.Threading;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for administrative server operations.
	/// </summary>
	public interface IAdministrationAuthority : IAuthority
	{
		/// <summary>
		/// Gets the <see cref="AdministrationResponse"/> containing server update information.
		/// </summary>
		/// <param name="forceFresh">Bypass the caching that the authority performs for this request, forcing it to contact GitHub.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="AdministrationResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<AdministrationResponse>> GetUpdateInformation(bool forceFresh, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a restart of tgstation-server without terminating running game instances, setting its version to a given <paramref name="targetVersion"/>.
		/// </summary>
		/// <param name="targetVersion">The <see cref="Version"/> TGS will switch to upon reboot.</param>
		/// <param name="uploadZip">If <see langword="true"/> a <see cref="FileTicketResponse.FileTicket"/> will be returned and the call must provide an uploaded zip file containing the update data to the file transfer service.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="ServerUpdateResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<ServerUpdateResponse>> TriggerServerVersionChange(Version targetVersion, bool uploadZip, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a restart of tgstation-server without terminating running game instances.
		/// </summary>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse"/>.</returns>
		RequirementsGated<AuthorityResponse> TriggerServerRestart();

		/// <summary>
		/// Get a ticket for downloading a log file at a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The relative path to the log file in the directory.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="LogFileResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<LogFileResponse>> GetLog(string path, CancellationToken cancellationToken);
	}
}
