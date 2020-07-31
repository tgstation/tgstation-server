/world
	sleep_offline = FALSE
	loop_checks = FALSE

/world/New()
	log << "Initial value of sleep_offline: [sleep_offline]"
	sleep_offline = FALSE

	// Intentionally slow down startup for testing purposes
	for(var/i in 1 to 10000000)
		dab()
	TgsNew(new /datum/tgs_event_handler/impl, TGS_SECURITY_ULTRASAFE)
	StartAsync()

/proc/dab()
	return

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	sleep(60)
	world.TgsChatBroadcast("World Initialized")
	// world.TgsInitializationComplete()

/world/Topic(T, Addr, Master, Keys)
	log << "Topic: [T]"
	. =  HandleTopic(T)
	log << "Response: [.]"

/world/proc/HandleTopic(T)
	TGS_TOPIC

	var/list/data = params2list(T)
	var/special_tactics = data["tgs_integration_test_special_tactics"]
	if(special_tactics)
		RebootAsync()
		return "ack"

	TgsChatBroadcast("Recieved non-tgs topic: [T]")

	return "feck"

/world/Reboot(reason)
	TgsChatBroadcast("World Rebooting")
	TgsReboot()

/datum/tgs_event_handler/impl/HandleEvent(event_code, ...)
	set waitfor = FALSE

	world.TgsChatBroadcast("Recieved event: [json_encode(args)]")

/world/Export(url)
	log << "Export: [url]"
	return ..()

/proc/RebootAsync()
	set waitfor = FALSE
	world.TgsChatBroadcast("Rebooting after 3 seconds");
	world.log << "About to sleep. sleep_offline: [world.sleep_offline]"
	sleep(30)
	world.log << "Done sleep, calling Reboot"
	world.Reboot()
