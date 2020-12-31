# Security Classes

- [IAuthenticationContext](./IAuthenticationContext.cs) and [implementation](./AuthenticationContext.cs) is what contains information about an authenticated user for a request. Includes things like the relevant `InstanceUser` and any associated rights.
- [IAuthenticationContextFactory](./IAuthenticationContextFactory.cs) and [implementation](AuthenticationContextFactory.cs) is a factory for `IAuthenticationContext`s. It handles things related to the database for a user's authentication. This includes loading their rights/associated instance user. It will also stop the request if the users token was issued before the last time their password or enabled status was updated.
- [IClaimsInjector](./IClaimsInjector.cs) and [implementation](./ClaimsInjector.cs) is used to associate rights with a request context so that it may properly pass appropriate `TgsAuthorizeAttribute`s.
- [ICrytopgraphySuite](./ICrytopgraphySuite.cs) and [implementation](./CrytopgraphySuite.cs) is used to generate secure strings and byte arrays. It also contains the password hashing and validation logic.
- [IIdentityCache](./IIdentityCache.cs) and [implementation](./IdentityCache.cs) is used to store `ISystemIdentity`s for the duration of their associated tokens as [IdentityCacheObject](./IdentityCacheObject.cs)s.
- [ITokenFactory](./ITokenFactory.cs) and [implementation](./TokenFactory.cs) is used to generate the Json Web Token for a session after a user successfully authenticates.
- [ISystemIdentity](./ISystemIdentity.cs)s represent a logon session with the operating system for a given user. It contains a method to run code under the security context of said user.
- [ISystemIdentityFactory](./ISystemIdentityFactory.cs) is used to create `ISystemIdentity`s by attempting to log the user in with the OS with a given username and password.

- [OAuth](./OAuth) contains classes related to OAuth 2.0 authentication
