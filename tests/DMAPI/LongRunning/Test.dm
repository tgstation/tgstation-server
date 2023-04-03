/world
	sleep_offline = FALSE
	loop_checks = FALSE

/world/Error(exception)
	fdel("test_success.txt")
	text2file("Runtime Error: [exception]", "test_fail_reason.txt")

/world/New()
	text2file("SUCCESS", "test_success.txt")
	log << "Initial value of sleep_offline: [sleep_offline]"
	sleep_offline = FALSE

	// Intentionally slow down startup for testing purposes
	for(var/i in 1 to 10000000)
		dab()
	TgsNew(new /datum/tgs_event_handler/impl, TGS_SECURITY_ULTRASAFE)
	StartAsync()

/proc/dab()
	return

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	sleep(60)
	world.TgsChatBroadcast("World Initialized")
	var/datum/tgs_message_content/response = new("Embed support test1")
	response.embed = new()
	response.embed.description = "desc"
	response.embed.title = "title"
	response.embed.colour = "#00FF00"
	response.embed.author = new /datum/tgs_chat_embed/provider/author("Dominion")
	response.embed.author.url = "https://github.com/Cyberboss"
	response.embed.timestamp = time2text(world.timeofday, "YYYY-MM-DD hh:mm:ss")
	response.embed.url = "https://github.com/tgstation/tgstation-server"
	response.embed.fields = list()
	var/datum/tgs_chat_embed/field/field = new("field1","value1")
	field.is_inline = TRUE
	response.embed.fields += field
	field = new("field2","value2")
	field.is_inline = TRUE
	response.embed.fields += field
	field = new("field3","value3")
	response.embed.fields += field
	response.embed.footer = new /datum/tgs_chat_embed/footer("Footer text")
	world.TgsChatBroadcast(response)
	world.TgsInitializationComplete()

/world/Topic(T, Addr, Master, Keys)
	log << "Topic: [T]"
	. =  HandleTopic(T)
	log << "Response: [.]"

/world/proc/HandleTopic(T)
	TGS_TOPIC

	var/list/data = params2list(T)
	var/special_tactics = data["tgs_integration_test_special_tactics"]
	if(special_tactics)
		RebootAsync()
		return "ack"

	TgsChatBroadcast("Recieved non-tgs topic: [T]")

	return "feck"

/world/Reboot(reason)
	TgsChatBroadcast("World Rebooting")
	TgsReboot()

/datum/tgs_event_handler/impl/HandleEvent(event_code, ...)
	set waitfor = FALSE

	world.TgsChatBroadcast("Recieved event: [json_encode(args)]")

/world/Export(url)
	log << "Export: [url]"
	return ..()

/proc/RebootAsync()
	set waitfor = FALSE
	world.TgsChatBroadcast("Rebooting after 3 seconds");
	world.log << "About to sleep. sleep_offline: [world.sleep_offline]"
	sleep(30)
	world.log << "Done sleep, calling Reboot"
	world.Reboot()
	
/datum/tgs_chat_command/embeds_test
	name = "embeds_test"
	help_text = "dumps an embed"

/datum/tgs_chat_command/embeds_test/Run(datum/tgs_chat_user/sender, params)
	var/datum/tgs_message_content/response = new("Embed support test2")
	response.embed = new()
	response.embed.description = "desc"
	response.embed.title = "title"
	response.embed.colour = "#0000FF"
	response.embed.author = new /datum/tgs_chat_embed/provider/author("Dominion")
	response.embed.author.url = "https://github.com/Cyberboss"
	response.embed.timestamp = time2text(world.timeofday, "YYYY-MM-DD hh:mm:ss")
	response.embed.url = "https://github.com/tgstation/tgstation-server"
	response.embed.fields = list()
	var/datum/tgs_chat_embed/field/field = new("field1","value1")
	response.embed.fields += field
	field = new("field2","value2")
	field.is_inline = TRUE
	response.embed.fields += field
	field = new("field3","value3")
	field.is_inline = TRUE
	response.embed.fields += field
	response.embed.footer = new /datum/tgs_chat_embed/footer("Footer text")
	return response
