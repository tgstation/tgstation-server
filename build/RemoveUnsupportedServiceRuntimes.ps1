param (
	[Parameter(Position=0)] $PublishDirectory
)

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$PublishDirectory/runtimes/browser*"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$PublishDirectory/runtimes/linux*"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$PublishDirectory/runtimes/osx*"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$PublishDirectory/runtimes/unix*"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$PublishDirectory/runtimes/alpine*"
