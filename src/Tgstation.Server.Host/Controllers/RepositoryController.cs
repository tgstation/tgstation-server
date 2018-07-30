using Microsoft.AspNetCore.Mvc;
using System;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing the <see cref="Api.Models.Repository"/>s
	/// </summary>
	[Route("/" + nameof(Api.Models.Repository))]
	public sealed class RepositoryController : ModelController<Api.Models.Repository>
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Construct a <see cref="RepositoryController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceMangaer">The value of <see cref="instanceMangaer"/></param>
		public RepositoryController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}
	}
}
