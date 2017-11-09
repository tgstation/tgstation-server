using System.Reflection;
using System.Runtime.CompilerServices;

#if !DEBUG && !NO_STRONG_NAME
[assembly: InternalsVisibleTo("TGServiceTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f10487511f8056df7ead40f8f3bb0a7a4890d1bafdbf3d2cc0092655849223733672039671c3855e653c950b68b6a9dd04fbe0784f0c6e213c66be8afa4cc37afad52a05744c1305bf1d1c7c2702b4f64036c5b96045a7ccdc421847eec17203ad8188b5a34a5d5cd3c845a071ebf72fff1236410cce8d51616a49f6e53ba0b9")]
[assembly: AssemblyKeyName("TGStationServer3")]
#else
[assembly: InternalsVisibleTo("TGServiceTests")]
#endif

//You cannot one definition the version number
//Believe me, I've tried, the compiler hates it so much
[assembly: AssemblyVersion("3.2.0.0")]
[assembly: AssemblyFileVersion("3.2.0.0")]
[assembly: AssemblyInformationalVersion("3.2.0.0")]
