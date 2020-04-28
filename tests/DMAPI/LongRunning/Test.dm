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
	world.TgsInitializationsComplete()

/world/Topic(T, Addr, Master, Keys)
	TGS_TOPIC

/world/Reboot(reason)
	TgsReboot()

/datum/tgs_chat_command/reboot
	name = "echo"
	help_text = "echos input parameters"

/datum/tgs_chat_command/reboot/Run(datum/tgs_chat_user/sender, params)
	set waitfor = FALSE
	RebootAsync()
	return "Echo: [sender.channel.connection_name]|[sender.channel.friendly_name]|[sender.friendly_name]: [params]. Rebooting..."

/proc/RebootAsync()
	sleep(30)
	world.Reboot()
