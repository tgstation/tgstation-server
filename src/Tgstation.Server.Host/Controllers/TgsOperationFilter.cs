using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="IOperationFilter"/> for the server.
	/// </summary>
	sealed class TgsOperationFilter : IOperationFilter
	{
		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for password authentication.
		/// </summary>
		public const string PasswordSecuritySchemeId = "Password_Login";

		/// <summary>
		/// The <see cref="OpenApiSecurityScheme"/> name for token authentication.
		/// </summary>
		public const string TokenSecuritySchemeId = "Token_Authorization";

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

			// stub var because debugger conditions are bad
			if (authAttributes.Any())
			{
				var tokenScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = TokenSecuritySchemeId }
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
						In = ParameterLocation.Header,
						Description = "The instance ID being accessed",
						Name = ApiHeaders.InstanceIdHeader,
						Required = true,
						Style = ParameterStyle.Simple
					});
			}
			else
			{
				// HomeController.CreateToken
				var passwordScheme = new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = PasswordSecuritySchemeId }
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

				operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = "_Login" } };
			}

			operation.Parameters.Add(new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Description = "The API version being used in the form \"Tgstation.Server.Api/[API version]\"",
				Name = ApiHeaders.ApiVersionHeader,
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString($"Tgstation.Server.Api/{ApiHeaders.Version}")
			});

			operation.Parameters.Add(new OpenApiParameter
			{
				In = ParameterLocation.Header,
				Description = "The user agent of the calling client.",
				Name = "User-Agent",
				Required = true,
				Style = ParameterStyle.Simple,
				Example = new OpenApiString("Your-user-agent/1.0.0.0")
			});

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

			// Add default common status codes
			operation.Responses.TryAdd("400", new OpenApiResponse
			{
				Description = "A badly formatted request was made. See error message for details.",
				Content = errorMessageContent
			});

			operation.Responses.TryAdd("401", new OpenApiResponse
			{
				Description = "No/invalid token provided."
			});

			operation.Responses.TryAdd("403", new OpenApiResponse
			{
				Description = "User lacks sufficient permissions for the operation."
			});

			operation.Responses.TryAdd("409", new OpenApiResponse
			{
				Description = "A data integrity check failed while performing the operation. See error message for details.",
				Content = errorMessageContent
			});

			operation.Responses.TryAdd("500", new OpenApiResponse
			{
				Description = "The server encountered an unhandled error. See error message for details.",
				Content = errorMessageContent
			});

			operation.Responses.TryAdd("503", new OpenApiResponse
			{
				Description = "The server may be starting up or shutting down."
			});
		}
	}
}
