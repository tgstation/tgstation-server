/datum/tgs_api/v5
	var/access_identifier
	var/instance_name
	var/json_path
	var/chat_channels_json_path
	var/chat_commands_json_path
	var/reboot_mode = TGS_REBOOT_MODE_NORMAL
	var/security_level
	var/server_port
	
	var/list/intercepted_message_queue
	
	var/list/custom_commands

	var/list/cached_test_merges
	var/datum/tgs_revision_information/cached_revision

	var/datum/tgs_event_handler/event_handler

	var/export_lock = FALSE

/datum/tgs_api/v5/ApiVersion()
	return "5.0.0"

/datum/tgs_api/v5/OnWorldNew(datum/tgs_event_handler/event_handler, minimum_required_security_level)
	json_path = world.params[DMAPI5_PARAM_RUNTIME_INFORMATION_FILE]
	if(!json_path)
		TGS_ERROR_LOG("Missing [DMAPI5_PARAM_RUNTIME_INFORMATION_FILE] world parameter!")
		return
	var/json_file = file2text(json_path)
	if(!json_file)
		TGS_ERROR_LOG("Missing specified json file: [json_path]")
		return
	var/cached_json = json_decode(json_file)
	if(!cached_json)
		TGS_ERROR_LOG("Failed to decode info json: [json_file]")
		return

	access_identifier = cached_json[DMAPI5_RUNTIME_INFORMATION_ACCESS_IDENTIFIER]
	server_port = cached_json[DMAPI5_RUNTIME_INFORMATION_SERVER_PORT]

	if(cached_json[DMAPI5_RUNTIME_INFORMATION_API_VALIDATE_ONLY])
		TGS_INFO_LOG("Validating DMAPI and exiting...")
		Bridge(DMAPI5_BRIDGE_COMMAND_VALIDATE, list(DMAPI5_BRIDGE_PARAMETER_MINIMUM_SECURITY_LEVEL = minimum_required_security_level, DMAPI5_BRIDGE_PARAMETER_VERSION = Version()))
		del(world)

	security_level = cached_json[DMAPI5_RUNTIME_INFORMATION_SECURITY_LEVEL]
	chat_channels_json_path = cached_json[DMAPI5_RUNTIME_INFORMATION_CHAT_CHANNELS_JSON]
	chat_commands_json_path = cached_json[DMAPI5_RUNTIME_INFORMATION_CHAT_COMMANDS_JSON]
	src.event_handler = event_handler
	instance_name = cached_json[DMAPI5_RUNTIME_INFORMATION_INSTANCE_NAME]

	ListCustomCommands()

	var/list/revisionData = cached_json[DMAPI5_RUNTIME_INFORMATION_REVISION]
	if(revisionData)
		cached_revision = new
		cached_revision.commit = revisionData[DMAPI5_REVISION_INFORMATION_COMMIT_SHA]
		cached_revision.origin_commit = revisionData[DMAPI5_REVISION_INFORMATION_ORIGIN_COMMIT_SHA]

	cached_test_merges = list()
	var/list/json = cached_json[DMAPI5_RUNTIME_INFORMATION_TEST_MERGES]
	for(var/entry in json)
		var/datum/tgs_revision_information/test_merge/tm = new
		tm.time_merged = text2num(entry[DMAPI5_TEST_MERGE_TIME_MERGED])

		var/list/revInfo = entry[DMAPI5_TEST_MERGE_REVISION]
		if(revInfo)
			tm.commit = revisionData[DMAPI5_REVISION_INFORMATION_COMMIT_SHA]
			tm.origin_commit = revisionData[DMAPI5_REVISION_INFORMATION_ORIGIN_COMMIT_SHA]

		tm.title = entry[DMAPI5_TEST_MERGE_TITLE_AT_MERGE]
		tm.body = entry[DMAPI5_TEST_MERGE_BODY_AT_MERGE]
		tm.url = entry[DMAPI5_TEST_MERGE_URL]
		tm.author = entry[DMAPI5_TEST_MERGE_AUTHOR]
		tm.number = entry[DMAPI5_TEST_MERGE_NUMBER]
		tm.pull_request_commit = entry[DMAPI5_TEST_MERGE_PULL_REQUEST_REVISION]
		tm.comment = entry[DMAPI5_TEST_MERGE_COMMENT]

		cached_test_merges += tm

	return TRUE

/datum/tgs_api/v5/OnInitializationComplete()
	Bridge(TGS4_COMM_SERVER_PRIMED)

	var/tgs4_secret_sleep_offline_sauce = 29051994
	var/old_sleep_offline = world.sleep_offline
	world.sleep_offline = tgs4_secret_sleep_offline_sauce
	sleep(1)
	if(world.sleep_offline == tgs4_secret_sleep_offline_sauce)	//if not someone changed it
		world.sleep_offline = old_sleep_offline

/datum/tgs_api/v5/TopicError(message)
	return json_encode(list(DMAPI5_RESPONSE_ERROR_MESSAGE = message))

/datum/tgs_api/v5/OnTopic(T)
	var/list/params = params2list(T)
	var/json = params[DMAPI5_TOPIC_DATA]
	if(!json)
		return FALSE	//continue world/Topic

	var/list/topic_parameters = json_decode(json)
	if(!topic_parameters)
		return TopicError("Invalid topic parameters json!");

	var/their_sCK = topic_parameters[DMAPI5_PARAMETER_ACCESS_IDENTIFIER]
	if(their_sCK != access_identifier)
		return TopicError("Invalid access identifier!");

	var/command = topic_parameters[DMAPI5_TOPIC_PARAMETER_COMMAND_TYPE]
	if(command == null)
		return TopicError("No command type!")

	switch(command)
		if(DMAPI5_TOPIC_COMMAND_CHAT_COMMAND)
			var/result = HandleCustomCommand(topic_parameters[DMAPI5_TOPIC_PARAMETER_CHAT_COMMAND])
			if(!result)
				result = TopicError("Error running chat command!")
			return result
		if(DMAPI5_TOPIC_COMMAND_EVENT_NOTIFICATION)
			intercepted_message_queue = list()
			var/list/event_notification = topic_parameters[DMAPI5_TOPIC_PARAMETER_EVENT_NOTIFICATION]
			var/list/event_parameters = event_notification[DMAPI5_EVENT_NOTIFICATION_PARAMETERS]
			var/list/event_call = list(event_notification[DMAPI5_EVENT_NOTIFICATION_TYPE])
			if(event_parameters)
				event_call += event_parameters

			if(event_handler != null)
				event_handler.HandleEvent(arglist(event_call))

			var/list/response = list()
			if(intercepted_message_queue.len)
				response[DMAPI5_TOPIC_RESPONSE_CHAT_RESPONSES] = intercepted_message_queue
			intercepted_message_queue = null
			return json_encode(response)
		if(DMAPI5_TOPIC_COMMAND_CHANGE_PORT)
			var/new_port = text2num(topic_parameters[DMAPI5_TOPIC_PARAMETER_NEW_PORT])
			if (!(new_port > 0))
				return TopicError("Invalid port: [new_port]")

			//the topic still completes, miraculously
			//I honestly didn't believe byond could do it
			if(event_handler != null)
				event_handler.HandleEvent(TGS_EVENT_PORT_SWAP, new_port)
			if(!world.OpenPort(new_port))
				return TopicError("Port change failed!")
			return json_encode(list())
		if(DMAPI5_TOPIC_COMMAND_CHANGE_REBOOT_STATE)
			var/new_reboot_mode = text2num(topic_parameters[DMAPI5_TOPIC_PARAMETER_NEW_REBOOT_STATE])
			if(event_handler != null)
				event_handler.HandleEvent(TGS_EVENT_REBOOT_MODE_CHANGE, reboot_mode, new_reboot_mode)
			reboot_mode = new_reboot_mode
			return json_encode(list())
		if(DMAPI5_TOPIC_COMMAND_INSTANCE_RENAMED)
			var/new_instance_name = topic_parameters[DMAPI5_TOPIC_PARAMETER_NEW_INSTANCE_NAME]
			if(!new_instance_name)
				return TopicError("Missing new instance name!")
			instance_name = new_instance_name
			return json_encode(list())

	return TopicError("Unknown command: [command]")

/datum/tgs_api/v5/proc/Bridge(command, list/data)
	if(command == null)
		TGS_ERROR_LOG("Attempted to bridge with no command!")
		return

	if(!data)
		data = list()

	data[DMAPI5_BRIDGE_PARAMETER_COMMAND_TYPE] = command
	data[DMAPI5_PARAMETER_ACCESS_IDENTIFIER] = access_identifier

	var/json = json_encode(data)
	var/encoded_json = url_encode(json)

	// This is an infinite sleep until we get a response
	var/export_response = world.Export("http://127.0.0.1:[server_port]/Bridge?[DMAPI5_BRIDGE_DATA]=[encoded_json]")
	if(!export_response)
		TGS_ERROR_LOG("Failed export request: [json]")
		return

	var/response_json = file2text(export_result["CONTENT"])
	if(!response_json)
		TGS_ERROR_LOG("Failed export request, missing content!")
		return

	var/list/bridge_response = json_decode(response_json)
	if(!bridge_response)
		TGS_ERROR_LOG("Failed export request, bad json: [response_json]")
		return
	
	var/error = bridge_response[DMAPI5_RESPONSE_ERROR_MESSAGE]
	if(error)
		TGS_ERROR_LOG("Failed export request, bad request: [error]")
		return
	
	return bridge_response

/datum/tgs_api/v5/OnReboot()
	var/list/result = Bridge(DMAPI5_BRIDGE_COMMAND_REBOOT)
	if(!result)
		return
	
	//okay so the standard TGS4 proceedure is: right before rebooting change the port to whatever was sent to us in the above json's data parameter

	var/port = result[DMAPI5_BRIDGE_RESPONSE_NEW_PORT]
	if(!isnum(port))
		return	//this is valid, server may just want use to reboot

	if(port == 0)
		//to byond 0 means any port and "none" means close vOv
		port = "none"

	if(!world.OpenPort(port))
		TGS_ERROR_LOG("Unable to set port to [port]!")

/datum/tgs_api/v5/InstanceName()
	return instance_name

/datum/tgs_api/v5/TestMerges()
	return cached_test_merges
	
/datum/tgs_api/v5/EndProcess()
	Bridge(DMAPI5_BRIDGE_COMMAND_KILL)

/datum/tgs_api/v5/Revision()
	return cached_revision

/datum/tgs_api/v5/ChatBroadcast(message, list/channels)
	var/list/ids
	if(length(channels))
		ids = list()
		for(var/I in channels)
			var/datum/tgs_chat_channel/channel = I
			ids += channel.id
	message = list(DMAPI5_CHAT_MESSAGE_TEXT = message, DMAPI5_CHAT_MESSAGE_CHANNEL_IDS = ids)
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Bridge(DMAPI5_BRIDGE_PARAMETER_CHAT_MESSAGE, message)

/datum/tgs_api/v5/ChatTargetedBroadcast(message, admin_only)
	var/list/channels = list()
	for(var/I in ChatChannelInfo())
		var/datum/tgs_chat_channel/channel = I
		if (!channel.is_private_channel && ((channel.is_admin_channel && admin_only) || (!channel.is_admin_channel && !admin_only)))
			channels += channel.id
	message = list(DMAPI5_CHAT_MESSAGE_TEXT = message, DMAPI5_CHAT_MESSAGE_CHANNEL_IDS = channels)
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export(TGS4_COMM_CHAT, message)

/datum/tgs_api/v5/ChatPrivateMessage(message, datum/tgs_chat_user/user)
	message = list(DMAPI5_CHAT_MESSAGE_TEXT = message, DMAPI5_CHAT_MESSAGE_CHANNEL_IDS = list(user.channel.id))
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export(TGS4_COMM_CHAT, message)

/datum/tgs_api/v5/ChatChannelInfo()
	. = list()
	//no caching cause tgs may change this
	var/list/json = json_decode(file2text(chat_channels_json_path))
	for(var/I in json)
		. += DecodeChannel(I)

/datum/tgs_api/v5/proc/DecodeChannel(channel_json)
	var/datum/tgs_chat_channel/channel = new
	channel.id = channel_json[DMAPI5_CHAT_CHANNEL_ID]
	channel.friendly_name = channel_json[DMAPI5_CHAT_CHANNEL_FRIENDLY_NAME]
	channel.connection_name = channel_json[DMAPI5_CHAT_CHANNEL_CONNECTION_NAME]
	channel.is_admin_channel = channel_json[DMAPI5_CHAT_CHANNEL_IS_ADMIN_CHANNEL]
	channel.is_private_channel = channel_json[DMAPI5_CHAT_CHANNEL_IS_PRIVATE_CHANNEL]
	channel.custom_tag = channel_json[DMAPI5_CHAT_CHANNEL_TAG]
	return channel

/datum/tgs_api/v5/SecurityLevel()
	return security_level

/*
The MIT License

Copyright (c) 2020 Jordan Brown

Permission is hereby granted, free of charge,
to any person obtaining a copy of this software and
associated documentation files (the "Software"), to
deal in the Software without restriction, including
without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom
the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR
ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
