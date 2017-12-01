using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using TGS.Interface.Proxying;

namespace TGS.Server.Proxying
{
	class Request
	{
		const int TokenLength = 7;
		const int RequestTimeoutInterval = 60;

		static readonly TimeSpan RequestTimeout = new TimeSpan(0, 0, RequestTimeoutInterval);

		public string Token => token;

		protected readonly Task task;
		readonly string token;

		DateTime lastQueried;

		public Request(Task _task)
		{
			task = _task;
			token = Membership.GeneratePassword(TokenLength, 0);
			lastQueried = DateTime.UtcNow;
		}

		public bool TimedOut()
		{
			var sinceQuery = DateTime.UtcNow - lastQueried;
			return sinceQuery > RequestTimeout;
		}

		public bool ValidateToken(string providedToken)
		{
			return Token == providedToken;
		}

		public RequestInfo GetRequestInfo(UInt64 id, string providedToken)
		{
			lastQueried = DateTime.UtcNow;
			return new RequestInfo(ValidateToken(providedToken) ? (task.IsCompleted ? RequestState.Finished : RequestState.InProgress) : RequestState.BadToken, id, Token);
		}

		public virtual object GetResult(string providedToken)
		{
			if (ValidateToken(providedToken))
				task.Wait();
			return null;
		}
	}
}
