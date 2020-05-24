# Jobs Subsystem

- [IJobManager](./IJobManager.cs) and [implementation](./JobManager.cs) is where the bulk of the magic happens. The `RegisterOperation()` call is what takes a work unit and sets it to run asynchronously while being tracked through the API.
- [JobException] is a special .NET Exception implementation that is able to carry API `ErrorCode`s and other additional data.
- [JobHandler](./JobHandler.cs) carries the [CancellationTokenSource](https://stackoverflow.com/questions/20638952/cancellationtoken-and-cancellationtokensource-how-to-use-it) for a given job in a disposable context.
