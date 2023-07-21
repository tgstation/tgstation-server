var/auxcov

/proc/start_code_coverage(filename)
	CRASH("auxcov not loaded")

/proc/stop_code_coverage(filename)
	CRASH("auxcov not loaded")

/proc/auxtools_stack_trace(msg)
	CRASH(msg)

/proc/auxtools_expr_stub()
	CRASH("auxtools not loaded")

/proc/enable_debugging(mode, port)
	CRASH("auxtools not loaded")

#define AUXCOV_MIN_BUILD 1607 // https://www.byond.com/forum/post/108025

/world/New()
#if defined(TGS_DMAPI_VERSION) && defined(DM_BUILD)
#if DM_BUILD >= AUXCOV_MIN_BUILD
	auxcov = "../[world.system_type == MS_WINDOWS ? "auxcov.dll" : "libauxcov.so"]"
	if (fexists(auxcov))
		log << "Loading auxcov..."
		var/result = call_ext(auxcov, "auxtools_init")()
		if(!findtext(result, "SUCCESS"))
			log << "Loading auxcov failed: [result]"
			del(src)
	else
		log << "auxcov not found"
		auxcov = null
#endif
#endif

	log << "Starting test..."

	if (auxcov)
		var/coverage_filename
		for(var/i = 0; i == 0 || fexists(coverage_filename); ++i)
			coverage_filename = "coverage/test_coverage_[i].xml"

		text2file("touch", coverage_filename) // ghetto mkdir
		fdel(coverage_filename)

		start_code_coverage(coverage_filename)

	world.RunTest()
	return ..()

/world/Del()
#ifdef DM_BUILD
#if DM_BUILD >= AUXCOV_MIN_BUILD
	if(auxcov)
		call_ext(auxcov, "auxtools_full_shutdown")()
#endif
#endif

	return ..()

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
	text2file(reason, "test_fail_reason.txt")
	world.log << "Terminating..."
	del(world)
