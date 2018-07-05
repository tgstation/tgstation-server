#define TGS4_PARAM_INFO_JSON "tgs_json"

#define TGS4_TOPIC_COMMAND "tgs_com"
#define TGS4_TOPIC_TOKEN "tgs_tok"
#define TGS4_TOPIC_SUCCESS "tgs_succ"
#define TGS4_TOPIC_SWAP "tgs_swap"
#define TGS4_TOPIC_SWAP_DELAYED "tgs_swap_delayed"
#define TGS4_TOPIC_CHAT_COMMAND "tgs_chat_comm"
#define TGS4_TOPIC_EVENT "tgs_event"
#define TGS4_TOPIC_IDENTIFY "tgs_ident"

#define TGS4_COMM_VALIDATE "tgs_vali"
#define TGS4_COMM_SERVER_PRIMED "tgs_prime"
#define TGS4_COMM_SERVER_REBOOT "tgs_reboot"
#define TGS4_COMM_END_PROCESS "tgs_kill"
#define TGS4_COMM_CHAT "tgs_chat_send"

/datum/tgs_api/v4
	var/access_identifier
	var/instance_name
	var/host_path
	var/chat_channels_json_path
	var/chat_commands_json_path
	
	var/list/intercepted_message_queue
	
	var/list/custom_commands

	var/list/cached_test_merges
	var/datum/tgs_revision_information/cached_revision

	var/datum/tgs_event_handler/event_handler

/datum/tgs_api/v4/ApiVersion()
	return "4.0.0.0"

/datum/tgs_api/v4/OnWorldNew(datum/tgs_event_handler/event_handler)
	json_path = world.params[TGS4_PARAM_INFO_JSON]
	if(!json_path)
		TGS_ERROR_LOG("Missing [TGS4_PARAM_INFO_JSON] world parameter!")
		return
	var/json_file = file2text(json_path)
	if(!json_file)
		TGS_ERROR_LOG("Missing specified json file: [json_path]")
		return
	var/cached_json = json_decode(json_file)
	if(!cached_json)
		TGS_ERROR_LOG("Failed to decode info json: [json_file]")
		return

	access_token = cached_json["access_token"]
	instance_id = text2num(cached_json["instance_id"])
	host_path = cached_json["host_path"]
	if(cached_json["api_validate_only"])
		Export(TGS4_COMM_VALIDATE)
		del(world)
		
	chat_channels_json_path = cached_json["chat_channels_json"]
	chat_commands_json_path = cached_json["chat_commands_json"]
	src.event_handler = event_handler
	instance_name = cached_json["instance_name"]
	port_1 = world.port
	port_2 = cached_json["next_port"]
	ListCustomCommands()

/datum/tgs_api/v4/OnInitializationComplete()
	Export(TGS4_COMM_SERVER_PRIMED)
	var/tgs4_secret_sleep_offline_sauce = 24051994
	var/old_sleep_offline = world.sleep_offline
	sleep(1)
	if(world.sleep_offline == tgs4_secret_sleep_offline_sauce)	//if not someone changed it
		world.sleep_offline = old_sleep_offline

/datum/tgs_api/v4/OnTopic(T)
	var/list/params = params2list(T)
	var/their_sCK = params[TGS4_TOPIC_TOKEN]
	if(!their_sCK)
		return FALSE	//continue world/Topic

	if(their_sCK != access_token)
		return "Invalid comms key!";

	var/command = params[TGS4_TOPIC_COMMAND]
	if(!command)
		return "No command!"

	switch(command)
		if(TGS4_TOPIC_SWAP)
			SwapPorts(FALSE)
			return TGS4_TOPIC_SUCCESS
		if(TGS4_TOPIC_SWAP_DELAYED)
			SwapPorts(TRUE)
			return TGS4_TOPIC_SUCCESS
		if(TGS4_TOPIC_CHAT_COMMAND)
			var/result = HandleCustomCommand(params[TGS4_TOPIC_CHAT_COMMAND])
			if(!result)
				return json_encode(list("error" = "Error running chat command!"))
			return result
		if(TGS4_TOPIC_EVENT)
			intercepted_message_queue = list()
			event_handler.HandleEvent(text2num(params[TGS4_TOPIC_EVENT]))
			. = json_encode(intercepted_message_queue)
			intercepted_message_queue = null
			return
		if(TGS4_TOPIC_IDENTIFY)
			//they want to know our initial port
			return "[port_1]"
	
	return "Unknown command: [command]"

/datum/tgs_api/v4/proc/SwapPorts(delayed)
	set waitfor = FALSE
	var/new_port = world.port == port_1 ? port_2 : port_1
	event_handler.HandleEvent(TGS_EVENT_PORT_SWAP, new_port)
	if(delayed)
		world.OpenPort("none")	//close the port
		sleep(50)	//wait for other server to close port

	//do NOT give up, if we remain unresponsive we will be killed
	while(!world.OpenPort(new_port))
		sleep(10)

/datum/tgs_api/v4/proc/Export(command)
	var/list/res = world.Export("[host_path]/Interop?access=[url_encode(access_identifier)]&command=[url_encode(command)]")
	if(!istype(res))
		TGS_ERROR_LOG("Error contacting TGS webapi at [host_path]! Export returned something not a list (probably null): [res]")
		return
	var/byond_status = res["STATUS"]
	var/byond_content = res["CONTENT"]
	if(byond_status != "200 OK")
		TGS_ERROR_LOG("Error contacting TGS webapi at [host_path]! Top level HTTP [byond_status]: [byond_content]")
		return
	var/json = json_decode(byond_content)	//always expecting json
	if(!json)
		TGS_ERROR_LOG("Error contacting TGS webapi at [host_path]! Byond content not json: [byond_content]")
		return
	var/status = json["STATUS"]
	var/content = json["CONTENT"]
	if(status != 200)	//HTTP OK
		TGS_ERROR_LOG("Error contacting TGS webapi at [host_path]! HTTP [status]: [json_encode(content)]")
		return
	return content

/datum/tgs_api/v4/OnReboot()
	var/json = Export(TGS4_COMM_SERVER_REBOOT)
	var/list/result = json_decode(json)
	if(!result)
		return
	if(json["port_swap"])
		SwapPorts(json["delayed"])

/datum/tgs_api/v4/InstanceName()
	return instance_name

/datum/tgs_api/v4/TestMerges()
	if(cached_test_merges)
		return cached_test_merges
		
	. = list()
	cached_test_merges = .
	var/json = cached_json["test_merges"]
	for(var/I in json)
		var/datum/tgs_revision_information/test_merge/tm = new
		tm.number = text2num(I)
		var/list/entry = json[I]
		tm.pull_request_commit = entry["pr_commit"]
		tm.author = entry["author"]
		tm.title = entry["title"]
		tm.commit = entry["commit"]
		tm.origin_commit = entry["origin_commit"]
		tm.time_merged = text2num(entry["time_merged"])
		tm.comment = entry["comment"]
		tm.url = entry["url"]
		. += tm

/datum/tgs_api/v4/EndProcess()
	Export(TGS4_COMM_END_PROCESS)

/datum/tgs_api/v4/Revision()
	if(!cached_revision)
		var/json = cached_json["revision"]
		cached_revision = new
		cached_revision.commit = json["commit"]
		cached_revision.origin_commit = json["origin_commit"]
	return cached_revision

/datum/tgs_api/v4/ChatBroadcast(message, list/channels)
	var/list/ids
	if(length(channels))
		ids = list()
		for(var/I in channels)
			var/datum/tgs_chat_channel/channel = I
			ids += channel.id
	message = list("message" = message, "channels" = ids)
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export("[TGS4_COMM_CHAT] [json_encode(message)]")

/datum/tgs_api/v4/ChatTargetedBroadcast(message, admin_only)
	message = list("message" = message, "channels" = admin_only ? "admin" : "game")
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export("[TGS4_COMM_CHAT] [json_encode(message)]")

/datum/tgs_api/v4/ChatPrivateMessage(message, datum/tgs_chat_user/user)
	message = list("message" = message, "user" = list("id" = user.id, "channel" = user.channel.id))
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export("[TGS4_COMM_CHAT] [json_encode(message)]")

/datum/tgs_api/v4/ChatChannelInfo()
	. = list()
	//no caching cause tgs may change this
	var/list/json = json_decode(file2text(chat_channels_json_path))
	for(var/I in json)
		var/datum/tgs_chat_channel/channel = new
		channel.id = I["id"]
		channel.friendly_name = I["friendly_name"]
		channel.server_name = I["server_name"]
		channel.provider_name = I["provider_name"]
		channel.is_admin_channel = I["is_admin_channel"]
		channel.is_private_channel = FALSE	//tgs will never send us pm channels
		. += channel

#undef TGS4_TOPIC_COMMAND
#undef TGS4_TOPIC_TOKEN
#undef TGS4_TOPIC_SUCCESS
#undef TGS4_TOPIC_SWAP
#undef TGS4_TOPIC_SWAP_DELAYED
#undef TGS4_TOPIC_CHAT_COMMAND
#undef TGS4_TOPIC_EVENT

#undef TGS4_COMM_SERVER_PRIMED
#undef TGS4_COMM_SERVER_REBOOT
#undef TGS4_COMM_END_PROCESS
#undef TGS4_COMM_CHAT

#undef TGS4_COMM_VALIDATE

/*
The MIT License

Copyright (c) 2017 Jordan Brown

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
