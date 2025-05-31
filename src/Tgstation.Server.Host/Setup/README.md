# Setup Code

This is the code used to facilitate running the [SetupWizard](./SetupWizard.cs).

- [SetupApplication](./SetupApplication.cs) is used to configure services used by the setup wizard. It is the parent class of `Application`.
- [IPostSetupServices](./IPostSetupServices.cs) and [implementation](./PostSetupServices.cs) is used to provide certain services `Application` depends on at configuration time after the setup wizard has (or has not) run.
