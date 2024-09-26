using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Implements various filters for <see cref="Swashbuckle"/>.
	/// </summary>
	sealed class SwaggerConfiguration : IOperationFilter, IDocumentFilter, ISchemaFilter, IRequestBodyFilter
	{
		/// <summary>
		/// The name of the swagger document.
		/// </summary>
		public const string DocumentName = "tgs_api";

		/// <summary>
		/// The path to the hosted documentation site.
		/// </summary>
		public const string DocumentationSiteRouteExtension = "documentation";

		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for password authentication.
		/// </summary>
		const string PasswordSecuritySchemeId = "Password_Login_Scheme";

		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for OAuth 2.0 authentication.
		/// </summary>
		const string OAuthSecuritySchemeId = "OAuth_Login_Scheme";

		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for token authentication.
		/// </summary>
		const string TokenSecuritySchemeId = "Token_Authorization_Scheme";

		/// <summary>
		/// Configure the swagger settings.
		/// </summary>
		/// <param name="swaggerGenOptions">The <see cref="SwaggerGenOptions"/> to use.</param>
		/// <param name="assemblyDocumentationPath">The path to the XML documentation file for the <see cref="Host"/> assembly.</param>
		/// <param name="apiDocumentationPath">The path to the XML documentation file for the <see cref="Api"/> assembly.</param>
		public static void Configure(SwaggerGenOptions swaggerGenOptions, string assemblyDocumentationPath, string apiDocumentationPath)
		{
			swaggerGenOptions.SwaggerDoc(
				DocumentName,
				new OpenApiInfo
				{
					Title = "TGS API",
					Version = ApiHeaders.Version.Semver().ToString(),
					License = new OpenApiLicense
					{
						Name = "AGPL-3.0",
						Url = new Uri("https://github.com/tgstation/tgstation-server/blob/dev/LICENSE"),
					},
					Contact = new OpenApiContact
					{
						Name = "/tg/station 13",
						Url = new Uri("https://github.com/tgstation"),
					},
					Description = "A production scale tool for DreamMaker server management",
				});

			// Important to do this before applying our own filters
			// Otherwise we'll get NullReferenceExceptions on parameters to be setup in our document filter
			swaggerGenOptions.IncludeXmlComments(assemblyDocumentationPath);
			swaggerGenOptions.IncludeXmlComments(apiDocumentationPath);

			// nullable stuff
			swaggerGenOptions.UseAllOfToExtendReferenceSchemas();

			swaggerGenOptions.OperationFilter<SwaggerConfiguration>();
			swaggerGenOptions.DocumentFilter<SwaggerConfiguration>();
			swaggerGenOptions.SchemaFilter<SwaggerConfiguration>();
			swaggerGenOptions.RequestBodyFilter<SwaggerConfiguration>();

			swaggerGenOptions.CustomSchemaIds(GenerateSchemaId);

			swaggerGenOptions.AddSecurityDefinition(PasswordSecuritySchemeId, new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.BasicAuthenticationScheme,
			});

			swaggerGenOptions.AddSecurityDefinition(OAuthSecuritySchemeId, new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.OAuthAuthenticationScheme,
			});

			swaggerGenOptions.AddSecurityDefinition(TokenSecuritySchemeId, new OpenApiSecurityScheme
			{
				BearerFormat = "JWT",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.BearerAuthenticationScheme,
			});
		}

		/// <summary>
		/// Add the default error responses to a given <paramref name="document"/>.
		/// </summary>
		/// <param name="document">The <see cref="OpenApiDocument"/> to augment.</param>
		static void AddDefaultResponses(OpenApiDocument document)
		{
			var errorMessageContent = new Dictionary<string, OpenApiMediaType>
			{
				{
					MediaTypeNames.Application.Json,
					new OpenApiMediaType
					{
						Schema = new OpenApiSchema
						{
							Reference = new OpenApiReference
							{
								Id = nameof(ErrorMessageResponse),
								Type = ReferenceType.Schema,
							},
						},
					}
				},
			};

			void AddDefaultResponse(HttpStatusCode code, OpenApiResponse concrete)
			{
				string responseKey = $"{(int)code}";

				document.Components.Responses.Add(responseKey, concrete);

				var referenceResponse = new OpenApiResponse
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Response,
						Id = responseKey,
					},
				};

				foreach (var operation in document.Paths.SelectMany(path => path.Value.Operations))
					operation.Value.Responses.TryAdd(responseKey, referenceResponse);
			}

			AddDefaultResponse(HttpStatusCode.BadRequest, new OpenApiResponse
			{
				Description = "A badly formatted request was made. See error message for details.",
				Content = errorMessageContent,
			});

			AddDefaultResponse(HttpStatusCode.Unauthorized, new OpenApiResponse
			{
				Description = "Invalid Authentication header.",
			});

			AddDefaultResponse(HttpStatusCode.Forbidden, new OpenApiResponse
			{
				Description = "User lacks sufficient permissions for the operation.",
			});

			AddDefaultResponse(HttpStatusCode.Conflict, new OpenApiResponse
			{
				Description = "A data integrity check failed while performing the operation. See error message for details.",
				Content = errorMessageContent,
			});

			AddDefaultResponse(HttpStatusCode.NotAcceptable, new OpenApiResponse
			{
				Description = $"Invalid Accept header, TGS requires `{HeaderNames.Accept}: {MediaTypeNames.Application.Json}`.",
				Content = errorMessageContent,
			});

			AddDefaultResponse(HttpStatusCode.InternalServerError, new OpenApiResponse
			{
				Description = ErrorCode.InternalServerError.Describe(),
				Content = errorMessageContent,
			});

			AddDefaultResponse(HttpStatusCode.ServiceUnavailable, new OpenApiResponse
			{
				Description = "The server may be starting up or shutting down.",
			});

			AddDefaultResponse(HttpStatusCode.NotImplemented, new OpenApiResponse
			{
				Description = ErrorCode.RequiresPosixSystemIdentity.Describe(),
				Content = errorMessageContent,
			});
		}

		/// <summary>
		/// Applies the <see cref="OpenApiSchema.Nullable"/>, <see cref="OpenApiSchema.ReadOnly"/>, and <see cref="OpenApiSchema.WriteOnly"/> to <see cref="OpenApiSchema.Properties"/> of a given <paramref name="rootSchema"/>.
		/// </summary>
		/// <param name="rootSchema">The root <see cref="OpenApiSchema"/>.</param>
		/// <param name="context">The current <see cref="SchemaFilterContext"/>.</param>
		static void ApplyAttributesForRootSchema(OpenApiSchema rootSchema, SchemaFilterContext context)
		{
			// tune up the descendants
			rootSchema.Nullable = false;
			var rootSchemaId = GenerateSchemaId(context.Type);
			var rootRequestSchema = rootSchemaId.EndsWith("Request", StringComparison.Ordinal);
			var rootResponseSchema = rootSchemaId.EndsWith("Response", StringComparison.Ordinal);
			var isPutRequest = rootSchemaId.EndsWith("CreateRequest", StringComparison.Ordinal);

			Tuple<PropertyInfo, string, OpenApiSchema, IDictionary<string, OpenApiSchema>> GetTypeFromKvp(Type currentType, KeyValuePair<string, OpenApiSchema> kvp, IDictionary<string, OpenApiSchema> schemaDictionary)
			{
				var propertyInfo = currentType
					.GetProperties()
					.Single(x => x.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

				return Tuple.Create(
					propertyInfo,
					kvp.Key,
					kvp.Value,
					schemaDictionary);
			}

			var subSchemaStack = new Stack<Tuple<PropertyInfo, string, OpenApiSchema, IDictionary<string, OpenApiSchema>>>(
				rootSchema
					.Properties
					.Select(
						x => GetTypeFromKvp(context.Type, x, rootSchema.Properties))
					.Where(x => x.Item3.Reference == null));

			while (subSchemaStack.Count > 0)
			{
				var tuple = subSchemaStack.Pop();
				var subSchema = tuple.Item3;

				var subSchemaPropertyInfo = tuple.Item1;

				if (subSchema.Properties != null
					&& !subSchemaPropertyInfo
						.PropertyType
						.GetInterfaces()
						.Any(x => x == typeof(IEnumerable)))
					foreach (var kvp in subSchema.Properties.Where(x => x.Value.Reference == null))
						subSchemaStack.Push(GetTypeFromKvp(subSchemaPropertyInfo.PropertyType, kvp, subSchema.Properties));

				var attributes = subSchemaPropertyInfo
					.GetCustomAttributes();
				var responsePresence = attributes
					.OfType<ResponseOptionsAttribute>()
					.FirstOrDefault()
					?.Presence
					?? FieldPresence.Required;
				var requestOptions = attributes
					.OfType<RequestOptionsAttribute>()
					.OrderBy(x => x.PutOnly) // Process PUTs last
					.ToList();

				if (requestOptions.Count == 0 && requestOptions.All(x => x.Presence == FieldPresence.Ignored && !x.PutOnly))
					subSchema.ReadOnly = true;

				var subSchemaId = tuple.Item2;
				var subSchemaOwningDictionary = tuple.Item4;
				if (rootResponseSchema)
				{
					subSchema.Nullable = responsePresence == FieldPresence.Optional;
					if (responsePresence == FieldPresence.Ignored)
						subSchemaOwningDictionary.Remove(subSchemaId);
				}
				else if (rootRequestSchema)
				{
					subSchema.Nullable = true;
					var lastOptionWasIgnored = false;
					foreach (var requestOption in requestOptions)
					{
						var validForThisRequest = !requestOption.PutOnly || isPutRequest;
						if (!validForThisRequest)
							continue;

						lastOptionWasIgnored = false;
						switch (requestOption.Presence)
						{
							case FieldPresence.Ignored:
								lastOptionWasIgnored = true;
								break;
							case FieldPresence.Optional:
								subSchema.Nullable = true;
								break;
							case FieldPresence.Required:
								subSchema.Nullable = false;
								break;
							default:
								throw new InvalidOperationException($"Invalid FieldPresence: {requestOption.Presence}!");
						}
					}

					if (lastOptionWasIgnored)
						subSchemaOwningDictionary.Remove(subSchemaId);
				}
				else if (responsePresence == FieldPresence.Required
					&& requestOptions.All(x => x.Presence == FieldPresence.Required && !x.PutOnly))
					subSchema.Nullable = subSchemaId.Equals(
						nameof(TestMergeParameters.TargetCommitSha),
						StringComparison.OrdinalIgnoreCase)
						&& rootSchemaId == nameof(TestMergeParameters); // special tactics

				// otherwise, we have to assume it's a shared schema
				// use what Swagger thinks the nullability is by default
			}
		}

		/// <summary>
		/// Generates the OpenAPI schema ID for a given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to generate a schema ID for.</param>
		/// <returns>The generated schema ID for <see cref="Type"/>.</returns>
		static string GenerateSchemaId(Type type)
		{
			if (type == typeof(UserName))
				return "ShallowUserResponse";

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PaginatedResponse<>))
				return $"Paginated{type.GenericTypeArguments.First().Name}";

			return type.Name;
		}

		/// <inheritdoc />
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			ArgumentNullException.ThrowIfNull(operation);
			ArgumentNullException.ThrowIfNull(context);

			operation.OperationId = $"{context.MethodInfo.DeclaringType!.Name}.{context.MethodInfo.Name}";

			var authAttributes = context
				.MethodInfo
				.DeclaringType
				.GetCustomAttributes(true)
				.Union(
					context
					.MethodInfo
					.GetCustomAttributes(true))
					.OfType<TgsAuthorizeAttribute>();

			if (authAttributes.Any())
			{
				var tokenScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = TokenSecuritySchemeId,
					},
				};

				operation.Security = new List<OpenApiSecurityRequirement>
				{
					new()
					{
						{
							tokenScheme,
							new List<string>()
						},
					},
				};

				if (typeof(InstanceRequiredController).IsAssignableFrom(context.MethodInfo.DeclaringType))
					operation.Parameters.Insert(0, new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.InstanceIdHeader,
						},
					});
				else if (typeof(TransferController).IsAssignableFrom(context.MethodInfo.DeclaringType))
					if (context.MethodInfo.Name == nameof(TransferController.Upload))
						operation.RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								{
									MediaTypeNames.Application.Octet,
									new OpenApiMediaType
									{
										Schema = new OpenApiSchema
										{
											Type = "string",
											Format = "binary",
										},
									}
								},
							},
						};
					else if (context.MethodInfo.Name == nameof(TransferController.Download))
					{
						var twoHundredResponseContents = operation.Responses["200"].Content;
						var fileContent = twoHundredResponseContents[MediaTypeNames.Application.Json];
						twoHundredResponseContents.Remove(MediaTypeNames.Application.Json);
						twoHundredResponseContents.Add(MediaTypeNames.Application.Octet, fileContent);
					}
			}
			else if (context.MethodInfo.Name == nameof(ApiRootController.CreateToken))
			{
				var passwordScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = PasswordSecuritySchemeId,
					},
				};

				var oAuthScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = OAuthSecuritySchemeId,
					},
				};

				operation.Parameters.Add(new OpenApiParameter
				{
					In = ParameterLocation.Header,
					Name = ApiHeaders.OAuthProviderHeader,
					Description = "The external OAuth service provider.",
					Style = ParameterStyle.Simple,
					Example = new OpenApiString("Discord"),
					Schema = new OpenApiSchema
					{
						Type = "string",
					},
				});

				operation.Security = new List<OpenApiSecurityRequirement>
				{
					new()
					{
						{
							passwordScheme,
							new List<string>()
						},
						{
							oAuthScheme,
							new List<string>()
						},
					},
				};
			}
		}

		/// <inheritdoc />
		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			ArgumentNullException.ThrowIfNull(swaggerDoc);
			ArgumentNullException.ThrowIfNull(context);

			swaggerDoc.ExternalDocs = new OpenApiExternalDocs
			{
				Description = "API Usage Documentation",
				Url = new Uri("https://tgstation.github.io/tgstation-server/api.html"),
			};

			swaggerDoc.Components.Parameters.Add(ApiHeaders.InstanceIdHeader, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = ApiHeaders.InstanceIdHeader,
				Description = "The instance ID being accessed",
				Required = true,
				Style = ParameterStyle.Simple,
				Schema = new OpenApiSchema
				{
					Type = "integer",
				},
			});

			var productHeaderSchema = new OpenApiSchema
			{
				Type = "string",
				Format = "productheader",
			};

			swaggerDoc.Components.Parameters.Add(ApiHeaders.ApiVersionHeader, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = ApiHeaders.ApiVersionHeader,
				Description = "The API version being used in the form \"Tgstation.Server.Api/[API version]\"",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString($"Tgstation.Server.Api/{ApiHeaders.Version}"),
				Schema = productHeaderSchema,
			});

			swaggerDoc.Components.Parameters.Add(HeaderNames.UserAgent, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = HeaderNames.UserAgent,
				Description = "The user agent of the calling client.",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString("Your-user-agent/1.0.0.0"),
				Schema = productHeaderSchema,
			});

			var allSchemas = context
				.SchemaRepository
				.Schemas;
			foreach (var path in swaggerDoc.Paths)
				foreach (var operation in path.Value.Operations.Select(x => x.Value))
				{
					operation.Parameters.Insert(0, new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.ApiVersionHeader,
						},
					});

					operation.Parameters.Insert(1, new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = HeaderNames.UserAgent,
						},
					});
				}

			AddDefaultResponses(swaggerDoc);
		}

		/// <inheritdoc />
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			ArgumentNullException.ThrowIfNull(schema);
			ArgumentNullException.ThrowIfNull(context);

			// Nothing is required
			schema.Required.Clear();

			if (context.MemberInfo == null)
				ApplyAttributesForRootSchema(schema, context);

			if (!schema.Enum?.Any() ?? false)
				return;

			// Could be nullable type, make sure to get the right one
			Type firstGenericArgumentOrType = context.Type.IsConstructedGenericType
				? context.Type.GenericTypeArguments.First()
				: context.Type;

			OpenApiEnumVarNamesExtension.Apply(schema, firstGenericArgumentOrType);
		}

		/// <inheritdoc />
		public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
		{
			ArgumentNullException.ThrowIfNull(requestBody);
			ArgumentNullException.ThrowIfNull(context);

			requestBody.Required = true;
		}
	}
}
