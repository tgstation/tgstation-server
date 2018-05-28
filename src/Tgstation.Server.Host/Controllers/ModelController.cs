using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// An <see cref="ApiController"/> representing a <typeparamref name="TModel"/>
	/// </summary>
	/// <typeparam name="TModel">The model being represented</typeparam>
	public abstract class ModelController<TModel> : ApiController where TModel : class
	{
		/// <summary>
		/// The <see cref="ModelAttribute"/> of the <typeparamref name="TModel"/>
		/// </summary>
		protected static readonly ModelAttribute ModelAttribute = (ModelAttribute)typeof(TModel).GetCustomAttributes(typeof(ModelAttribute), true).First();

		/// <summary>
		/// Construct a <see cref="ModelController{TModel}"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		public ModelController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory) : base(databaseContext, authenticationContextFactory) { }

		/// <summary>
		/// Attempt to create a <paramref name="model"/>
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> being created</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpPut]
		public virtual Task<IActionResult> Create([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		/// <summary>
		/// Attempt to read a <typeparamref name="TModel"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpGet]
		public virtual Task<IActionResult> Read(CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		/// <summary>
		/// Attempt to update a <paramref name="model"/>
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> being updated</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpPost]
		public virtual Task<IActionResult> UpdateAsync([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		/// <summary>
		/// Attempt to delete a <paramref name="model"/>
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> being deleted</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpDelete]
		public virtual Task<IActionResult> Delete([FromBody]TModel model, CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());

		/// <summary>
		/// Attempt to list entries of the <typeparamref name="TModel"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpGet("/List")]
		public virtual Task<IActionResult> List(CancellationToken cancellationToken) => Task.FromResult((IActionResult)NotFound());
	}
}
