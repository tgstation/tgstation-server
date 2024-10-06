# Guided Code Tour

This is a series of README.md files aimed at directing people around the codebase based on what they want to know/change. Best viewed on the GitHub website.

- To explore the server code navigate to [Tgstation.Server.Host](./Tgstation.Server.Host).
- To explore the DreamMaker public API navigate to [DMAPI/tgs.dm](./DMAPI/tgs.dm).
- To explore the DMAPI internals, navigate to [DMAPI/tgs](./DMAPI/tgs).
- To explore the REST API definitions navigate to [Tgstation.Server.Api](./Tgstation.Server.Api).
- To explore the C# client code navigate to [Tgstation.Server.Client](./Tgstation.Server.Client).

[Tgstation.Server.Host.Watchdog](./Tgstation.Server.Host.Watchdog), [Tgstation.Server.Host.Service](./Tgstation.Server.Host.Service), and [Tgstation.Server.Host.Console](./Tgstation.Server.Host.Console) are related to the Service/Console runners which have the simple task of executing Tgstation.Server.Host and updating it when requested.

[Tgstation.Server.Common](./Tgstation.Server.Common) are functions and dependencies shared publically (Published to Nuget).

[Tgstation.Server.Shared](./Tgstation.Server.Shared) are functions and dependencies shared internally (Not published to Nuget).
