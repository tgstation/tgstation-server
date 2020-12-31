﻿using System;
using Tgstation.Server.Host.Components.Interop.Topic;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Combines a <see cref="global::Byond.TopicSender.TopicResponse"/> with a <see cref="TopicResponse"/>.
	/// </summary>
	sealed class CombinedTopicResponse
	{
		/// <summary>
		/// The raw <see cref="global::Byond.TopicSender.TopicResponse"/>.
		/// </summary>
		public global::Byond.TopicSender.TopicResponse ByondTopicResponse { get; }

		/// <summary>
		/// The interop <see cref="TopicResponse"/>, if any.
		/// </summary>
		public TopicResponse InteropResponse { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CombinedTopicResponse"/> <see langword="class"/>.
		/// </summary>
		/// <param name="byondTopicResponse">The value of <see cref="ByondTopicResponse"/>.</param>
		/// <param name="interopResponse">The optional value of <see cref="InteropResponse"/>.</param>
		public CombinedTopicResponse(global::Byond.TopicSender.TopicResponse byondTopicResponse, TopicResponse interopResponse)
		{
			ByondTopicResponse = byondTopicResponse ?? throw new ArgumentNullException(nameof(byondTopicResponse));
			InteropResponse = interopResponse;
		}
	}
}
