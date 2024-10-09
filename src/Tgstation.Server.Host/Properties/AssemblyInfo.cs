using System.Runtime.CompilerServices;

using GreenDonut;

[assembly: InternalsVisibleTo("Tgstation.Server.Host.Tests")]
[assembly: InternalsVisibleTo("Tgstation.Server.Host.Tests.Signals")]
[assembly: InternalsVisibleTo("Tgstation.Server.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

[assembly: DataLoaderDefaults(AccessModifier = DataLoaderAccessModifier.Internal)]
