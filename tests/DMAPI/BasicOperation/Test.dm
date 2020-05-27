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
	world.TgsTargetedChatBroadcast("Sample admin-only message", TRUE)

	world.log << "Validating API sleep"
	// Validate TGS_DMAPI_VERSION against DMAPI version used
	var/datum/tgs_version/active_version = world.TgsApiVersion()
	var/datum/tgs_version/dmapi_version = new /datum/tgs_version(TGS_DMAPI_VERSION)
	if(!active_version.Equals(dmapi_version))
		text2file("DMAPI version [TGS_DMAPI_VERSION] does not match active API version [active_version.raw_parameter]", "test_fail_reason.txt")

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
