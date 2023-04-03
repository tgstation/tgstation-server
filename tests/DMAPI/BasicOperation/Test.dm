/world/New()
	text2file("SUCCESS", "test_success.txt")
	log << "About to call TgsNew()"
	sleep_offline = FALSE
	TgsNew(minimum_required_security_level = TGS_SECURITY_SAFE)
	log << "About to call StartAsync()"
	StartAsync()

/world/Error(exception)
	fdel("test_success.txt")
	text2file("Runtime Error: [exception]", "test_fail_reason.txt")

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	world.log << "sleep"
	sleep(50)
	world.TgsTargetedChatBroadcast("Sample admin-only message", TRUE)

	var/list/world_params = params2list(world.params)
	if(!("test" in world_params) || world_params["test"] != "bababooey")
		text2file("Expected parameter test=bababooey but did not receive", "test_fail_reason.txt")

	world.log << "sleep2"
	sleep(150)
	world.log << "Terminating..."
	world.TgsEndProcess()

	world.log << "You really shouldn't be able to read this"

/world/Export(url)
	log << "Export: [url]"
	return ..()

/world/Topic(T, Addr, Master, Keys)
	world.log << "Topic: [T]"
	. =  HandleTopic(T)
	world.log << "Response: [.]"

/world/proc/HandleTopic(T)
	TGS_TOPIC

/world/Reboot(reason)
	TgsReboot()

/datum/tgs_chat_command/echo
	name = "echo"
	help_text = "echos input parameters"

/datum/tgs_chat_command/echo/Run(datum/tgs_chat_user/sender, params)
	return "[sender.channel.connection_name]|[sender.channel.friendly_name]|[sender.friendly_name]: [params]"
