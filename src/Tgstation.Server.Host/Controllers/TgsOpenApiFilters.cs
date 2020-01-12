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

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="IOperationFilter"/> and <see cref="IDocumentFilter"/> for the server.
	/// </summary>
	sealed class TgsOpenApiFilters : IOperationFilter, IDocumentFilter
	{
		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for password authentication.
		/// </summary>
		public const string PasswordSecuritySchemeId = "Password_Login_Scheme";

		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for token authentication.
		/// </summary>
		public const string TokenSecuritySchemeId = "Token_Authorization_Scheme";

		/// <inheritdoc />
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (operation == null)
				throw new ArgumentNullException(nameof(operation));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

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
							Type = ReferenceType.Header,
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
			swaggerDoc.Components.Headers.Add(ApiHeaders.InstanceIdHeader, new OpenApiHeader
			{
				Description = "The instance ID being accessed",
				Required = true,
				Style = ParameterStyle.Simple
			});

			swaggerDoc.Components.Headers.Add(ApiHeaders.ApiVersionHeader, new OpenApiHeader
			{
				Description = "The API version being used in the form \"Tgstation.Server.Api/[API version]\"",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString($"Tgstation.Server.Api/{ApiHeaders.Version}")
			});

			swaggerDoc.Components.Headers.Add(HeaderNames.UserAgent, new OpenApiHeader
			{
				Description = "The user agent of the calling client.",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString("Your-user-agent/1.0.0.0")
			});

			foreach (var operation in swaggerDoc
				.Paths
				.SelectMany(path => path.Value.Operations)
				.Select(kvp => kvp.Value))
			{
				operation.Parameters.Add(new OpenApiParameter
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Header,
						Id = ApiHeaders.ApiVersionHeader
					}
				});

				operation.Parameters.Add(new OpenApiParameter
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Header,
						Id = HeaderNames.UserAgent
					}
				});
			}

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

				swaggerDoc.Components.Responses.Add(responseKey, concrete);

				var referenceResponse = new OpenApiResponse
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Response,
						Id = responseKey
					}
				};

				foreach (var path in swaggerDoc.Paths)
					foreach (var operation in path.Value.Operations)
						operation.Value.Responses.TryAdd(responseKey, referenceResponse);
			}

			AddDefaultResponse(HttpStatusCode.BadRequest, new OpenApiResponse
			{
				Description = "A badly formatted request was made. See error message for details.",
				Content = errorMessageContent,
			});

			AddDefaultResponse(HttpStatusCode.Unauthorized, new OpenApiResponse
			{
				Description = "No/invalid token provided."
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
				Description = "The server encountered an unhandled error. See error message for details.",
				Content = errorMessageContent
			});

			AddDefaultResponse(HttpStatusCode.ServiceUnavailable, new OpenApiResponse
			{
				Description = "The server may be starting up or shutting down."
			});
		}
	}
}
