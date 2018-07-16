namespace Tgstation.Server.Host.Components.Chat.Commands
{
	public abstract class Command
	{
		public string Name { get; set; }
		public string HelpText { get; set; }
		public bool AdminOnly { get; set; }

		public abstract void Invoke(string arguments);
	}
}
