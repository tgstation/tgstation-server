# tgstation-server Client Library

This library is used for accessing [tgstation-server](https://github.com/tgstation/tgstation-server) instances via .NET code.

## Examples

### Connecting to a Server:

```cs
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Client;

...

async ValueTask<IServerClient> CreateClientWithDefaultCredentials(CancellationToken cancellationToken)
{


	return await clientFactory.CreateFromLogin(
		url,
		DefaultCredentials.AdminUserName,
		DefaultCredentials.DefaultAdminUserPassword,
		cancellationToken: cancellationToken);
}
```
