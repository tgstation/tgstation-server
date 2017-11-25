using System.Reflection;
using System.Runtime.CompilerServices;

//You cannot one definition the version number
//Believe me, I've tried, the compiler hates it so much
[assembly: AssemblyVersion("3.2.0.15")]
[assembly: AssemblyFileVersion("3.2.0.15")]
[assembly: AssemblyInformationalVersion("3.2.0.15")]

[assembly: InternalsVisibleTo("TGS.Tests")]
// For Moqing, see https://stackoverflow.com/questions/17569746/mocking-internal-classes-with-moq-for-unit-testing
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
