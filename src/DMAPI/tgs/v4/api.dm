#define TGS4_API_VALIDATE "tgs_vali"
#define TGS4_PARAM_TOKEN "tgs_tok"
#define TGS4_PARAM_INSTANCE "tgs_inst_name"
#define TGS4_PARAM_INSTANCE_ID "tgs_inst_id"
#define TGS4_PARAM_REVISION_JSON "tgs_json"
#define TGS4_PARAM_HOST_PORT "tgs_port"
#define TGS4_PARAM_SECOND_PORT "tgs_port2"

#define TGS4_TOPIC_COMMAND "tgs_com"

#define TGS4_COMM_SERVER_PRIMED "tgs_prime"
#define TGS4_COMM_END_PROCESS "tgs_kill"
#define TGS4_COMM_CHAT_BROADCAST "tgs_chat"
#define TGS4_COMM_CHAT_PM "tgs_chat_pm"

/datum/tgs_api/v4
	var/access_token
	var/instance_id
	var/instance_name
	var/host_port
	var/port_1
	var/port_2
	var/revision_json_path

/datum/tgs_api/v4/ApiVersion()
	return "4.0.0.0"

/datum/tgs_api/v4/OnWorldNew(datum/tgs_event_handler/event_handler)
	access_token = world.params[TGS4_PARAM_TOKEN]
	instance_id = world.params[TGS4_PARAM_INSTANCE_ID]
	host_port = world.params[TGS4_PARAM_HOST_PORT]
	if(world.params[TGS4_API_VALIDATE])
		Export(TGS4_API_VALIDATE)
		del(world)

	instance_name = world.params[TGS4_PARAM_INSTANCE]
	port_1 = world.port
	port_2 = world.params[TGS4_PARAM_SECOND_PORT]
	revision_json_path = world.params[TGS4_PARAM_REVISION_JSON]

/datum/tgs_api/v4/OnInitializationComplete()
	Export(TGS4_COMM_SERVER_PRIMED)
	var/old_sleep_offline = world.sleep_offline
	sleep(1)
	world.sleep_offline = old_sleep_offline

/datum/tgs_api/v4/OnTopic(T)
	var/list/params = params2list(T)
	var/their_sCK = params[TGS4_PARAM_TOKEN]
	if(!their_sCK)
		return FALSE	//continue world/Topic

	if(their_sCK != access_token)
		return "Invalid comms key!";

	var/command = params[TGS4_TOPIC_COMMAND]
	if(!command)
		return "No command!"

	switch(command)
	
	return "Unknown command: [command]"

/datum/tgs_api/v4/proc/Export(command)
	return world.Export("http://127.0.0.1:[host_port]/Interop/[instance_id]?command=[command]&access_token=[access_token]")

/datum/tgs_api/v4/OnReboot()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/v4/InstanceName()
	return instance_name

/datum/tgs_api/v4/TestMerges()
	//do the best we can here as the datum can't be completed using the v3 api
	. = list()
	if(!revision_json_path || !fexists(revision_json_path))
		return
	var/list/json = json_decode(file2text(revision_json_path))
	if(!json)
		return
	json = json["test_merges"]
	for(var/I in json)
		var/datum/tgs_revision_information/test_merge/tm = new
		tm.number = text2num(I)
		var/list/entry = json[I]
		tm.pull_request_commit = entry["pr_commit"]
		tm.author = entry["author"]
		tm.title = entry["title"]
		tm.commit = entry["commit"]
		tm.commit = entry["origin_commit"]
		tm.time_merged = text2num(entry["time_merged"])
		tm.comment = entry["comment"]
		tm.url = entry["url"]
		. += tm

/datum/tgs_api/v4/EndProcess()
	Export(TGS4_COMM_END_PROCESS)

/datum/tgs_api/v4/Revision()
	if(!revision_json_path || !fexists(revision_json_path))
		return
	var/list/json = json_decode(file2text(revision_json_path))
	if(!json)
		return
	var/datum/tgs_revision_information

/datum/tgs_api/v4/ChatChannelInfo()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/v4/ChatBroadcast(message, list/channels)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/v4/ChatTargetedBroadcast(message, admin_only)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/v4/ChatPrivateMessage(message, datum/tgs_chat_user/user)
	Export("[TGS_COMM_CHAT_PM] [user.id] [user.channel.id]")

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
