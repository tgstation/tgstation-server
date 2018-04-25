/world/New()
	SERVER_TOOLS_ON_NEW
	world.log << "Service API Version: [SERVER_TOOLS_API_VERSION]"
	WaitForAPICompatResponse()

/world/Topic(T, Addr, Master, Keys)
	SERVER_TOOLS_ON_TOPIC

/world/Reboot(reason)
	SERVER_TOOLS_ON_REBOOT
	SERVER_TOOLS_REBOOT_BYOND(FALSE)

/proc/message_admins(event)
	world << event
	world.log << event

/proc/WaitForAPICompatResponse()
	set waitfor = FALSE
	sleep(50)
	if(!SERVER_TOOLS_PRESENT)
		world.log << "No running service detected"
		del(world)
		return
	world.log << "Running service version: [SERVER_TOOLS_VERSION]"
	var/list/prs = SERVER_TOOLS_PR_LIST
	for(var/I in prs)
		var/data = prs[I]
		world.log << "Testmerge: #[I]: [data["title"]] by [data["author"]] at commit [data["commit"]]"
	var/checks_complete = "Server tools API checks complete! Rebooting..."
	SERVER_TOOLS_CHAT_BROADCAST(checks_complete)
	SERVER_TOOLS_RELAY_BROADCAST(checks_complete)
	world.Reboot()

var/list/clients = list()

/client/New()
	clients += src
	return ..()

/client/Del()
	clients -= src
	return ..()
