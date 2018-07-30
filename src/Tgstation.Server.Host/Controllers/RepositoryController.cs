using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing the <see cref="Api.Models.Repository"/>s
	/// </summary>
	[Route("/" + nameof(Repository))]
	public sealed class RepositoryController : ModelController<Repository>
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
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		public RepositoryController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		static string GetAccessString(Api.Models.Internal.RepositorySettings repositorySettings) => repositorySettings.AccessUser != null ? String.Concat(repositorySettings.AccessUser, '@', repositorySettings.AccessToken) : null;

		async Task<bool> PopulateApi(Repository model, Components.Repository.IRepository repository, string lastOriginCommitSha, CancellationToken cancellationToken)
		{
			model.IsGitHub = repository.IsGitHubRepository;
			model.Origin = repository.Origin;
			model.Reference = repository.Reference;
			model.Sha = repository.Head;

			//rev info stuff
			var revisionInfo = await DatabaseContext.RevisionInformations.Where(x => x.CommitSha == model.Sha)
				.Include(x => x.CompileJobs)
				.Include(x => x.TestMerges)	//minimal info, they can query the rest if they're allowed
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);  //search every rev info because LOL SHA COLLISIONS

			var needsDbUpdate = revisionInfo == default;
			if (needsDbUpdate)
			{
				//needs insertion
				revisionInfo = new Models.RevisionInformation
				{
					CommitSha = model.Sha,
					CompileJobs = new List<Models.CompileJob>(),
					TestMerges = new List<Models.TestMerge>(),  //non null vals for api returns
					OriginCommitSha = lastOriginCommitSha ?? model.Sha
				};

				DatabaseContext.RevisionInformations.Add(revisionInfo);
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			}

			model.RevisionInformation = revisionInfo.ToApi();
			return needsDbUpdate;
		}

		/// <inheritdoc />
		[TgsAuthorize(RepositoryRights.SetOrigin)]
		public override async Task<IActionResult> Create([FromBody] Repository model, CancellationToken cancellationToken)
		{
			if (model == null)
				return BadRequest(new { message = "Missing request model!" });

			if (model.Origin == null)
				return BadRequest(new { message = "Missing repo origin!" });

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new { message = "Either both accessToken and accessUser must be present or neither!" });

			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);


			currentModel.AccessToken = model.AccessToken;
			currentModel.AccessUser = model.AccessUser;
			currentModel.Origin = model.Origin;  //intentionally only these fields, user not allowed to change anything else atm
			var cloneBranch = model.Reference;

			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;
			using (var repo = await repoManager.CloneRepository(new Uri(currentModel.Origin), cloneBranch, GetAccessString(currentModel), cancellationToken).ConfigureAwait(false))
			{
				if (repo == null)
					//clone conflict
					return Conflict();
				var api = currentModel.ToApi();
				await PopulateApi(api, repo, null, cancellationToken).ConfigureAwait(false);
				currentModel.LastOriginCommitSha = repo.Head;
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
				return Json(api);
			}
		}

		/// <summary>
		/// Delete the <see cref="Repository"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[TgsAuthorize(RepositoryRights.Delete)]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);
			
			currentModel.Origin = null;
			currentModel.LastOriginCommitSha = null;
			currentModel.AccessToken = null;
			currentModel.AccessUser = null;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			await instanceManager.GetInstance(Instance).RepositoryManager.DeleteRepository(cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(RepositoryRights.Read)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var api = currentModel.ToApi();

			using (var repo = await instanceManager.GetInstance(Instance).RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (await PopulateApi(api, repo, currentModel.LastOriginCommitSha, cancellationToken).ConfigureAwait(false))
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
				return Json(api);
			}
		}
	}
}
