using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tgstation.Server.Api
{
	public sealed class ApiHeaders
	{
		const string userAgentHeader = "User-Agent";
		const string apiVersionHeader = "Api-Version";
		const string tokenHeader = "Token";
        const string usernameHeader = "Username";
        const string passwordHeader = "Password";

        public static readonly Version CurrentApiVersion = Assembly.GetExecutingAssembly().GetName().Version;

        public IReadOnlyDictionary<string, string> HeaderEntries => headerEntries;

        public string UserAgent {
            get => headerEntries[userAgentHeader];
            private set => headerEntries[userAgentHeader] = value;
        }

        public Version ApiVersion
        {
            get => new Version(headerEntries[apiVersionHeader]);
            private set => headerEntries[userAgentHeader] = value.ToString();
        }

        public string Token
        {
            get => headerEntries[tokenHeader];
            private set => headerEntries[tokenHeader] = value;
        }

        public string Username
        {
            get => headerEntries[usernameHeader];
            private set => headerEntries[usernameHeader] = value;
        }

        public string Password
        {
            get => headerEntries[passwordHeader];
            private set => headerEntries[passwordHeader] = value;
        }

        public bool IsTokenAuthentication => headerEntries.TryGetValue(tokenHeader, out string value);

        readonly Dictionary<string, string> headerEntries;

        public ApiHeaders(string userAgent, string token) : this(userAgent, token, null, null)
        {
            if (userAgent == null)
                throw new ArgumentNullException(nameof(userAgent));
            if (token == null)
                throw new ArgumentNullException(nameof(token));
        }

        public ApiHeaders(string userAgent, string username, string password) : this(userAgent, null, username, password)
        {
            if (userAgent == null)
                throw new ArgumentNullException(nameof(userAgent));
            if (username == null)
                throw new ArgumentNullException(nameof(username));
            if (password == null)
                throw new ArgumentNullException(nameof(password));
        }

        public ApiHeaders(IReadOnlyDictionary<string, string> headerEntries)
        {
            this.headerEntries = headerEntries?.ToDictionary(x => x.Key, x => x.Value) ?? throw new ArgumentNullException(nameof(headerEntries));
            AssertHeader(userAgentHeader);
            AssertHeader(apiVersionHeader);
            try
            {
                AssertHeader(usernameHeader);
                AssertHeader(passwordHeader);
            }
            catch (InvalidOperationException)
            {
                AssertHeader(tokenHeader);
            }
        }

        void AssertHeader(string headerName)
        {
            try
            {
                var headerValue = headerEntries[headerName];
            }
            catch(Exception e)
            {
                throw new InvalidOperationException("Missing required header!", e);
            }
        }

        ApiHeaders(string userAgent, string token, string username, string password)
        {
            headerEntries = new Dictionary<string, string>();
            ApiVersion = CurrentApiVersion;
            UserAgent = userAgent;
            Token = token;
            Username = username;
            Password = password;
        }
    }
}
