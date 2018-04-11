using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Tgstation.Server.Api;

namespace Tgstation.Server.Host.Controllers
{
	[Route("/")]
	[Produces(ApiHeaders.ApplicationJson)]
	public sealed class HomeController : Controller
	{
		[HttpGet]
		public JsonResult Home() => Json(Assembly.GetExecutingAssembly().GetName().Version);
	}
}
