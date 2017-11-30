using System.Threading.Tasks;

namespace TGS.Interface.Proxying
{
	interface ICallBinder
	{
		Task<T> HandleCall<T>(string componentName, string methodName, object[] args);
	}
}
