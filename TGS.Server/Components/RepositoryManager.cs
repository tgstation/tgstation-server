using LibGit2Sharp;
using Newtonsoft.Json;
using Octokit;
using System;
using System.ServiceModel;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
	sealed class RepositoryManager : IRepositoryManager, IDisposable
	{
		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager IO;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		public RepositoryManager(IInstanceLogger logger, IChatManager chat, IInstanceConfig config, IIOManager io)
		{
			Logger = logger;
			Chat = chat;
			Config = config;
			IO = io;
		}
	}
}
