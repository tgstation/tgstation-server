﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Security;

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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AdministrationResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ChangeVersion)]
		ValueTask<AuthorityResponse<AdministrationResponse>> GetUpdateInformation(bool forceFresh, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a restart of tgstation-server without terminating running game instances, setting its version to a given <paramref name="targetVersion"/>.
		/// </summary>
		/// <param name="targetVersion">The <see cref="Version"/> TGS will switch to upon reboot.</param>
		/// <param name="uploadZip">If <see langword="true"/> a <see cref="FileTicketResponse.FileTicket"/> will be returned and the call must provide an uploaded zip file containing the update data to the file transfer service.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="ServerUpdateResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ChangeVersion | AdministrationRights.UploadVersion)]
		ValueTask<AuthorityResponse<ServerUpdateResponse>> TriggerServerVersionChange(Version targetVersion, bool uploadZip, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a restart of tgstation-server without terminating running game instances.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse"/>.</returns>
		[TgsAuthorize(AdministrationRights.RestartHost)]
		ValueTask<AuthorityResponse> TriggerServerRestart();
	}
}
