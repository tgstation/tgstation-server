using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	sealed class TopicResponse
	{
		public string ErrorMessage { get; set; }

		public string CommandResponse { get; set; }

		public ICollection<Response> ChatResponses { get; set; }
	}
}
