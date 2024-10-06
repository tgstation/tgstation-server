using YamlDotNet.Serialization;

namespace Tgstation.Server.ReleaseNotes
{
	enum Component
	{
		Configuration,
		Core,
		HostWatchdog,
		WebControlPanel,
		HttpApi, // can't be properly renamed due to changelog
		GraphQLApi,
		DreamMakerApi,
		InteropApi,
		NugetCommon,
		NugetApi,
		NugetClient,
	}
}
