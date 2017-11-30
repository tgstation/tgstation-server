using System.Threading.Tasks;

namespace TGS.Server.Proxying
{
	sealed class ResultingRequest<T> : Request
	{
		public ResultingRequest(Task<T> task) : base(task) { }

		public override object GetResult(string providedToken)
		{
			if (!ValidateToken(providedToken))
				return null;
			return ((Task<T>)task).Result;
		}
	}
}
