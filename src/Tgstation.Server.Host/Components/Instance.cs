using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	sealed class Instance : IInstance
	{
		public IRepositoryManager RepositoryManager { get; }

		public IByond Byond { get; }

		public IDreamMaker DreamMaker { get; }

		public IDreamDaemon DreamDaemon { get; }

		public IChat Chat { get; }

		public IConfiguration Configuration { get; }

		readonly Api.Models.Instance metadata;

		public Instance(Api.Models.Instance metadata, IRepositoryManager repositoryManager, IByond byond, IDreamMaker dreamMaker, IDreamDaemon dreamDaemon, IChat chat, IConfiguration configuration)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			Byond = byond ?? throw new ArgumentNullException(nameof(byond));
			DreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			DreamDaemon = dreamDaemon ?? throw new ArgumentNullException(nameof(dreamDaemon));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public Api.Models.Instance GetMetadata() => metadata.CloneMetadata();

		public void Rename(string newName)
		{
			if (String.IsNullOrWhiteSpace(newName))
				throw new ArgumentNullException(nameof(newName));
			metadata.Name = newName;
		}

		public Task StartAsync(CancellationToken cancellationToken) => Task.WhenAll(RepositoryManager.StartAsync(cancellationToken), DreamDaemon.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken));

		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(RepositoryManager.StopAsync(cancellationToken), DreamDaemon.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken));
	}
}
