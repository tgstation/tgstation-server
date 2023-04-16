/world
	sleep_offline = FALSE
	loop_checks = FALSE

/world/Error(exception/E, datum/e_src)
	var/list/usrinfo = null
	var/list/splitlines = splittext(E.desc, "\n")
	var/list/desclines = list()
	for(var/line in splitlines)
		if(length(line) < 3 || findtext(line, "source file:") || findtext(line, "usr.loc:"))
			continue
		if(findtext(line, "usr:"))
			if(usrinfo)
				desclines.Add(usrinfo)
				usrinfo = null
			continue // Our usr info is better, replace it

		if(copytext(line, 1, 3) != "  ")//3 == length("  ") + 1
			desclines += ("  " + line) // Pad any unpadded lines, so they look pretty
		else
			desclines += line

	if(usrinfo) //If this info isn't null, it hasn't been added yet
		desclines.Add(usrinfo)

	fdel("test_success.txt")
	text2file("Runtime Error: [E]", "test_fail_reason.txt")

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

	startup_complete = TRUE
	if(run_bridge_test)
		CheckBridgeLimits()

/world/Topic(T, Addr, Master, Keys)
	log << "Topic: [T]"
	. =  HandleTopic(T)
	log << "Response: [.]"

var/startup_complete
var/run_bridge_test

/world/proc/HandleTopic(T)
	TGS_TOPIC

	var/list/data = params2list(T)
	var/special_tactics = data["tgs_integration_test_special_tactics"]
	if(special_tactics)
		RebootAsync()
		return "ack"

	var/tactics2 = data["tgs_integration_test_tactics2"]
	if(tactics2)
		if(startup_complete)
			CheckBridgeLimits()
		else
			run_bridge_test = TRUE
		return "ack2"

	// Topic limit tests
	// Receive
	var/tactics3 = data["tgs_integration_test_tactics3"]
	if(tactics3)
		var/list/json = json_decode(tactics3)
		if(!json || !istext(json["payload"]) || !istext(json["size"]))
			return "fail"

		var/size = text2num(json["size"])
		var/payload = json["payload"]
		if(length(payload) != size)
			return "fail"

		return "pass"

	// Send
	var/tactics4 = data["tgs_integration_test_tactics4"]
	if(tactics4)
		var/size = isnum(tactics4) ? tactics4 : text2num(tactics4)
		if(!isnum(size))
			text2file("tgs_integration_test_tactics4 wasn't a number!", "test_fail_reason.txt")
			del(world)

		var/payload = create_payload(size)
		return payload

	TgsChatBroadcast(new /datum/tgs_message_content("Recieved non-tgs topic: `[T]`"))

	return "feck"

/world/Reboot(reason)
	TgsChatBroadcast("World Rebooting")
	TgsReboot()

/datum/tgs_event_handler/impl/HandleEvent(event_code, ...)
	set waitfor = FALSE

	world.TgsChatBroadcast(new /datum/tgs_message_content("Recieved event: `[json_encode(args)]`"))

/world/Export(url)
	if(length(url) < 1000)
		log << "Export: [url]"
	return ..()

/proc/RebootAsync()
	set waitfor = FALSE
	world.TgsChatBroadcast(new /datum/tgs_message_content("Rebooting after 3 seconds"));
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

/datum/tgs_chat_command/response_overload_test
	name = "response_overload_test"
	help_text = "returns a massive string that probably won't display in a chat client but is used to test topic response chunking"

/datum/tgs_chat_command/response_overload_test/Run(datum/tgs_chat_user/sender, params)
	// DMAPI5_TOPIC_RESPONSE_LIMIT
	var/limit = 65528
	// this actually gets doubled because it's in two fields for backwards compatibility, but that's fine
	var/datum/tgs_message_content/response = new(create_payload(limit * 3))
	return response

var/lastTgsError

/proc/TgsError(message)
	world.log << "Err: [message]"
	lastTgsError = message

/proc/create_payload(size)
	var/builder = list()
	for(var/j = 0; j < size; ++j)
		builder += "a"
	var/payload = jointext(builder, "")
	return payload

/proc/CheckBridgeLimits()
	set waitfor = FALSE
	CheckBridgeLimitsImpl()


/proc/BridgeWithoutChunking(command, list/data)
	var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
	var/bridge_request = api.CreateBridgeRequest(command, data)
	return api.PerformBridgeRequest(bridge_request)

/proc/CheckBridgeLimitsImpl()
	sleep(30)

	// Evil custom bridge command hacking here
	var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
	var/old_ai = api.access_identifier
	api.access_identifier = "tgs_integration_test"

	// Always send chat messages because they can have extremely large payloads with the text

	// bisecting request test
	var/base = 1
	var/nextPow = 0
	var/lastI = 0
	var/i
	lastTgsError = null
	for(i = 1; ; i = base + (2 ** nextPow))
		var/payload = create_payload(i)

		var/list/result = BridgeWithoutChunking(0, list("chatMessage" = list("text" = "payload:[payload]")))

		if(!result || lastTgsError || result["integrationHack"] != "ok")
			if(i == lastI + 1)
				break
			lastTgsError = null
			i = lastI
			base = lastI
			nextPow = 0
			continue

		lastI = i
		++nextPow

	// DMAPI5_BRIDGE_REQUEST_LIMIT
	var/limit = 8198

	var/finalResult = api.Bridge(0, list("chatMessage" = list("text" = "done:[create_payload(limit * 3)]")))
	if(!finalResult || lastTgsError || finalResult["integrationHack"] != "ok")
		text2file("Failed to end bridge limit test!", "test_fail_reason.txt")
		del(world)

	api.access_identifier = old_ai
