using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Handles requests from DreamDaemon
	/// </summary>
	[Route("/Interop")]
	public sealed class InteropController : ApiController
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="InteropController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Construct an <see cref="InteropController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		public InteropController(IInstanceManager instanceManager, IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory) : base(databaseContext, authenticationContextFactory, false)
		{
			this.instanceManager = instanceManager;
		}

		/// <summary>
		/// Handle a GET to the <see cref="InteropController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpGet]
		public async Task<IActionResult> HandleInterop(CancellationToken cancellationToken)
		{
			var result = await instanceManager.HandleWorldExport(Request.Query, cancellationToken).ConfigureAwait(false);
			//explain things in very simple terms dream daemon can understand	
			//EXCEPT DREAMDAENEN BUGS
			//THAT"S RIGHT !BUGS!
			//ON ANYTHING THAT ISNT A 200 RESPOSNCE REEEE
			if (result == null)
				result = new { STATUS = 404 };
			return Json(result);
		}
	}
}
