using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	public abstract class ModelController<TModel> : ApiController
	{
		protected static readonly ModelAttribute ModelAttribute = (ModelAttribute)typeof(TModel).GetCustomAttributes(typeof(ModelAttribute), true).First();

		public ModelController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory) : base(databaseContext, authenticationContextFactory) { }

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
