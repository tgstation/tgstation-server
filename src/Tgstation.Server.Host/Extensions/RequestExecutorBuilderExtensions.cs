using System;

using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.GraphQL;
using Tgstation.Server.Host.GraphQL.Metadata;
using Tgstation.Server.Host.GraphQL.Scalars;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IRequestExecutorBuilder"/>.
	/// </summary>
	public static class RequestExecutorBuilderExtensions
	{
		public static void ConfigureGraphQLServer(this IRequestExecutorBuilder builder)
			=> (builder ?? throw new ArgumentNullException(nameof(builder)))
				.ModifyOptions(options =>
				{
					options.EnsureAllNodesCanBeResolved = true;
					options.EnableFlagEnums = true;
				})
#if DEBUG
				.ModifyCostOptions(options =>
				{
					options.EnforceCostLimits = false;
				})
#endif
				.AddMutationConventions()
				.AddInMemorySubscriptions(
					new SubscriptionOptions
					{
						TopicBufferCapacity = 1024, // mainly so high for tests, not possible to DoS the server without authentication and some other access to generate messages
					})
				.AddGlobalObjectIdentification()
				.AddQueryFieldToMutationPayloads()
				.ModifyOptions(options =>
				{
					options.EnableDefer = true;
				})
				.ModifyPagingOptions(pagingOptions =>
				{
					pagingOptions.IncludeTotalCount = true;
					pagingOptions.RequirePagingBoundaries = false;
					pagingOptions.DefaultPageSize = ApiController.DefaultPageSize;
					pagingOptions.MaxPageSize = ApiController.MaximumPageSize;
				})
				.AddFiltering()
				.AddSorting()
				.AddProjections()
				.AddHostTypes()
				.AddAuthorization()
				.ConfigureTypes();

		private static void ConfigureTypes(this IRequestExecutorBuilder builder)
			=> builder
				.AddQueryType<Query>()
				.AddMutationType<Mutation>()
				.AddSubscriptionType<Subscription>()
				.AddType<StandaloneNode>()
				.AddType<LocalGateway>()
				.AddType<UserName>()
				.AddType<UnsignedIntType>()
				.AddRightsHolders()
				.TryAddTypeInterceptor<RightsTypeInterceptor>()
				.BindRuntimeType<Version, SemverType>();

		private static IRequestExecutorBuilder AddRightsHolders(this IRequestExecutorBuilder builder)
		{
			var rightsHolderGeneric = typeof(RightsHolderType<>);
			foreach (var right in RightsHelper.AllRightTypes())
			{
				var instantiatedRightsHolder = rightsHolderGeneric.MakeGenericType(right);
				builder.AddType(instantiatedRightsHolder);
			}

			return builder;
		}
	}
}
