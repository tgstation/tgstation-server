#define SERVER_TOOLS_EXTERNAL_CONFIGURATION
#define SERVER_TOOLS_DEFINE_AND_SET_GLOBAL(Name, Value) var/##Name = ##Value
#define SERVER_TOOLS_READ_GLOBAL(Name) global.##Name
#define SERVER_TOOLS_WRITE_GLOBAL(Name, Value) global.##Name = ##Value
#define SERVER_TOOLS_WORLD_ANNOUNCE(message) world << ##message
#define SERVER_TOOLS_LOG(message) world.log << ##message
#define SERVER_TOOLS_NOTIFY_ADMINS(event) message_admins(event)
#define SERVER_TOOLS_CLIENT_COUNT clients.len