﻿using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api.Models.Response;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating <see cref="TokenResponse"/>s.
	/// </summary>
	public interface ITokenFactory
	{
		/// <summary>
		/// The <see cref="TokenValidationParameters"/> for the <see cref="ITokenFactory"/>.
		/// </summary>
		TokenValidationParameters ValidationParameters { get; }

		/// <summary>
		/// Create a <see cref="TokenResponse"/> for a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> to create the token for. Must have the <see cref="Api.Models.EntityId.Id"/> field available.</param>
		/// <param name="oAuth">Whether or not this is an OAuth login.</param>
		/// <returns>A new <see cref="TokenResponse"/>.</returns>
		TokenResponse CreateToken(Models.User user, bool oAuth);
	}
}
