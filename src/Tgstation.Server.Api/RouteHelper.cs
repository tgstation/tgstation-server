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

		/// <summary>
		/// Get the <see cref="Route"/> to a given <paramref name="user"/>'s token list
		/// </summary>
		/// <param name="user">The <see cref="User"/> to list tokens for</param>
		/// <returns>A <see cref="Route"/> to the list action</returns>
		public static Route ListUserTokens(User user)
		{
			var result = List<Token>(null);
			result.Path = String.Concat(result.Path, '/', user.Id);
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to a server's version
		/// </summary>
		/// <returns></returns>
		public static Route ServerVersion() => new Route { Path = "/", Method = HttpMethod.Get };

		/// <summary>
		/// Get the <see cref="Route"/> to read a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> the <see cref="Configuration"/> file resides in</param>
		/// <param name="path">The path to the file in the <see cref="Configuration"/> directory</param>
		/// <returns>A <see cref="Route"/> to the read action</returns>
		public static Route ReadFile(Instance instance, string path) => new Route { Path = String.Concat("/Configuration/", instance?.Id ?? throw new ArgumentNullException(nameof(instance)), '/', path?.TrimStart('/') ?? throw new ArgumentNullException(nameof(path))), Method = HttpMethod.Get };

		/// <summary>
		/// Get the <see cref="Route"/> to list <see cref="Configuration"/> files for a <paramref name="directory"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> the <see cref="Configuration"/> file resides in</param>
		/// <param name="directory">The <see cref="Configuration"/> directory to list</param>
		/// <returns>A <see cref="Route"/> to the read action</returns>
		public static Route ListFiles(Instance instance, string directory)
		{
			var result = ReadFile(instance, directory);
			result.Path = String.Concat("/ConfigurationList", result.Path.Substring(result.Path.IndexOf('/', 1)));
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to create a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> the <see cref="Configuration"/> file resides in</param>
		/// <param name="path">The path to the file in the <see cref="Configuration"/> directory</param>
		/// <returns>A <see cref="Route"/> to the create action</returns>
		public static Route CreateFile(Instance instance, string path)
		{
			var result = ReadFile(instance, path);
			result.Method = HttpMethod.Put;
			return result;
		}

		/// <summary>
		/// Get the <see cref="Route"/> to delete a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> the <see cref="Configuration"/> file resides in</param>
		/// <param name="path">The path to the file in the <see cref="Configuration"/> directory</param>
		/// <returns>A <see cref="Route"/> to the delete action</returns>
		public static Route DeleteFile(Instance instance, string path)
		{
			var result = ReadFile(instance, path);
			result.Method = HttpMethod.Delete;
			return result;
		}
	}
}
