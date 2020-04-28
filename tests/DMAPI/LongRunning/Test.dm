/world/New()
	log << "About to call TgsNew()"
	TgsNew(minimum_required_security_level = TGS_SECURITY_SAFE)
	log << "About to call StartAsync()"
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	world.log << "sleep"
	sleep(50)
	world.TgsInitializationsComplete()

/world/Topic(T, Addr, Master, Keys)
	TGS_TOPIC

/world/Reboot(reason)
	TgsReboot()
