using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TGStation Server Service")]
[assembly: AssemblyDescription("Server Service for running BYOND games")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f32eda25-0855-411c-af5e-f0d042917e2d")]

//allow the unit tester to peek inside us
[assembly: InternalsVisibleTo("TGServiceTests", AllInternalsVisible = true)]
