using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
	[Produces(ApiHeaders.ApplicationJson)]
	[Consumes(ApiHeaders.ApplicationJson)]
	public abstract class ApiController<TModel> : Controller
	{
		protected ModelAttribute ModelAttribute => (ModelAttribute)typeof(TModel).GetCustomAttributes(typeof(ModelAttribute), true).First();

		protected ApiHeaders ApiHeaders { get; }

		protected IDatabaseContext DatabaseContext { get; }

		protected IAuthenticationContext AuthenticationContext { get; private set; }

		readonly ITokenFactory tokenManager;

		public ApiController(IDatabaseContext databaseContext, ITokenFactory tokenManager)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
		}

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			User.Claims.
			
			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
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
