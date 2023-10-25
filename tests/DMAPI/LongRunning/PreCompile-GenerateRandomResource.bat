cd /D "%~dp0"
cd %1
cd tests\DMAPI\LongRunning
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -Command "(New-Guid).Guid | Out-File resource.txt; Add-Content -Path resource.txt -Value 'aljsdhfjahsfkjnsalkjdfhskljdackmcnvxkljhvkjsdanv,jdshlkufhklasjeFDhfjkalhdkjlfhalksfdjh'"
