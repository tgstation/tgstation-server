/world/New()
	log << "Starting test: [json_encode(params)]"
	text2file("SUCCESS", "test_success.txt")
	world.RunTest()

/world/Error(exception/E, datum/e_src)
	var/list/usrinfo = null
	var/list/splitlines = splittext(E.desc, "\n")
	var/list/desclines = list()
	for(var/line in splitlines)
		if(length(line) < 3 || findtext(line, "source file:") || findtext(line, "usr.loc:"))
			continue
		if(findtext(line, "usr:"))
			if(usrinfo)
				desclines.Add(usrinfo)
				usrinfo = null
			continue // Our usr info is better, replace it

		if(copytext(line, 1, 3) != "  ")//3 == length("  ") + 1
			desclines += ("  " + line) // Pad any unpadded lines, so they look pretty
		else
			desclines += line

	if(usrinfo) //If this info isn't null, it hasn't been added yet
		desclines.Add(usrinfo)

	FailTest("Runtime Error: [E]")

/proc/FailTest(reason)
	world.log << "TEST ERROR DM-SIDE: [reason]"
	fdel("test_success.txt")
	text2file(reason, "test_fail_reason.txt")
	world.log << "Terminating..."
	del(world)
	sleep(world.tick_lag) // https://www.byond.com/forum/post/2894866
