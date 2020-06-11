using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
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
		/// The <see cref="OpenApiSecurityScheme"/> name for token authentication.
		/// </summary>
		const string TokenSecuritySchemeId = "Token_Authorization_Scheme";

		static void AddDefaultResponses(OpenApiDocument document)
		{
			var errorMessageContent = new Dictionary<string, OpenApiMediaType>
			{
				{
					ApiHeaders.ApplicationJson,
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
					Version = ApiHeaders.Version.Semver().ToString()
				});

			// Important to do this before applying our own filters
			// Otherwise we'll get NullReferenceExceptions on parameters to be setup in our document filter
			swaggerGenOptions.IncludeXmlComments(assemblyDocumentationPath);
			swaggerGenOptions.IncludeXmlComments(apiDocumentationPath);

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

			swaggerGenOptions.AddSecurityDefinition(TokenSecuritySchemeId, new OpenApiSecurityScheme
			{
				BearerFormat = "JWT",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Name = HeaderNames.Authorization,
				Scheme = ApiHeaders.JwtAuthenticationScheme
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

				if (authAttributes.Any(attr => attr.RightsType.HasValue && RightsHelper.IsInstanceRight(attr.RightsType.Value)))
					operation.Parameters.Add(new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.InstanceIdHeader
						}
					});
			}
			else
			{
				// HomeController.CreateToken
				var passwordScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = PasswordSecuritySchemeId
					}
				};

				operation.Security = new List<OpenApiSecurityRequirement>
				{
					new OpenApiSecurityRequirement
					{
						{
							passwordScheme,
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

			string bridgeOperationPath = null;
			foreach (var path in swaggerDoc.Paths)
				foreach (var operation in path.Value.Operations.Select(x => x.Value))
				{
					if (operation.OperationId.Equals($"{nameof(BridgeController)}.{nameof(BridgeController.Process)}", StringComparison.Ordinal))
					{
						bridgeOperationPath = path.Key;
						continue;
					}

					operation.Parameters.Add(new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = ApiHeaders.ApiVersionHeader
						},
					});

					operation.Parameters.Add(new OpenApiParameter
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.Parameter,
							Id = HeaderNames.UserAgent
						}
					});
				}

			swaggerDoc.Paths.Remove(bridgeOperationPath);

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

			if (!schema.Enum?.Any() ?? false)
				return;

			// Could be nullable type, make sure to get the right one
			Type enumType = context.Type.IsConstructedGenericType
				? context.Type.GenericTypeArguments.First()
				: context.Type;

			OpenApiEnumVarNamesExtension.Apply(schema, enumType);
		}
	}
}
