echo "Running test_event script"
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -Command "Start-Sleep -Seconds 5"
cd %1
cd tests\DMAPI\BasicOperation
echo %2 > test_event_output.txt
