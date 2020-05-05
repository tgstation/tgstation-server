/world/New()
	TgsNew(new /datum/tgs_event_handler/impl, TGS_SECURITY_ULTRASAFE)
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
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

	world.TgsChatBroadcast("Recieved event: [json_encode(args)]")

	if(event_code != TGS_EVENT_REBOOT_MODE_CHANGE)
		world.TgsChatBroadcast("Not rebooting, wrong event");
		return

	if(args[3] != TGS_REBOOT_MODE_NORMAL)
		world.TgsChatBroadcast("Not rebooting, wrong reboot mode");
		return

	RebootAsync()


/proc/RebootAsync()
	set waitfor = FALSE
	world.TgsChatBroadcast("Rebooting after 3 seconds");
	sleep(30)
	world.Reboot()
