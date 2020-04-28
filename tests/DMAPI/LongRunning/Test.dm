/world/New()
	log << "About to call TgsNew()"
	TgsNew(minimum_required_security_level = TGS_SECURITY_ULTRASAFE)
	log << "About to call StartAsync()"
	StartAsync()

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	world.log << "sleep"
	sleep(50)
	world.TgsChatBroadcast("World Initialized")
	world.TgsInitializationComplete()

/world/Topic(T, Addr, Master, Keys)
	TGS_TOPIC

/world/Reboot(reason)
	TgsChatBroadcast("World Rebooting")
	TgsReboot()

/datum/tgs_chat_command/reboot
	name = "reboot"
	help_text = "echos input parameters and reboots server"

/datum/tgs_chat_command/reboot/Run(datum/tgs_chat_user/sender, params)
	set waitfor = FALSE
	. = "Echo: [sender.channel.connection_name]|[sender.channel.friendly_name]|[sender.friendly_name]: [params]. Rebooting..."
	RebootAsync()

/proc/RebootAsync()
	set waitfor = FALSE
	sleep(30)
	world.Reboot()
