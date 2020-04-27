/world/New()
	TgsNew()
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	sleep(100)
	TgsInitializationComplete()

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
