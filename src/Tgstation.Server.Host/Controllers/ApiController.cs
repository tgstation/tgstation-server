using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	[Authorize]
	[Route("[controller]")]
	[Produces(ApiHeaders.ApplicationJson)]
	[Consumes(ApiHeaders.ApplicationJson)]
	public abstract class ApiController<TModel> : Controller
	{
		protected ModelAttribute ModelAttribute => (ModelAttribute)typeof(TModel).GetCustomAttributes(typeof(ModelAttribute), true).First();

		protected ApiHeaders ApiHeaders { get; }

		protected IDatabaseContext DatabaseContext { get; }

		protected IAuthenticationContext AuthenticationContext { get;  }

		protected Instance Instance { get; }

		public ApiController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));

			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;

			if (AuthenticationContext.InstanceUser != null)
				Instance = AuthenticationContext.InstanceUser.Instance;
		}
		
		[HttpPut]
		public virtual Task<IActionResult> Create([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());
		
		[HttpGet]
		public virtual Task<IActionResult> Read(CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		[HttpPost]
		public virtual Task<IActionResult> Update([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		[HttpDelete]
		public virtual Task<IActionResult> Delete([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		[HttpGet("/List")]
		public virtual Task<IActionResult> List(CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());
	}
}
