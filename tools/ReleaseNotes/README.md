This is a small tool to generate TGS release notes from PR descriptions

Requires environment variable `TGS_RELEASE_NOTES_TOKEN`

Run it with `dotnet run <version> [--no-close (optionally doesn't close the milestone, USE WHILE DEBUGGING)]`

Will close the release milestone and output `release_notes.md` with the updated release notes
