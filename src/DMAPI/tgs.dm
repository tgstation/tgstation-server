// /tg/station 13 server tools DMAPI

//CONFIGURATION

//use this define if you want to do configuration outside of this file
#ifndef TGS_EXTERNAL_CONFIGURATION

//Comment this out once you've filled in the below
#error /tg/station server tools interface unconfigured

//Required interfaces (fill in with your codebase equivalent):

//create a global variable named `Name` and set it to `Value`
//These globals must not be modifiable from anywhere outside of the server tools
#define TGS_DEFINE_AND_SET_GLOBAL(Name, Value)

//Read the value in the global variable `Name`
#define TGS_READ_GLOBAL(Name)

//Set the value in the global variable `Name` to `Value`
#define TGS_WRITE_GLOBAL(Name, Value)

//Disallow ANYONE from reflecting a given path, security measure to prevent in-game priveledge escalation
#define TGS_PROTECT_DATUM(Path)

//display an announcement `message` from the server to all players
#define TGS_WORLD_ANNOUNCE(message)

//Write a string `message` to a server log
#define TGS_LOG(message)

//Notify current in-game administrators of a string `event`
#define TGS_NOTIFY_ADMINS(event)

#endif

//REQUIRED HOOKS

//Call this somewhere in /world/New() that is always run
/world/proc/TgsNew()
	return

//Put this somewhere in /world/Topic(T, Addr, Master, Keys) that is always run before T is modified
#define TGS_TOPIC var/tgs_topic_return = TgsCommand(params2list(T)); if(tgs_topic_return) return tgs_topic_return

//Call this at the beginning of world/Reboot(reason)
/world/proc/TgsReboot()
	return

//DATUM DEFINITIONS

//represents git revision information about the current world build
/datum/tgs_revision_information
	var/commit			//full sha of compiled commit
	var/origin_commit	//full sha of last known remote commit. This may be null if the TGS repository is not currently tracking a remote branch

//represents a merge of a GitHub pull request
/datum/tgs_revision_information/test_merge
	var/number				//pull request number
	var/body				//pull request body
	var/author				//pull request github author
	var/url					//link to pull request html
	var/pull_request_commit	//commit of the pull request when it was merged
	var/time_merged			//timestamp of when the merge commit for the pull request was created
	var/comment				//optional comment left by the one who initiated the test merge

//Gets a list of active `/datum/tgs_revision_information/test_merge`s
/world/proc/TgsGetTestMerges()
	return

/datum/tgs_chat_channel
	var/id					//internal channel representation
	var/friendly_name		//user friendly channel name
	var/server_name			//server name the channel resides on
	var/provider_name		//chat provider for the channel
	var/is_admin_channel	//if the server operator has marked this channel for game admins only

/datum/tgs_chat_user
	var/id						//Internal user representation
	var/friendly_name			//The user's public name
	var/datum/tgs_chat_channel	//The /datum/tgs_chat_channel this user was from
	var/mention					//The text to use to ping this user in a message

//FUNCTIONS

//Returns the respective string version of the API
/world/proc/TgsMaximumAPIVersion()
	return

/world/proc/TgsMinimumAPIVersion()
	return

//Returns TRUE if the world was launched under the server tools and the API matches, FALSE otherwise
//No function below this succeeds if it returns FALSE
/world/proc/TgsAvailable()
	return

//Gets the current version of the service running the server
/world/proc/TgsVersion()
	return

//Forces a hard reboot of BYOND by ending the process
//unlike del(world) clients will try to reconnect
//If the service has not requested a shutdown, the next server will take over
/world/proc/TgsEndProcess(silent)
	return

//Gets a list of connected tgs_chat_channel
/world/proc/TgsChatChannelInfo()
	return
	
//Sends a message to connected game chats
//message: The message to send
//channels: optional channels to limit the broadcast to
/world/proc/TgsChatBroadcast(message, list/channels)
	return

//Send a message to non-admin connected chats
//message: The message to send
//admin_only: If TRUE, message will instead be sent to only admin connected chats
/world/proc/TgsTargetedChatBroadcast(message, admin_only)
	return

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
