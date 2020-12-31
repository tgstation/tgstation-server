# Database Interaction Classes

The star of the show here is [IDatabaseContext](./IDatabaseContext.cs) which represents a connection to the database via the ORM across the server. The ORM used is [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).

- [IDatabaseCollection](./IDatabaseCollection.cs) and [implementation](./DatabaseCollection.cs) is a wrapper around EF's [DbSet<TModel>](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1?view=efcore-3.1) class. Used primarily for abstraction.
- [IDatabaseConnectionFactory](./IDatabaseConnectionFactory.cs) and [implementation](./DatabaseConnectionFactory.cs) is used for creating raw database connections. Mainly for testing purposes in the setup wizard.
- [IDatabaseContextFactory](./IDatabaseContextFactory.cs) and [implementation](./DatabaseContextFactory.cs) is used to create connections to the database. Used primarily within a component or job context.
- [IDatabaseSeeder](./IDatabaseSeeder.cs) and [implementation](./DatabaseSeeder.cs) is used to seed the admin user, reset the admin password if necessary, and clean up known bad data.
- The various XXXDatabaseContext classes override the abstract [DatabaseContext](./DatabaseContext.cs) class to implement it for the various DB backends.
- The [Design](./Design) directory contains classes used when generation migrations.
- The [Migrations](./Migrations) directory contains the migrations for each backend.
