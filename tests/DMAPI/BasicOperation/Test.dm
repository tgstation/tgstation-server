/world/proc/RunTest()
	text2file("SUCCESS", "test_success.txt")
	log << "About to call TgsNew()"
	sleep_offline = FALSE
	TgsNew(minimum_required_security_level = TGS_SECURITY_SAFE)
	log << "About to call StartAsync()"
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	world.log << "sleep"
	sleep(50)
	world.TgsTargetedChatBroadcast("Sample admin-only message", TRUE)

	var/list/world_params = world.params
	if(!("test" in world_params) || world_params["test"] != "bababooey")
		FailTest("Expected parameter test=bababooey but did not receive", "test_fail_reason.txt")

	fdel("test_event_output.txt")
	var/test_data = "nwfiuurhfu"
	world.TgsTriggerEvent("test_event", list(test_data), TRUE)
	if(!fexists("test_event_output.txt"))
		FailTest("Expected test_event_output.txt to exist here", "test_fail_reason.txt")

	var/test_contents = copytext(file2text("test_event_output.txt"), 1, length(test_data) + 1)
	if(test_contents != test_data)
		FailTest("Expected test_event_output.txt to contain [test_data] here. Got [test_contents]", "test_fail_reason.txt")

	fdel("test_event_output.txt")
	world.TgsTriggerEvent("test_event", list("asdf"), FALSE)
	if(fexists("test_event_output.txt"))
		FailTest("Expected test_event_output.txt to not exist here", "test_fail_reason.txt")

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
