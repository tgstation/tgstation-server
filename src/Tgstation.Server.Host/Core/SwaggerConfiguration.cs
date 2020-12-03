using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Controllers;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Implements various filters for <see cref="Swashbuckle"/>.
	/// </summary>
	sealed class SwaggerConfiguration : IOperationFilter, IDocumentFilter, ISchemaFilter
	{
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
								Id = nameof(ErrorMessage),
								Type = ReferenceType.Schema
							}
						}
					}
				}
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
						Id = responseKey
					}
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
				Description = "Invalid Authentication header."
			});

			AddDefaultResponse(HttpStatusCode.Forbidden, new OpenApiResponse
			{
				Description = "User lacks sufficient permissions for the operation."
			});

			AddDefaultResponse(HttpStatusCode.Conflict, new OpenApiResponse
			{
				Description = "A data integrity check failed while performing the operation. See error message for details.",
				Content = errorMessageContent
			});

			AddDefaultResponse(HttpStatusCode.NotAcceptable, new OpenApiResponse
			{
				Description = $"Invalid Accept header, TGS requires `{HeaderNames.Accept}: {MediaTypeNames.Application.Json}`.",
				Content = errorMessageContent
			});

			AddDefaultResponse(HttpStatusCode.InternalServerError, new OpenApiResponse
			{
				Description = ErrorCode.InternalServerError.Describe(),
				Content = errorMessageContent
			});

			AddDefaultResponse(HttpStatusCode.ServiceUnavailable, new OpenApiResponse
			{
				Description = "The server may be starting up or shutting down."
			});

			AddDefaultResponse(HttpStatusCode.NotImplemented, new OpenApiResponse
			{
				Description = ErrorCode.RequiresPosixSystemIdentity.Describe(),
				Content = errorMessageContent
			});
		}

		/// <summary>
		/// Configure the swagger settings.
		/// </summary>
		/// <param name="swaggerGenOptions">The <see cref="SwaggerGenOptions"/> to use.</param>
		/// <param name="assemblyDocumentationPath">The path to the XML documentation file for the <see cref="Host"/> assembly.</param>
		/// <param name="apiDocumentationPath">The path to the XML documentation file for the <see cref="Api"/> assembly.</param>
		public static void Configure(SwaggerGenOptions swaggerGenOptions, string assemblyDocumentationPath, string apiDocumentationPath)
		{
			swaggerGenOptions.SwaggerDoc(
				"v1",
				new OpenApiInfo
				{
					Title = "TGS API",
					Version = ApiHeaders.Version.Semver().ToString(),
					License = new OpenApiLicense
					{
						Name = "AGPL-3.0",
						Url = new Uri("https://github.com/tgstation/tgstation-server/blob/dev/LICENSE")
					},
					Contact = new OpenApiContact
					{
						Name = "/tg/station 13",
						Url = new Uri("https://github.com/tgstation")
					},
					Description = "A production scale tool for BYOND server management"
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

			swaggerGenOptions.CustomSchemaIds(type =>
			{
				if (type == typeof(Api.Models.Internal.User))
					return "ShallowUser";

				return type.Name;
			});

			swaggerGenOptions.AddSecurityDefinition(PasswordSecuritySchemeId, new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.BasicAuthenticationScheme
			});

			swaggerGenOptions.AddSecurityDefinition(OAuthSecuritySchemeId, new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.OAuthAuthenticationScheme
			});

			swaggerGenOptions.AddSecurityDefinition(TokenSecuritySchemeId, new OpenApiSecurityScheme
			{
				BearerFormat = "JWT",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.BearerAuthenticationScheme
			});
		}

		/// <inheritdoc />
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (operation == null)
				throw new ArgumentNullException(nameof(operation));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			operation.OperationId = $"{context.MethodInfo.DeclaringType.Name}.{context.MethodInfo.Name}";

			// request bodies are never nullable
			var bodySchemas = operation.RequestBody?.Content.Select(x => x.Value.Schema) ?? Enumerable.Empty<OpenApiSchema>();
			foreach (var bodySchema in bodySchemas)
				bodySchema.Nullable = false;

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
						Id = TokenSecuritySchemeId
					}
				};

				operation.Security = new List<OpenApiSecurityRequirement>
				{
					new OpenApiSecurityRequirement
					{
						{
							tokenScheme,
							new List<string>()
						}
					}
				};

				if (typeof(InstanceRequiredController).IsAssignableFrom(context.MethodInfo.DeclaringType))
					operation.Parameters.Add(new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.InstanceIdHeader
						}
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
											Format = "binary"
										}
									}
								}
							}
						};
					else if (context.MethodInfo.Name == nameof(TransferController.Download))
					{
						var twoHundredResponseContents = operation.Responses["200"].Content;
						var fileContent = twoHundredResponseContents[MediaTypeNames.Application.Json];
						twoHundredResponseContents.Remove(MediaTypeNames.Application.Json);
						twoHundredResponseContents.Add(MediaTypeNames.Application.Octet, fileContent);
					}
			}
			else if (context.MethodInfo.Name == nameof(HomeController.CreateToken))
			{
				var passwordScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = PasswordSecuritySchemeId
					}
				};

				var oAuthScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = OAuthSecuritySchemeId
					}
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
						Type = "string"
					}
				});

				operation.Security = new List<OpenApiSecurityRequirement>
				{
					new OpenApiSecurityRequirement
					{
						{
							passwordScheme,
							new List<string>()
						},
						{
							oAuthScheme,
							new List<string>()
						}
					}
				};
			}
		}

		/// <inheritdoc />
		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			if (swaggerDoc == null)
				throw new ArgumentNullException(nameof(swaggerDoc));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			swaggerDoc.Components.Parameters.Add(ApiHeaders.InstanceIdHeader, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = ApiHeaders.InstanceIdHeader,
				Description = "The instance ID being accessed",
				Required = true,
				Style = ParameterStyle.Simple,
				Schema = new OpenApiSchema
				{
					Type = "integer"
				}
			});

			var productHeaderSchema = new OpenApiSchema
			{
				Type = "string",
				Format = "productheader"
			};

			swaggerDoc.Components.Parameters.Add(ApiHeaders.ApiVersionHeader, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = ApiHeaders.ApiVersionHeader,
				Description = "The API version being used in the form \"Tgstation.Server.Api/[API version]\"",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString($"Tgstation.Server.Api/{ApiHeaders.Version}"),
				Schema = productHeaderSchema
			});

			swaggerDoc.Components.Parameters.Add(HeaderNames.UserAgent, new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Name = HeaderNames.UserAgent,
				Description = "The user agent of the calling client.",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString("Your-user-agent/1.0.0.0"),
				Schema = productHeaderSchema
			});

			var pathsToRemove = new List<string>();
			var filteredControllers = new string[]
			{
				nameof(BridgeController),
				nameof(ControlPanelController),
			};

			foreach (var path in swaggerDoc.Paths)
				foreach (var operation in path.Value.Operations.Select(x => x.Value))
				{
					if (filteredControllers.Any(
						x => operation.OperationId.StartsWith(x, StringComparison.Ordinal)))
					{
						pathsToRemove.Add(path.Key);
						continue;
					}

					operation.Parameters.Insert(0, new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.ApiVersionHeader
						},
					});

					operation.Parameters.Insert(1, new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = HeaderNames.UserAgent
						}
					});
				}

			foreach (var filteredPath in pathsToRemove)
				swaggerDoc.Paths.Remove(filteredPath);

			AddDefaultResponses(swaggerDoc);
		}

		/// <inheritdoc />
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			// Nothing is required
			schema.Required.Clear();

			// Could be nullable type, make sure to get the right one
			Type nonNullableType = context.Type.IsConstructedGenericType
				? context.Type.GenericTypeArguments.First()
				: context.Type;

			if (nonNullableType != context.Type
				&& !context.Type.GetInterfaces().Any(x => x == typeof(IEnumerable)))
				schema.Nullable = true;

			if (!schema.Enum?.Any() ?? false)
				return;

			OpenApiEnumVarNamesExtension.Apply(schema, nonNullableType);
		}
	}
}
