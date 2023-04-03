/world/New()
	text2file("SUCCESS", "test_success.txt")
	log << "Hello world!"

/world/Error(exception)
	fdel("test_success.txt")
	text2file("Runtime Error: [exception]", "test_fail_reason.txt")
