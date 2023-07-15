This is a small tool to automate generating TGS releases

Requires environment variable `TGS_RELEASE_NOTES_TOKEN`

Run it with `dotnet run <version> [--no-close (optionally doesn't close the milestone, USE WHILE DEBUGGING)]`

Will close the release milestone and output `release_notes.md` with the updated release notes

Alternative modes

`dotnet run --ensure-release`

Ensures the latest GitHub release is a TGS release

`dotnet run --link-winget <action run url>`

Updates an existing https://github.com/microsoft/winget-pkgs manifest update pull request with the TGS template. The PR updated is the last one opened by the user

`dotnet run --winget-template-check <Latest edit SHA of https://github.com/microsoft/winget-pkgs/blob/master/.github/PULL_REQUEST_TEMPLATE.md>`

Validates the template we are PRing is up-to-date.
