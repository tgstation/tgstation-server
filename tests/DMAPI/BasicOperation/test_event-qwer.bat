echo "Running test_event script"

rem mingw has their own /usr/bin/timeout
C:\Windows\system32\timeout.exe /t 5
cd %1
cd tests\DMAPI\BasicOperation
echo %2 > test_event_output.txt
