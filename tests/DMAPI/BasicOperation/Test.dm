/world/New()
	TgsNew(minimum_required_security_level = TGS_SECURITY_SAFE)
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	sleep(50)
	world.TgsInitializationComplete()
	sleep(50)

	// Validate TGS_DMAPI_VERSION against DMAPI version used
	var/datum/tgs_version/active_version = world.TgsApiVersion()
	var/datum/tgs_version/dmapi_version = new /datum/tgs_version(TGS_DMAPI_VERSION)
	if(!active_version.Equals(dmapi_version))
		text2file("DMAPI version [TGS_DMAPI_VERSION] does not match active API version [active_version.raw_parameter]", "test_fail_reason.txt")

	world.TgsEndProcess()

/world/Topic(T, Addr, Master, Keys)
	TGS_TOPIC

/world/Reboot(reason)
	TgsReboot()

/proc/message_admins(event)
	event = "Admins: [event]"
	world << event
	world.log << event

var/list/clients = list()

/client/New()
	clients += src
	return ..()

/client/Del()
	clients -= src
	return ..()

/datum/tgs_chat_command/echo
	name = "echo"
	help_text = "echos input parameters"

/datum/tgs_chat_command/echo/Run(datum/tgs_chat_user/sender, params)
	return "[sender.channel.connection_name]|[sender.channel.friendly_name]|[sender.friendly_name]: [params]"
