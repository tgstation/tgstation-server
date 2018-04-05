using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : MarshalByRefObject, IWatchdog
	{
        string DomainName => GetType().Namespace.Replace(nameof(Watchdog), String.Empty);

        string DllName => String.Concat(DomainName, ".dll");

        async Task<string> RunServer(string[] args, CancellationToken cancellationToken)
        {
            var assembly = Assembly.LoadFrom(DllName);
            //the only thing we need to reflect is the IServerFactory, we can cross reference everything else from there
            var factoryInterfaceType = assembly.GetType(String.Concat(DomainName, ".IServerFactory"));
            var factoryType = assembly.GetTypes().Where(x => factoryInterfaceType.IsAssignableFrom(x)).First();
            var factory = Activator.CreateInstance(factoryType);
            var factoryFunction = factoryInterfaceType.GetMethods().First();
            var serverType = factoryFunction.ReturnType;
            var serverRunFunction = serverType.GetMethods().First();
            var serverUpdatePathAccessor = serverType.GetProperties().First().GetAccessors().First();

            var server = (IDisposable)factoryFunction.Invoke(factory, Array.Empty<object>());
            using (server)
            {
                var task = (Task)serverRunFunction.Invoke(server, new object[] { args, cancellationToken });
                await task.ConfigureAwait(false);
                return (string)serverUpdatePathAccessor.Invoke(server, Array.Empty<object>());
            }
        }

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
            {
                string updatePath = null;

                await Task.CompletedTask.ConfigureAwait(false);

                if (updatePath == null)
                    break;
                
                //Ensure the assembly is unloaded
                GC.Collect(Int32.MaxValue, GCCollectionMode.Default, true);

                File.Delete(DllName);
                File.Move(updatePath, DllName);
            }
		}
	}
}
