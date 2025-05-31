# IO Related Classes

- [IConsole](./IConsole.cs) and [implementation](./Console.cs) provide methods for writing to the console.
- [IIOManager](./IIOManager.cs) is the primary method of performing file I/O across the server. It comes in two flavors.
  - [DefaultIOManager](./DefaultIOManager.cs) implements the interface as expected.
  - [ResolvingIOManager](./ResolvingIOManager.cs) implements the interface with a custom [working directory](https://en.wikipedia.org/wiki/Working_directory).
- [IPostWriteHandler](./IPostWriteHandler.cs) dictates a set of actions to take after writing a file.
  - The [WindowsPostWriteHandler](./WindowsPostWriteHandler.cs) is a no-op.
  - The [PosixPostWriteHandler](./PosixPostWriteHandler.cs) sets +x on files.
- [ISymlinkFactory](./ISymlinkFactory.cs) is used to create [symbolic links](https://en.wikipedia.org/wiki/Symbolic_link) across the server.
- [ISynchronousIOManager](./ISynchronousIOManager.cs) and [implementation](./SynchronousIOManager.cs) is a subset of `IIOManager` functions that are performed in a blocking manner.
  - The only current use of this is to perform `ISystemIdentity` impersonated operations while keeping the OS security context of the executing thread. This means that `async` and `Task`s cannot be used as they make the active thread non-deterministic.
