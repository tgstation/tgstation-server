# System Services

- [IAssemblyInformationProvider](./IAssemblyInformationProvider.cs) and [implementation](./AssemblyInformationProvider.cs) is used to provide server build metadata to code.
- [INetworkPromptReaper](./INetworkPromptReaper.cs) exists mainly to prevent DreamDaemon dialogs from popping up when using `/world/proc/OpenPort()` in DM code.
  - Currently it's only implemented as the [WindowsNetworkPromptReaper](./WindowsNetworkPromptReaper.cs)
- [IPlatformIdentifier](./IPlatformIdentifier.cs) and [implementation](./PlatformIdentifier.cs) provides detection of the current operating system.
- The process-related interfaces and classes are used to provide a nice level of abstration around process execution.
- [ProgramShutdownTokenSource](./ProgramShutdownTokenSource.cs) generates a `CancellationToken` that triggers when a quit signal is sent to the process.
