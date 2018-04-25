/world/New()
	TgsNew()
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
