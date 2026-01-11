/world
	sleep_offline = FALSE
	loop_checks = FALSE

/world/proc/RunTest()
	log << "Initial value of sleep_offline: [sleep_offline], setting to FALSE"
	sleep_offline = FALSE

	if(params["slow_start"])
		// Intentionally slow down startup for health check testing purposes
		for(var/i in 1 to 10000000)
			dab()

	TgsNew(new /datum/tgs_event_handler/impl, TGS_SECURITY_SAFE)

	var/sec = TgsSecurityLevel()
	if(isnull(sec))
		FailTest("TGS Security level was null!")

	log << "Running in security level: [sec]"

	var/vis = TgsVisibility()
	if(isnull(vis))
		FailTest("TGS Visibility was null!")

	log << "Running in visibility: [vis]"

	if(params["expect_chat_channels"])
		var/list/channels = TgsChatChannelInfo()
		if(!length(channels))
			FailTest("Expected some chat channels!")

	var/res = file('resource.txt')
	if(!res)
		FailTest("Failed to resource!")

	var/res_contents = TGS_FILE2TEXT_NATIVE(res) // we need a .rsc to be generated
	if(!res_contents)
		FailTest("Failed to resource? No contents!")

#ifndef OPENDREAM
	if(!fexists("[DME_NAME].rsc"))
		FailTest("Failed to create .rsc!")
#endif

#ifdef RUN_STATIC_FILE_TESTS
	if(params["expect_static_files"])
		if(!fexists("test2.txt"))
			FailTest("Missing test2.txt")

		var/f2content = file2text("test2.txt")
		if(f2content != "bbb")
			FailTest("Unexpected test2.txt content: [f2content]")

		if(!fexists("data/test.txt"))
			FailTest("Missing data/test.txt")

		var/f1content = file2text("data/test.txt")
		if(f1content != "aaa")
			FailTest("Unexpected data/test.txt content: [f1content]")
#endif

	StartAsync()

/proc/dab()
	return

/proc/StartAsync()
	set waitfor = FALSE
	Run()

/proc/Run()
	sleep(60)
	world.TgsChatBroadcast(new /datum/tgs_message_content("World Initialized"))
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
		CheckBridgeLimits(run_bridge_test)

/world/Topic(T, Addr, Master, Keys)
	if(findtext(T, "tgs_integration_test_tactics3") == 0)
		log << "Topic (sleep_offline: [sleep_offline]): [T]"
	else
		log << "tgs_integration_test_tactics3 <TOPIC SUPPRESSED>"
	. =  HandleTopic(T)
	log << "Response (sleep_offline: [sleep_offline]): [.]"

var/startup_complete
var/run_bridge_test

/world/proc/HandleTopic(T)
	TGS_TOPIC

	log << "Custom topic: [T]"
	var/list/data = params2list(T)
	var/special_tactics = data["tgs_integration_test_special_tactics"]
	if(special_tactics)
		RebootAsync()
		return "ack"

	var/tactics2 = data["tgs_integration_test_tactics2"]
	if(tactics2)
		if(startup_complete)
			CheckBridgeLimits(tactics2)
		else
			run_bridge_test = tactics2
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
			FailTest("tgs_integration_test_tactics4 wasn't a number!")

		var/payload = create_payload(size)
		return payload

	// Chat overload
	var/tactics5 = data["tgs_integration_test_tactics5"]
	if(tactics5)
		TgsChatBroadcast(new /datum/tgs_message_content(create_payload(3000)))
		return "sent"

	// Bridge response queuing
	var/tactics6 = data["tgs_integration_test_tactics6"]
	if(tactics6)
		// hack hack, calling world.TgsChatChannelInfo() will try to delay until the channels come back
		var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
		if (length(api.chat_channels))
			return "channels_present!"

		DetachedChatMessageQueuing()
		return "queued"

	var/tactics7 = data["tgs_integration_test_tactics7"]
	if(tactics7)
		var/list/channels = TgsChatChannelInfo()
		return length(channels)

	var/tactics8 = data["tgs_integration_test_tactics8"]
	if(tactics8)
		return received_health_check ? "received health check" : "did not receive health check"

	var/tactics_broadcast = data["tgs_integration_test_tactics_broadcast"]
	if(tactics_broadcast)
		return last_tgs_broadcast || "!!NULL!!"

	var/legalize_nuclear_bombs = data["shadow_wizard_money_gang"]
	if(legalize_nuclear_bombs)
		text2file("I expect this to remain here for a while", "kajigger.txt")
		kajigger_test = TRUE
		return "we love casting spells"

	var/its_sad = data["im_out_of_memes"]
	if(its_sad)
		TestLegacyBridge()
		return "all gucci"

	var/deploy_test = data["test_deployment_trigger"]
	if(deploy_test)
		return world.TgsTriggerDeployment() == TRUE ? "all gucci" : "deployment trigger failed!"

	TgsChatBroadcast(new /datum/tgs_message_content("Received non-tgs topic: `[T]`"))

	return "feck"

// Look I always forget how waitfor = FALSE works
/proc/DetachedChatMessageQueuing()
	set waitfor = FALSE
	DetachedChatMessageQueuingP2()

/proc/DetachedChatMessageQueuingP2()
	sleep(world.tick_lag)
	DetachedChatMessageQueuingP3()

/proc/DetachedChatMessageQueuingP3()
	set waitfor = FALSE
	world.TgsChatBroadcast(new /datum/tgs_message_content("1/3 queued detached chat messages"))
	world.TgsChatBroadcast(new /datum/tgs_message_content("2/3 queued detached chat messages"))
	world.TgsChatBroadcast(new /datum/tgs_message_content("3/3 queued detached chat messages"))

var/kajigger_test = FALSE

/world/Reboot(reason)
	log << "Reboot Start"
	TgsChatBroadcast("World Rebooting")

	if(kajigger_test && !fexists("kajigger.txt"))
		FailTest("TGS STOLE MY KAJIGGER (#1548 regression)")

	TgsReboot()

	log << "Calling base reboot"
	..()

var/received_health_check = FALSE

/datum/tgs_event_handler/impl
	receive_health_checks = TRUE

/datum/tgs_event_handler/impl/HandleEvent(event_code, ...)
	set waitfor = FALSE

	world.TgsChatBroadcast(new /datum/tgs_message_content("Received event: `[json_encode(args)]`"))

	if(event_code == TGS_EVENT_HEALTH_CHECK)
		received_health_check = TRUE
	else if(event_code == TGS_EVENT_WATCHDOG_DETACH)
		DelayCheckDetach()

/proc/DelayCheckDetach()
	sleep(world.tick_lag)
	// hack hack, calling world.TgsChatChannelInfo() will try to delay until the channels come back
	var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
	if(length(api.chat_channels))
		FailTest("Expected no chat channels after detach!")

/world/Export(url)
	var/redact = length(url) > 1000
	log << "Export (sleep_offline: [sleep_offline]): [redact ? "<REDACTED>" : url]"
	. = ..()
	log << "Export completed (sleep_offline: [sleep_offline]): [redact ? "<REDACTED>" : json_encode(.)]"

/proc/RebootAsync()
	set waitfor = FALSE
	world.TgsChatBroadcast(new /datum/tgs_message_content("Rebooting after 1 seconds"));
	world.log << "About to sleep. sleep_offline: [world.sleep_offline]"
	sleep(10)
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
	var/limit = 65529
	// this actually gets doubled because it's in two fields for backwards compatibility, but that's fine
	var/datum/tgs_message_content/response = new(create_payload(limit * 3))
	return response

var/lastTgsError
var/suppress_bridge_spam = FALSE

/proc/TgsInfo(message)
	if(suppress_bridge_spam && findtext(message, "Export: http://127.0.0.1:") != 0)
		return
	world.log << "Info: [message]"

/proc/TgsError(message)
	lastTgsError = message
	if(suppress_bridge_spam && findtext(message, "Failed bridge request: http://127.0.0.1:") != 0)
		return
	world.log << "Err: [message]"

/proc/create_payload(size)
	var/builder = list()
	for(var/j = 0; j < size; ++j)
		builder += "a"
	var/payload = jointext(builder, "")
	return payload

/proc/CheckBridgeLimits(id)
	set waitfor = FALSE
	CheckBridgeLimitsImpl(id)

/proc/CheckBridgeLimitsImpl(id)
	sleep(30)

	// Evil custom bridge command hacking here
	var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
	var/old_ai = api.access_identifier
	api.access_identifier = id

	lastTgsError = null

	var/limit = 8198 // DMAPI5_BRIDGE_REQUEST_LIMIT

	// Always send chat messages because they can have extremely large payloads with the text
	var/base_bridge_request = api.CreateBridgeRequest(0, list("chatMessage" = list("text" = "payload:")))

	// In 515 overloaded bridge requests started BUG-ing because of the HTTP 414 response it gets on errors
	// so now we can only test that the limit is valid
	// It's fine, chunking will handle the rest
	var/payload_size = limit - length(base_bridge_request)

	var/payload = create_payload(payload_size)
	var/bridge_request = api.CreateBridgeRequest(0, list("chatMessage" = list("text" = "payload:[payload]")))

	var/list/result
	try
		result = api.PerformBridgeRequest(bridge_request)
	catch(var/exception/e)
		world.log << "Caught exception: [e]"
		result = null

	if(!result || lastTgsError || result["integrationHack"] != "ok")
		FailTest("Failed bridge request limit test!")
		return

	// this actually gets doubled because it's in two fields for backwards compatibility, but that's fine
	var/list/final_result = api.Bridge(0, list("chatMessage" = list("text" = "done:[create_payload(limit * 3)]")))
	if(!final_result || lastTgsError || final_result["integrationHack"] != "ok")
		FailTest("Failed to end bridge limit test! [(istype(final_result) ? json_encode(final_result): (final_result || "null"))]")

	api.access_identifier = old_ai

/proc/TestLegacyBridge()
	var/datum/tgs_api/v5/api = TGS_READ_GLOBAL(tgs)
	if(api.interop_version.suite != 5)
		FailTest("Legacy bridge test not required anymore?")

	var/old_minor_version = api.interop_version.minor
	api.interop_version.minor = 6 // before api repath

	var/result
	var/bridge_request = api.CreateBridgeRequest(5, list("chatMessage" = list("text" = "legacy bridge test", "channelIds" = list())))
	try
		result = api.PerformBridgeRequest(bridge_request)
	catch(var/exception/e2)
		world.log << "Caught exception: [e2]"
		result = null

	if(!result || lastTgsError)
		FailTest("Failed bridge request redirect test!")

	api.interop_version.minor = old_minor_version
