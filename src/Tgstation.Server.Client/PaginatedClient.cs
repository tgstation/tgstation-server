using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Client that deals with getting paginated results.
	/// </summary>
	abstract class PaginatedClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.
		/// </summary>
		protected IApiClient ApiClient { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatedClient"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="ApiClient"/>.</param>
		public PaginatedClient(IApiClient apiClient)
		{
			ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
		}

		/// <summary>
		/// Reads a given <paramref name="route"/> with paged results.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of result model.</typeparam>
		/// <param name="paginationSettings">The <see cref="PaginationSettings"/> if any.</param>
		/// <param name="route">The route.</param>
		/// <param name="instanceId">The optional <see cref="Instance"/> <see cref="EntityId.Id"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IReadOnlyList{T}"/> of the paginated <typeparamref name="TModel"/>s.</returns>
		protected async Task<IReadOnlyList<TModel>> ReadPaged<TModel>(
			PaginationSettings? paginationSettings,
			string route,
			long? instanceId,
			CancellationToken cancellationToken)
		{
			if (route == null)
				throw new ArgumentNullException(nameof(route));

			var routeFormatter = $"{route}?page={{0}}";
			var currentPage = 1;
			if (paginationSettings != null)
			{
				if (paginationSettings.RetrieveCount == 0)
					return new List<TModel>(); // that was easy
				else if (paginationSettings.RetrieveCount < 0)
					throw new ArgumentOutOfRangeException(nameof(paginationSettings), "RetrieveCount cannot be less than 0!");

				int? pageSize = null;
				if (paginationSettings.PageSize.HasValue)
				{
					// pagesize validates itself on first request
					pageSize = paginationSettings.PageSize.Value;
					routeFormatter += $"&pageSize={pageSize}";
				}

				if (paginationSettings.Offset.HasValue)
				{
					if (paginationSettings.Offset.Value < 0)
						throw new ArgumentOutOfRangeException(nameof(paginationSettings), "Offset cannot be less than 0!");

					pageSize ??= paginationSettings.Offset.Value;
					currentPage = (paginationSettings.Offset.Value / pageSize.Value) + 1;
				}
			}

			Task<PaginatedResponse<TModel>> GetPage() => instanceId.HasValue
				? ApiClient.Read<PaginatedResponse<TModel>>(
					String.Format(CultureInfo.InvariantCulture, routeFormatter, currentPage),
					instanceId.Value,
					cancellationToken)
				: ApiClient.Read<PaginatedResponse<TModel>>(
					String.Format(CultureInfo.InvariantCulture, routeFormatter, currentPage),
					cancellationToken);

			var firstPage = await GetPage().ConfigureAwait(false);

			var totalAvailable = firstPage.TotalPages * firstPage.PageSize;
			var maximumItems = paginationSettings?.RetrieveCount.HasValue == true
				? Math.Min(paginationSettings.RetrieveCount!.Value, totalAvailable)
				: totalAvailable;

			var results = new List<TModel>(maximumItems);
			var currentResults = firstPage;
			do
			{
				// check if first page
				if (currentPage > 1)
					currentResults = await GetPage().ConfigureAwait(false);

				if (currentResults.Content == null)
					throw new ApiConflictException("Paginated results missing content!");

				IEnumerable<TModel> rangeToAdd = currentResults.Content;
				if (paginationSettings?.Offset.HasValue == true)
				{
					rangeToAdd = rangeToAdd
						.Skip(paginationSettings.Offset!.Value % currentResults.PageSize);
				}

				var itemsAvailableInPage = rangeToAdd.Count();
				var itemsStillRequired = maximumItems - results.Count;
				if (itemsAvailableInPage > itemsStillRequired)
					rangeToAdd = rangeToAdd
						.Take(itemsStillRequired);

				results.AddRange(rangeToAdd);
				++currentPage;
			}
			while (results.Count < maximumItems && currentPage <= currentResults.TotalPages);

			return results;
		}
	}
}
