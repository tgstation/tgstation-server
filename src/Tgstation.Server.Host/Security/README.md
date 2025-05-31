# Security Classes

- [IAuthenticationContext](./IAuthenticationContext.cs) and [implementation](./AuthenticationContext.cs) is what contains information about an authenticated user for a request. Includes things like the relevant `InstanceUser` and any associated rights.
- [IAuthenticationContextFactory](./IAuthenticationContextFactory.cs) and [implementation](AuthenticationContextFactory.cs) is a factory for `IAuthenticationContext`s. It handles things related to the database for a user's authentication. This includes loading their rights/associated instance user. It will also stop the request if the users token was issued before the last time their password or `Enabled` status was updated.
- [AuthenticationContextClaimsTransformation](./AuthenticationContextClaimsTransformation.cs) is used to associate rights with a request context so that it may properly pass appropriate `TgsAuthorizeAttribute`s.
- [ICrytopgraphySuite](./ICrytopgraphySuite.cs) and [implementation](./CrytopgraphySuite.cs) is used to generate secure strings and byte arrays. It also contains the password hashing and validation logic.
- [IIdentityCache](./IIdentityCache.cs) and [implementation](./IdentityCache.cs) is used to store `ISystemIdentity`s for the duration of their associated tokens as [IdentityCacheObject](./IdentityCacheObject.cs)s.
- [ITokenFactory](./ITokenFactory.cs) and [implementation](./TokenFactory.cs) is used to generate the Json Web Token for a session after a user successfully authenticates.
- [ISystemIdentity](./ISystemIdentity.cs)s represent a logon session with the operating system for a given user. It contains a method to run code under the security context of said user.
- [ISystemIdentityFactory](./ISystemIdentityFactory.cs) is used to create `ISystemIdentity`s by attempting to log the user in with the OS with a given username and password.
- [TgsAuthorizeAttribute](./TgsAuthorizeAttribute.cs) is a special attribute applied to controller methods to define which rights are required to run a verb.

- [OAuth](./OAuth) contains classes related to OAuth 2.0 authentication

# A Basic Rundown of the Authenticaton Pipeline

## For the login request (`POST /`)

1. An attempt to parse the `ApiHeaders` is made. If they were valid, the API version check is performed. If it fails, HTTP 400 with an `ErrorMessageResponse` will be returned.
1. If, for some reason, the user attempts to use a JWT to authenticate this request, steps 2-4 of the non-login pipeline list below are performed.
1. The `ApiController` base class inspects the request.
   - At this point, if the `ApiHeaders` (MINUS the `Authorization` header) cannot be properly parsed, HTTP 400 with an `ErrorMessageResponse` is returned.
1. The `HomeController` inspects the request.
   1. If the `ApiHeaders` could not be properly parsed, HTTP 400 (or 406 if the `Accept` header was bad) with an `ErrorMessageResponse` is returned.
      - The `WWW-Authenticate` header will be set in this response.
   1. If authentication succeeded using a JWT `Bearer` token, HTTP 400 with an `ErrorMessageResponse` is returned. Refreshing a login using a token is not permitted.
   1. At this point, the path diverges based on the credential type.
      - If the user is using a username/password combo:
        1. The username and password combination is tried against the OS authentication system (currently a no-op on Linux).
           - If it succeeds, the session is held on to for future reference and the database is queried for a user matching the SID/UID of the login session.
           - Otherwise, the database is queried for a user matching the canonicalized username.
      - If the user is using an OAuth code:
        1. If the OAuth provider is disabled in the configuration, HTTP 400 with an `ErrorMessageResponse` is returned.
        1. The code is sent to the external provider for validation
           - If the provider is GitHub, there's a chance that this could fail due to rate limiting. In this case, HTTP 429 is returned.
           - If the provider rejects the OAuth code, HTTP 401 is returned.
        1. The database is queried for a user matching the OAuth provider and external user identifier sent with the OAuth provider's response.
   1. If the query selected above produces no results, HTTP 401 is returned.
   1. For non-OAuth logins, maintenance is performed on the user's DB entry at this point
      - For non-OS logins:
        1. The provided password is hashed and checked against the database entry. If it does not match, HTTP 401 will be returned.
           - This can potentially cause a change to the DB's stored `PasswordHash` if TGS has updated its dependencies and Microsoft has decided to deprecate the previous hashing method since the user last logged in.
             - If this occurs, it invalidates all previous logins for the user.
      - For OS logins:
        1. If the `PasswordHash` in the DB isn't null, it is set as such. This invalidates all previous logins for the user.
        1. If the `Name` in the DB does not match the user's OS login, it is updated.
   1. If the user's database entry says they are not enabled, HTTP 403 is returned.
   1. A token is generated from the [ITokenFactory](./ITokenFactory.cs).
   1. For OS logins, the user's login session is cached for the duration of the token's validity plus 15 seconds.
   1. The token is returned as a `TokenResponse` with an HTTP 200 status code.

## For all other authenticated requests

1. An attempt to parse the `ApiHeaders` is made. If they were valid. The API version check is performed. If it fails, HTTP 400 with an `ErrorMessageResponse` will be returned.
1. The JWT, if present, is validated. If it is, the scope's [AuthenticationContextFactory](./AuthenticationContextFactory.cs) has `SetTokenNbf` called. If not, HTTP 401 will be returned.
   - Inside ASP.NET Core, this initializes the calling user's identity principal and sets the "sub" claim to the TGS user ID parsed out of the JWT.
     - We know it's the user ID because we set it up like that in the [TokenFactory](./TokenFactory.cs)
1. The [AuthenticationContextClaimsTransformation](./AuthenticationContextClaimsTransformation.cs) is run (this does not short circuit to responses).
   1. This invokes `IAuthenticationContextFactory.CreateAuthenticationContext` using the "sub" claim from the user's identity and the "nbf" timestamp set earlier (We don't get this from the scope's [IApiHeadersProvider](./IApiHeadersProvider.cs) because there may be other errors preventing the `ApiHeaders` from being parsed).
      - At this point, the database lookup using the user ID occurs. This hyrates the scope's [AuthenticationContext](./AuthenticationContext.cs) (which is available at the start of the request, but uninitialized). If the user is a system user, their login session is pulled from the cache. This is also where the instance data for a request is loaded if the user has a valid `InstancePermissionSet` for that instance. The user needs to have a few prerequisites for a valid [IAuthenticationContext](./IAuthenticationContext.cs) to be generated:
        - The user with the matching ID must exist in the database.
        - The last time the user's password or `Enabled` status changed must be before the "nbf" of their token"
      - If the user logged in with an OS login, the session is retrieved from the cache here and added to the scope's [AuthenticationContext](./AuthenticationContext.cs).
   1. If a valid authentication context is returned from the [IAuthenticationContextFactory](./IAuthenticationContextFactory.cs), the [AuthenticationContextClaimsTransformation](./AuthenticationContextClaimsTransformation.cs) uses the context to add claims for each permission bit to the user's identity principal.
      - Internally, ASP.NET Core uses this to determine whether or not a request to an endpoint will 403 or not based on the parameters of its [TgsAuthorizeAttribute](./TgsAuthorizeAttribute.cs).
1. The authorization filter is invoked
   - For non-SignalR hub requests, this is the `IAuthorizationFilter` part of the [TgsAuthorizeAttribute](./TgsAuthorizeAttribute.cs). It does two simple things:
     1. It checks the validity of the scope's [IAuthenticationContext](./IAuthenticationContext.cs). If it is invalid (indicating the user is not authorized either due to not existing (Only possible with a forged and signed JWT) or if their token was outdated compared to the last time their password or `Enabled` status was updated), HTTP 401 will be returned.
     1. It checks the user's `Enabled` status. If the user is disabled, HTTP 403 will be returned.
   - For SignalR hub requests, this is the [AuthorizationContextHubFilter](./AuthorizationContextHubFilter.cs).
     - If either [IAuthenticationContext](./IAuthenticationContext.cs) is either invalid OR unauthorized, it unceremoniously aborts the connection.
1. The `ApiController` base class inspects the request.
   1. If the `ApiHeaders` could not be properly parsed, HTTP 400 (or 406 if the `Accept` header was bad) with an `ErrorMessageResponse` is returned.
   1. If the request is to an Instance component path:
      1. If there is no valid `Instance` header, HTTP 400 with an `ErrorMessageResponse` is returned.
      1. If the active [IAuthenticationContext](./IAuthenticationContext.cs) has no instance data loaded (indicating the user is not authorized to access said instance), HTTP 403 is returned.
      1. If the instance is offline, HTTP 409 with an `ErrorMessageResponse` is returned.
   1. If the request takes an API model as a parameters and the model included in the request body encountered validation errors, HTTP 400 with an `ErrorMessageResponse` is returned.
1. The request at this point, is considered authorized. Remaining behaviour is left up to each individual route to implement.
