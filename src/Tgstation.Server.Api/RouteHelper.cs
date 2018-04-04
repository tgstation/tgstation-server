using System;
using System.Linq;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Gets routes for a given model
	/// </summary>
	public static class RouteHelper
	{
		/// <summary>
		/// Read a <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model type to read</typeparam>
		/// <param name="objectId">The optional ID to pass in</param>
		/// <returns>A route to the read action</returns>
		static Route Read<TModel>(long? objectId) where TModel : class
		{
			var result = new Route { Path = String.Concat('/', typeof(TModel).Name), Method = HttpMethod.Get };
			if (objectId.HasValue)
				result.Path = String.Concat(result.Path, '/', objectId);
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to the read action for a given <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model to read</typeparam>
		/// <param name="instance"><see cref="Instance"/> to read from if required</param>
		/// <returns>A <see cref="Route"/> to the read action</returns>
		public static Route Read<TModel>(Instance instance) where TModel : class
		{
			var model = (ModelAttribute)typeof(TModel).GetCustomAttributes(typeof(ModelAttribute), false).FirstOrDefault();
			if (model == default(ModelAttribute))
				throw new InvalidOperationException("TModel must have the ModelAttribute");
			if (model.RequiresInstance ^ instance != null)
				throw (model.RequiresInstance ? new ArgumentNullException(nameof(instance)) : new ArgumentException("Instance is not used for this route!", nameof(instance)));
			return Read<TModel>(instance?.Id);
		}

		/// <summary>
		/// Get the <see cref="Route"/> to the update action for a given <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model to update</typeparam>
		/// <returns>A <see cref="Route"/> to the update action</returns>
		public static Route Update<TModel>(Instance instance) where TModel : class
		{
			var result = Read<TModel>(instance);
			result.Method = HttpMethod.Post;
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to the list action for a given <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model to list</typeparam>
		/// <returns>A <see cref="Route"/> to the list action</returns>
		public static Route List<TModel>(Instance instance) where TModel : class
		{
			var result = Read<TModel>(instance);
			result.Path = String.Concat(result.Path, '/', nameof(List));
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to the create action for a given <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model to create</typeparam>
		/// <returns>A <see cref="Route"/> to the create action</returns>
		public static Route Create<TModel>(Instance instance) where TModel : class
		{
			var result = Read<TModel>(instance);
			result.Method = HttpMethod.Put;
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to the delete action for a given <typeparamref name="TModel"/>
		/// </summary>
		/// <typeparam name="TModel">The model to delete</typeparam>
		/// <returns>A <see cref="Route"/> to the delete action</returns>
		public static Route Delete<TModel>(Instance instance) where TModel : class
		{
			var result = Read<TModel>(instance);
			result.Method = HttpMethod.Delete;
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to a given <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to get</param>
		/// <returns>A <see cref="Route"/> to the get action</returns>
		public static Route GetJob(Job job) => Read<Job>(job.Id);
	}
}
