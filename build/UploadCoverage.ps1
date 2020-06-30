codecov -f api_coverage.xml --flag unittests
codecov -f client_coverage.xml --flag unittests
codecov -f host_coverage.xml  --flag unittests
codecov -f console_coverage.xml --flag unittests
codecov -f watchdog_coverage.xml --flag unittests
codecov -f service.coveragexml --flag unittests
codecov -f server_coverage.xml --flag integration
