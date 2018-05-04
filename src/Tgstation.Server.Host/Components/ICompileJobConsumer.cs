using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	interface ICompileJobConsumer
	{
		void LoadCompileJob(CompileJob job);
	}
}