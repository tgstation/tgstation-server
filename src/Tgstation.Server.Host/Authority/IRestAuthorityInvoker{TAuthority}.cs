using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// Invokes <typeparamref name="TAuthority"/> methods and generates <see cref="IActionResult"/> responses.
	/// </summary>
	/// <typeparam name="TAuthority">The type of <see cref="IAuthority"/>.</typeparam>
	public interface IRestAuthorityInvoker<TAuthority> : IAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method with no success result.
		/// </summary>
		/// <param name="controller">The <see cref="ApiController"/> invoking the <typeparamref name="TAuthority"/>.</param>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> resulting in the <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> generated for the resulting <see cref="AuthorityResponse"/>.</returns>
		ValueTask<IActionResult> Invoke(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse>> authorityInvoker);

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The resulting <see cref="Type"/> of the <see cref="IActionResult"/>.</typeparam>
		/// <param name="controller">The <see cref="ApiController"/> invoking the <typeparamref name="TAuthority"/>.</param>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> resulting in the <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<IActionResult> Invoke<TResult, TApiModel>(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : TApiModel
			where TApiModel : notnull;

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The returned REST <see cref="Type"/>.</typeparam>
		/// <param name="controller">The <see cref="ApiController"/> invoking the <typeparamref name="TAuthority"/>.</param>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> resulting in the <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<IActionResult> InvokeTransformable<TResult, TApiModel>(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : notnull, ILegacyApiTransformable<TApiModel>
			where TApiModel : notnull;
	}
}
