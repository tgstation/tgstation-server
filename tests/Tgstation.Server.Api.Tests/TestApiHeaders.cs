using System;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Api.Tests
{
	/// <summary>
	/// Tests for <see cref="ApiHeaders"/>
	/// </summary>
	[TestClass]
	public sealed class TestApiHeaders
	{
		readonly ProductHeaderValue productHeaderValue = new("Tgstation.Server.Api.Tests", "1.0.0");

		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new ApiHeaders(null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new ApiHeaders(productHeaderValue, null));
			var headers = new ApiHeaders(productHeaderValue, new TokenResponse { Bearer = String.Empty });
			headers = new ApiHeaders(productHeaderValue, String.Empty, OAuthProvider.GitHub);
		}

		[TestMethod]
		public void TestUserAgentsAreValid()
		{
			const string BrowserHeader = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36.";
			const string ConformantHeader = "TGSClient/3.2.1.4";

			static ApiHeaders TestHeader(string userAgent)
			{
				var headers = new HeaderDictionary
				{
					{ "Accept", MediaTypeNames.Application.Json },
					{ "Api", "Tgstation.Server.Api/4.0.0.0" },
					{ "Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJuYmYiOjEyMzR9.0CsmEwXt9oNTDisikbZ-MUr1eXSMKD8YKdZIOwMeLoc" }, // fake, but we need a valid token to avoid errors
					{ "User-Agent", userAgent }
				};

				return new ApiHeaders(new RequestHeaders(headers), false, false);
			}
			;

			var header = TestHeader(BrowserHeader);
			Assert.AreEqual(BrowserHeader, header.RawUserAgent);
			Assert.IsNull(header.UserAgent);

			header = TestHeader(ConformantHeader);
			Assert.AreEqual(ConformantHeader, header.RawUserAgent);
			Assert.IsNotNull(header.UserAgent);

			Assert.ThrowsExactly<HeadersException>(() => TestHeader(String.Empty));
		}

		[TestMethod]
		public void TestBasicAuthenticationDeserialization()
		{
			const string BrowserHeader = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36.";
			const string Password = "askdjf::SDokjdf**";
			const string Username = "deeznuts";

			var authHeader = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"))}";
			var headers = new HeaderDictionary
				{
					{ "Accept", MediaTypeNames.Application.Json },
					{ "Api", "Tgstation.Server.Api/4.0.0.0" },
					{ "Authorization", authHeader },
					{ "User-Agent", BrowserHeader },
				};

			var header = new ApiHeaders(new RequestHeaders(headers), false, false);

			Assert.AreEqual(BrowserHeader, header.RawUserAgent);
			Assert.AreEqual(Username, header.Username);
			Assert.AreEqual(Password, header.Password);
		}
	}
}
