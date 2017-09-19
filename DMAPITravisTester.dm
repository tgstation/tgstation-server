#define SERVER_TOOLS_EXTERNAL_CONFIGURATION
#define SERVER_TOOLS_DEFINE_AND_SET_GLOBAL(Name, Value) var/##Name = ##Value
#define SERVER_TOOLS_READ_GLOBAL(Name) global.##Name
#define SERVER_TOOLS_WRITE_GLOBAL(Name, Value) global.##Name = ##Value
#define SERVER_TOOLS_WORLD_ANNOUNCE(message) world << ##message
#define SERVER_TOOLS_LOG(message) world.log << ##message
#define SERVER_TOOLS_NOTIFY_ADMINS(event) message_admins(event)

/world/New()
	SERVER_TOOLS_ON_NEW
	world.log << "Service API Version: [SERVER_TOOLS_API_VERSION]"
	WaitForAPICompatResponse()

/world/Topic(T, Addr, Master, Keys)
	SERVER_TOOLS_ON_TOPIC

/world/Reboot(reason)
	SERVER_TOOLS_ON_REBOOT
	SERVER_TOOLS_REBOOT_BYOND

/proc/message_admins(event)
	world << event
	world.log << event

/proc/WaitForAPICompatResponse()
	set waitfor = FALSE
	sleep(50)
	if(!SERVER_TOOLS_PRESENT)
		world.log << "No running service detected"
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
