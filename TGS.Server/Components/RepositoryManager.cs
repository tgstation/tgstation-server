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
		/// <param name="logger"></param>
		/// <param name="chat"></param>
		/// <param name="config"></param>
		public RepositoryManager(IInstanceLogger logger, IChatManager chat, IInstanceConfig config)
		{
			Logger = logger;
			Chat = chat;
			Config = config;
		}
	}
}
