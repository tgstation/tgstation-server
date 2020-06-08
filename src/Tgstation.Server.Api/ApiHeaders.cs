using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Represents the header that must be present for every server request
	/// </summary>
	public sealed class ApiHeaders
	{
		/// <summary>
		/// TODO: Remove this when we upgrade to .NET Standard 2.1
		/// </summary>
		public const string ApplicationJson = "application/json";

		/// <summary>
		/// The <see cref="ApiVersion"/> header key
		/// </summary>
		public const string ApiVersionHeader = "api";

		/// <summary>
		/// The <see cref="InstanceId"/> header key
		/// </summary>
		public const string InstanceIdHeader = "instance";

		/// <summary>
		/// The JWT authentication header scheme
		/// </summary>
		public const string JwtAuthenticationScheme = "bearer";

		/// <summary>
		/// The JWT authentication header scheme
		/// </summary>
		public const string BasicAuthenticationScheme = "basic";

		/// <summary>
		/// The <see cref="Username"/> header key
		/// </summary>
		const string UsernameHeader = "username";

		/// <summary>
		/// The basic authentication header scheme
		/// </summary>
		const string PasswordAuthenticationScheme = "password";

		/// <summary>
		/// The current <see cref="System.Reflection.AssemblyName"/>
		/// </summary>
		static readonly AssemblyName AssemblyName = Assembly.GetExecutingAssembly().GetName();

		/// <summary>
		/// Get the version of the <see cref="Api"/> the caller is using
		/// </summary>
		public static readonly Version Version = AssemblyName.Version.Semver();

		/// <summary>
		/// The instance <see cref="Models.EntityId.Id"/> being accessed
		/// </summary>
		public long? InstanceId { get; set; }

		/// <summary>
		/// The client's user agent as a <see cref="ProductHeaderValue"/> if valid
		/// </summary>
		public ProductHeaderValue? UserAgent => ProductInfoHeaderValue.TryParse(RawUserAgent, out var userAgent) ? userAgent.Product : null;

		/// <summary>
		/// The client's raw user agent
		/// </summary>
		public string? RawUserAgent { get; }

		/// <summary>
		/// The client's API version
		/// </summary>
		public Version ApiVersion { get; }

		/// <summary>
		/// The client's JWT
		/// </summary>
		public string? Token { get; }

		/// <summary>
		/// The client's username
		/// </summary>
		public string? Username { get; }

		/// <summary>
		/// The client's password
		/// </summary>
		public string? Password { get; }

		/// <summary>
		/// If the header uses password or JWT authentication
		/// </summary>
		public bool IsTokenAuthentication => Token != null;

		/// <summary>
		/// Checks if a given <paramref name="otherVersion"/> is compatible with our own
		/// </summary>
		/// <param name="otherVersion">The <see cref="Version"/> to test</param>
		/// <returns><see langword="true"/> if the given version is compatible with the API. <see langword="false"/> otherwise</returns>
		public static bool CheckCompatibility(Version otherVersion) => Version.Major == (otherVersion?.Major ?? throw new ArgumentNullException(nameof(otherVersion)));

		/// <summary>
		/// Construct <see cref="ApiHeaders"/> for JWT authentication
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="token">The value of <see cref="Token"/></param>
		public ApiHeaders(ProductHeaderValue userAgent, string token) : this(userAgent, token, null, null)
		{
			if (userAgent == null)
				throw new ArgumentNullException(nameof(userAgent));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
		}

		/// <summary>
		/// Construct <see cref="ApiHeaders"/> for password authentication
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="username">The value of <see cref="Username"/></param>
		/// <param name="password">The value of <see cref="Password"/></param>
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
		/// Construct and validates <see cref="ApiHeaders"/> from a set of <paramref name="requestHeaders"/>
		/// </summary>
		/// <param name="requestHeaders">The <see cref="RequestHeaders"/> containing the <see cref="ApiHeaders"/></param>
		public ApiHeaders(RequestHeaders requestHeaders)
		{
			if (requestHeaders == null)
				throw new ArgumentNullException(nameof(requestHeaders));

			var jsonAccept = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(ApplicationJson);
			if (!requestHeaders.Accept.Any(x => x.MediaType == jsonAccept.MediaType))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Client does not accept {0}!", ApplicationJson));

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) || userAgentValues.Count == 0)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Missing {0} headers!", HeaderNames.UserAgent));

			RawUserAgent = userAgentValues.First();
			if (String.IsNullOrWhiteSpace(RawUserAgent))
				throw new InvalidOperationException("Malformed client User-Agent!");

			// make sure the api header matches ours
			if (!requestHeaders.Headers.TryGetValue(ApiVersionHeader, out var apiUserAgentHeaderValues) || !ProductInfoHeaderValue.TryParse(apiUserAgentHeaderValues.FirstOrDefault(), out var apiUserAgent) || apiUserAgent.Product.Name != AssemblyName.Name)
				throw new InvalidOperationException("Missing API version!");

			if (!Version.TryParse(apiUserAgent.Product.Version, out var apiVersion))
				throw new InvalidOperationException("Malformed API version!");

			ApiVersion = apiVersion.Semver();

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorization))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Missing {0} header!", HeaderNames.Authorization));
			var auth = authorization.First();
			var splits = new List<string>(auth.Split(' '));
			var scheme = splits.First();
			if (String.IsNullOrWhiteSpace(scheme))
				throw new InvalidOperationException("Missing authentication scheme!");

			splits.RemoveAt(0);
			var parameter = String.Concat(splits);
			if (String.IsNullOrEmpty(parameter))
				throw new InvalidOperationException("Missing authentication parameter!");

			if (requestHeaders.Headers.TryGetValue(InstanceIdHeader, out var instanceIdValues))
			{
				var instanceIdString = instanceIdValues.FirstOrDefault();
				if (instanceIdString != default && Int64.TryParse(instanceIdString, out var instanceId))
					InstanceId = instanceId;
			}

#pragma warning disable CA1308 // Normalize strings to uppercase
			switch (scheme.ToLowerInvariant())
#pragma warning restore CA1308 // Normalize strings to uppercase
			{
				case JwtAuthenticationScheme:
					Token = parameter;
					break;
				case PasswordAuthenticationScheme:
					Password = parameter;
					var fail = !requestHeaders.Headers.TryGetValue(UsernameHeader, out var values);
					if (!fail)
					{
						Username = values.FirstOrDefault();
						fail = String.IsNullOrWhiteSpace(Username);
					}

					if (fail)
						throw new InvalidOperationException("Missing Username header!");
					break;
				case BasicAuthenticationScheme:
					string joinedString;
					try
					{
						var base64Bytes = Convert.FromBase64String(parameter);
						joinedString = Encoding.UTF8.GetString(base64Bytes);
					}
					catch
					{
						throw new InvalidOperationException("Invalid basic Authorization header!");
					}

					var basicAuthSplits = joinedString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					if (basicAuthSplits.Length < 2)
						throw new InvalidOperationException("Invalid basic Authorization header!");

					Username = basicAuthSplits.First();
					Password = String.Concat(basicAuthSplits.Skip(1));
					break;
				default:
					throw new InvalidOperationException("Invalid authentication scheme!");
			}
		}

		/// <summary>
		/// Construct <see cref="ApiHeaders"/>
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="token">The value of <see cref="Token"/></param>
		/// <param name="username">The value of <see cref="Username"/></param>
		/// <param name="password">The value of <see cref="Password"/></param>
		ApiHeaders(ProductHeaderValue userAgent, string? token, string? username, string? password)
		{
			RawUserAgent = userAgent?.ToString();
			Token = token;
			Username = username;
			Password = password;
			ApiVersion = Version;
		}

		/// <summary>
		/// Checks if the <see cref="ApiVersion"/> is compatible with <see cref="Version"/>
		/// </summary>
		/// <returns><see langword="true"/> if the API is compatible, <see langword="false"/> otherwise</returns>
		public bool Compatible() => CheckCompatibility(ApiVersion);

		/// <summary>
		/// Set <see cref="HttpRequestHeaders"/> using the <see cref="ApiHeaders"/>. This initially clears <paramref name="headers"/>
		/// </summary>
		/// <param name="headers">The <see cref="HttpRequestHeaders"/> to set</param>
		/// <param name="instanceId">The instance <see cref="Models.EntityId.Id"/> for the request</param>
		public void SetRequestHeaders(HttpRequestHeaders headers, long? instanceId = null)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));
			if (instanceId.HasValue && InstanceId.HasValue && instanceId != InstanceId)
				throw new InvalidOperationException("Specified instance ID in constructor and SetRequestHeaders!");

			headers.Clear();
			headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJson));
			if (IsTokenAuthentication)
				headers.Authorization = new AuthenticationHeaderValue(JwtAuthenticationScheme, Token);
			else
				headers.Authorization = new AuthenticationHeaderValue(
					BasicAuthenticationScheme,
					Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));

			headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
			headers.Add(ApiVersionHeader, new ProductHeaderValue(AssemblyName.Name, ApiVersion.ToString()).ToString());
			instanceId ??= InstanceId;
			if (instanceId.HasValue)
				headers.Add(InstanceIdHeader, instanceId.ToString());
		}
	}
}
