/world/New()
	log << "About to call TgsNew()"
	TgsNew(new /datum/tgs_event_handler/impl, TGS_SECURITY_ULTRASAFE)
	log << "About to call StartAsync()"
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	world.log << "sleep"
	sleep(60)
	world.TgsChatBroadcast("World Initialized")
	world.TgsInitializationComplete()

/world/Topic(T, Addr, Master, Keys)
	TGS_TOPIC

/world/Reboot(reason)
	TgsChatBroadcast("World Rebooting")
	TgsReboot()

/datum/tgs_event_handler/impl/HandleEvent(event_code, ...)
	set waitfor = FALSE

	TgsChatBroadcast("Recieved event: [json_encode(args)]")

	if(event_code != TGS_EVENT_REBOOT_MODE_CHANGE)
		return

	if(args[3] != TGS_REBOOT_MODE_NORMAL)
		return

	RebootAsync()


/proc/RebootAsync()
	set waitfor = FALSE
	sleep(30)
	world.Reboot()
