# API Controllers

Most of these controllers map to their specific routes. See the public functions marked with `[HttpXXX]` attributes for where the web meets code.

Some notable exceptions:

- [ApiController](./ApiController.cs) is the base class of nearly all API related controllers. It does the following:
  - Contains code to deny the request if the instance is not present when it should be.
  - Contains the `IDatabaseContext` and `ILogger` properties for child controllers.
  - Returns 400 Bad Request if the headers or the PUT/POST'd model is invalid.
  - Returns 401 If an `IAuthenticationContext` could not be created for a request.
- [BridgeController](./BridgeController.cs) is a special controller accessible only from localhost and is used to receive bridge request from DreamDaemon
- [HomeController](./HomeController.cs) contains the code to initially log in and generate an API token for a user.
