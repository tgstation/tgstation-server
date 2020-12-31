# Configuration Classes

These types map directly to the settings used in the [appsettings.json](../appsettings.json) file and its derivatives. See the [Microsoft Docs on the ASP .NET Core configuration system](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1) for details.

When making changes here, it's important to also update the config version in [build/Version.props](../../../build/Version.props) according to semver semantics. You'll also need to update the constant in [GeneralConfiguration.cs](./GeneralConfigration.cs).