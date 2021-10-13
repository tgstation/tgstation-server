using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Properties;
using Tgstation.Server.Helpers;
using Tgstation.Server.Helpers.Extensions;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Represents the header that must be present for every server request.
	/// </summary>
	public sealed class ApiHeaders
	{
		/// <summary>
		/// The <see cref="ApiVersion"/> header key.
		/// </summary>
		public const string ApiVersionHeader = "Api";

		/// <summary>
		/// The <see cref="InstanceId"/> header key.
		/// </summary>
		public const string InstanceIdHeader = "Instance";

		/// <summary>
		/// The <see cref="OAuthProvider"/> header key.
		/// </summary>
		public const string OAuthProviderHeader = "OAuthProvider";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string BearerAuthenticationScheme = "Bearer";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string BasicAuthenticationScheme = "Basic";

		/// <summary>
		/// The JWT authentication header scheme.
		/// </summary>
		public const string OAuthAuthenticationScheme = "OAuth";

		/// <summary>
		/// Get the version of the <see cref="Api"/> the caller is using.
		/// </summary>
		public static readonly Version Version = Version.Parse(ApiVersionAttribute.Instance.RawApiVersion);

		/// <summary>
		/// The current <see cref="System.Reflection.AssemblyName"/>.
		/// </summary>
		static readonly AssemblyName AssemblyName = Assembly.GetExecutingAssembly().GetName();

		/// <summary>
		/// The instance <see cref="EntityId.Id"/> being accessed.
		/// </summary>
		public long? InstanceId { get; set; }

		/// <summary>
		/// The client's user agent as a <see cref="ProductHeaderValue"/> if valid.
		/// </summary>
		public ProductHeaderValue? UserAgent => RawUserAgent != null && ProductInfoHeaderValue.TryParse(RawUserAgent, out var userAgent) ? userAgent.Product : null;

		/// <summary>
		/// The client's raw user agent.
		/// </summary>
		public string? RawUserAgent { get; }

		/// <summary>
		/// The client's API version.
		/// </summary>
		public Version ApiVersion { get; }

		/// <summary>
		/// The client's JWT.
		/// </summary>
		public string? Token { get; }

		/// <summary>
		/// The client's username.
		/// </summary>
		public string? Username { get; }

		/// <summary>
		/// The client's password.
		/// </summary>
		public string? Password { get; }

		/// <summary>
		/// The <see cref="Models.OAuthProvider"/> the <see cref="Token"/> is for, if any.
		/// </summary>
		public OAuthProvider? OAuthProvider { get; }

		/// <summary>
		/// If the header uses TGS JWT authentication.
		/// </summary>
		[MemberNotNullWhen(true, nameof(Token))]
		public bool IsTokenAuthentication => Token != null && !OAuthProvider.HasValue;

		/// <summary>
		/// Checks if a given <paramref name="otherVersion"/> is compatible with our own.
		/// </summary>
		/// <param name="otherVersion">The <see cref="Version"/> to test.</param>
		/// <returns><see langword="true"/> if the given version is compatible with the API. <see langword="false"/> otherwise.</returns>
		public static bool CheckCompatibility(Version otherVersion) => Version.Major == (otherVersion?.Major ?? throw new ArgumentNullException(nameof(otherVersion)));

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class. Used for token authentication.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="token">The value of <see cref="Token"/>.</param>
		/// <param name="oauthProvider">The value of <see cref="OAuthProvider"/>.</param>
		public ApiHeaders(ProductHeaderValue userAgent, string token, OAuthProvider? oauthProvider = null) : this(userAgent, token, null, null)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			OAuthProvider = oauthProvider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class. Used for password authentication.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="username">The value of <see cref="Username"/>.</param>
		/// <param name="password">The value of <see cref="Password"/>.</param>
		public ApiHeaders(ProductHeaderValue userAgent, string username, string password) : this(userAgent, null, username, password)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class.
		/// </summary>
		/// <param name="requestHeaders">The <see cref="RequestHeaders"/> containing the serialized <see cref="ApiHeaders"/>.</param>
		/// <param name="ignoreMissingAuth">If a missing <see cref="HeaderNames.Authorization"/> should be ignored.</param>
		/// <exception cref="HeadersException">Thrown if the <paramref name="requestHeaders"/> constitue invalid <see cref="ApiHeaders"/>.</exception>
#pragma warning disable CA1502
		public ApiHeaders(RequestHeaders requestHeaders, bool ignoreMissingAuth = false)
		{
			if (requestHeaders == null)
				throw new ArgumentNullException(nameof(requestHeaders));

			var badHeaders = HeaderTypes.None;
			var errorBuilder = new StringBuilder();

			void AddError(HeaderTypes headerType, string message)
			{
				if (badHeaders != HeaderTypes.None)
					errorBuilder.AppendLine();
				badHeaders |= headerType;
				errorBuilder.Append(message);
			}

			var jsonAccept = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Application.Json);
			if (!requestHeaders.Accept.Any(x => jsonAccept.IsSubsetOf(x)))
				AddError(HeaderTypes.Accept, $"Client does not accept {MediaTypeNames.Application.Json}!");

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) || userAgentValues.Count == 0)
				AddError(HeaderTypes.UserAgent, $"Missing {HeaderNames.UserAgent} header!");
			else
			{
				RawUserAgent = userAgentValues.First();
				if (String.IsNullOrWhiteSpace(RawUserAgent))
					AddError(HeaderTypes.UserAgent, $"Malformed {HeaderNames.UserAgent} header!");
			}

			// make sure the api header matches ours
			Version? apiVersion = null;
			if (!requestHeaders.Headers.TryGetValue(ApiVersionHeader, out var apiUserAgentHeaderValues)
				|| !ProductInfoHeaderValue.TryParse(apiUserAgentHeaderValues.FirstOrDefault() ?? String.Empty, out var apiUserAgent)
				|| apiUserAgent!.Product!.Name != AssemblyName.Name)
				AddError(HeaderTypes.Api, $"Missing {ApiVersionHeader} header!");
			else if (!Version.TryParse(apiUserAgent.Product.Version, out apiVersion))
				AddError(HeaderTypes.Api, $"Malformed {ApiVersionHeader} header!");

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorization))
			{
				if (!ignoreMissingAuth)
					AddError(HeaderTypes.Authorization, $"Missing {HeaderNames.Authorization} header!");
			}
			else
			{
				var auth = authorization.First();
				var splits = new List<string>(auth.Split(' '));
				var scheme = splits.First();
				if (String.IsNullOrWhiteSpace(scheme))
					AddError(HeaderTypes.Authorization, "Missing authentication scheme!");
				else
				{
					splits.RemoveAt(0);
					var parameter = String.Concat(splits);
					if (String.IsNullOrEmpty(parameter))
						AddError(HeaderTypes.Authorization, "Missing authentication parameter!");
					else
					{
						if (requestHeaders.Headers.TryGetValue(InstanceIdHeader, out var instanceIdValues))
						{
							var instanceIdString = instanceIdValues.FirstOrDefault();
							if (instanceIdString != default && Int64.TryParse(instanceIdString, out var instanceId))
								InstanceId = instanceId;
						}

						switch (scheme)
						{
							case OAuthAuthenticationScheme:
								if (requestHeaders.Headers.TryGetValue(OAuthProviderHeader, out StringValues oauthProviderValues))
								{
									var oauthProviderString = oauthProviderValues.First();
									if (Enum.TryParse<OAuthProvider>(oauthProviderString, out var oauthProvider))
										OAuthProvider = oauthProvider;
									else
										AddError(HeaderTypes.OAuthProvider, "Invalid OAuth provider!");
								}
								else
									AddError(HeaderTypes.OAuthProvider, $"Missing {OAuthProviderHeader} header!");

								goto case BearerAuthenticationScheme;
							case BearerAuthenticationScheme:
								Token = parameter;
								break;
							case BasicAuthenticationScheme:
								string badBasicAuthHeaderMessage = $"Invalid basic {HeaderNames.Authorization} header!";
								string joinedString;
								try
								{
									var base64Bytes = Convert.FromBase64String(parameter);
									joinedString = Encoding.UTF8.GetString(base64Bytes);
								}
								catch
								{
									AddError(HeaderTypes.Authorization, badBasicAuthHeaderMessage);
									break;
								}

								var basicAuthSplits = joinedString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
								if (basicAuthSplits.Length < 2)
								{
									AddError(HeaderTypes.Authorization, badBasicAuthHeaderMessage);
									break;
								}

								Username = basicAuthSplits.First();
								Password = String.Concat(basicAuthSplits.Skip(1));
								break;
							default:
								AddError(HeaderTypes.Authorization, "Invalid authentication scheme!");
								break;
						}
					}
				}
			}

			if (badHeaders != HeaderTypes.None)
				throw new HeadersException(badHeaders, errorBuilder.ToString());

			ApiVersion = apiVersion!.Semver();
		}
#pragma warning restore CA1502

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeaders"/> class.
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/>.</param>
		/// <param name="token">The value of <see cref="Token"/>.</param>
		/// <param name="username">The value of <see cref="Username"/>.</param>
		/// <param name="password">The value of <see cref="Password"/>.</param>
		ApiHeaders(ProductHeaderValue userAgent, string? token, string? username, string? password)
		{
			RawUserAgent = userAgent?.ToString();
			Token = token;
			Username = username;
			Password = password;
			ApiVersion = Version;
		}

		/// <summary>
		/// Checks if the <see cref="ApiVersion"/> is compatible with <see cref="Version"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the API is compatible, <see langword="false"/> otherwise.</returns>
		public bool Compatible() => CheckCompatibility(ApiVersion);

		/// <summary>
		/// Set <see cref="HttpRequestHeaders"/> using the <see cref="ApiHeaders"/>. This initially clears <paramref name="headers"/>.
		/// </summary>
		/// <param name="headers">The <see cref="HttpRequestHeaders"/> to set.</param>
		/// <param name="instanceId">The instance <see cref="EntityId.Id"/> for the request.</param>
		public void SetRequestHeaders(HttpRequestHeaders headers, long? instanceId = null)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));
			if (instanceId.HasValue && InstanceId.HasValue && instanceId != InstanceId)
				throw new InvalidOperationException("Specified different instance IDs in constructor and SetRequestHeaders!");
			if (UserAgent == null)
				throw new InvalidOperationException("User-Agent is not set!");

			headers.Clear();
			headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
			headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
			headers.Add(ApiVersionHeader, new ProductHeaderValue(AssemblyName.Name!, ApiVersion.ToString()).ToString());
			if (OAuthProvider.HasValue)
			{
				headers.Authorization = new AuthenticationHeaderValue(OAuthAuthenticationScheme, Token);
				headers.Add(OAuthProviderHeader, OAuthProvider.ToString());
			}
			else if (!IsTokenAuthentication)
				headers.Authorization = new AuthenticationHeaderValue(
					BasicAuthenticationScheme,
					Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));
			else
				headers.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, Token);

			instanceId ??= InstanceId;
			if (instanceId.HasValue)
				headers.Add(InstanceIdHeader, instanceId.ToString());
		}
	}
}
