using Byond.TopicSender;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	sealed class DreamDaemonControl : IDreamDaemonControl
	{
		public bool IsPrimary
		{
			get
			{
				CheckDisposed();
				return reattachInformation.IsPrimary;
			}
		}

		public IDmbProvider Dmb
		{
			get
			{
				CheckDisposed();
				return reattachInformation.Dmb;
			}
		}

		public ushort Port => throw new NotImplementedException();

		readonly DreamDaemonReattachInformation reattachInformation;

		readonly IByondTopicSender byondTopicSender;

		IDreamDaemonSession session;

		public DreamDaemonControl(DreamDaemonReattachInformation reattachInformation, IDreamDaemonSession session, IByondTopicSender byondTopicSender)
		{
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.session = session ?? throw new ArgumentNullException(nameof(session));
		}

		public void Dispose()
		{
			lock (this)
				if (session != null)
				{
					session.Dispose();
					Dmb?.Dispose();	//will be null when released
					session = null;
				}
		}

		void CheckDisposed()
		{
			if (session == null)
				throw new ObjectDisposedException(nameof(DreamDaemonControl));
		}

		public DreamDaemonReattachInformation Release()
		{
			var tmpProvider = reattachInformation.Dmb;
			reattachInformation.Dmb = null;
			Dispose();
			Dmb.KeepAlive();
			reattachInformation.Dmb = tmpProvider;
			return reattachInformation;
		}

		public Task<string> SendCommand(string command, CancellationToken cancellationToken) => byondTopicSender.SendTopic(new IPEndPoint(IPAddress.Loopback, reattachInformation.Port), String.Format(CultureInfo.InvariantCulture, "?command={0}", command), cancellationToken);

		public Task SwapPorts(CancellatonToken cancellatonToken)
		{
			throw new NotImplementedException();
		}
	}
}
