using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace Tgstation.Server.Client.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="HubConnection"/> <see langword="class"/>.
	/// </summary>
	static class HubConnectionExtensions
	{
		/// <summary>
		/// Apply a given <paramref name="proxy"/> to a given <paramref name="hubConnection"/>.
		/// </summary>
		/// <typeparam name="TClientProxy">The strongly typed client proxy.</typeparam>
		/// <param name="hubConnection">The <see cref="HubConnection"/> to proxy on.</param>
		/// <param name="proxy">The <typeparamref name="TClientProxy"/> to forward operations to.</param>
		public static void ProxyOn<TClientProxy>(this HubConnection hubConnection, TClientProxy proxy)
			where TClientProxy : class
		{
			if (hubConnection == null)
				throw new ArgumentNullException(nameof(hubConnection));

			if (proxy == null)
				throw new ArgumentNullException(nameof(proxy));

			ProxyOn(hubConnection, typeof(TClientProxy), proxy);
		}

		/// <summary>
		/// Apply a given <paramref name="proxyObject"/> to a given <paramref name="hubConnection"/>.
		/// </summary>
		/// <param name="hubConnection">The <see cref="HubConnection"/> to proxy on.</param>
		/// <param name="proxyType">The <see cref="Type"/> of <paramref name="proxyObject"/>.</param>
		/// <param name="proxyObject">The <see cref="object"/> to forward operations to.</param>
		static void ProxyOn(this HubConnection hubConnection, Type proxyType, object proxyObject)
		{
			var clientMethods = proxyType.GetMethods();
			var cancellationTokenType = typeof(CancellationToken);
			foreach (var clientMethod in clientMethods)
			{
				var parametersList = clientMethod
					.GetParameters()
					.Select(parameterInfo => parameterInfo.ParameterType)
					.ToList();

				var cancellationTokenIndex = parametersList.IndexOf(cancellationTokenType);
				if (cancellationTokenIndex != -1)
				{
					parametersList.RemoveAt(cancellationTokenIndex);
#if DEBUG
					if (parametersList.IndexOf(cancellationTokenType) != -1)
						throw new InvalidOperationException("Cannot ProxyOn a method with multiple CancellationToken parameters!");
#endif
				}

				var parameters = parametersList.ToArray();

				object?[] AddCancellationTokenToParametersArray(object?[] parametersArray)
				{
					if (cancellationTokenIndex == -1)
						return parametersArray;

					var newList = parametersArray.ToList();
					newList.Insert(cancellationTokenIndex, CancellationToken.None);
					return newList.ToArray();
				}

				var returnType = clientMethod.ReturnType;
				if (returnType != typeof(Task))
				{
					if (returnType.BaseType != typeof(Task))
						throw new InvalidOperationException($"Return type {returnType} of {proxyType.FullName}.{clientMethod.Name} is not supported! Only Task and derivatives are supported.");

					var resultProperty = returnType.GetProperty(nameof(Task<object>.Result));
					hubConnection.On(
						clientMethod.Name,
						parameters,
						async (parameterArray, _) =>
						{
							var task = (Task)clientMethod.Invoke(proxyObject, AddCancellationTokenToParametersArray(parameterArray));
							await task;
							return resultProperty.GetValue(task);
						},
						hubConnection);
				}
				else
					hubConnection.On(
						clientMethod.Name,
						parameters,
						(parameterArray) =>
						{
							return (Task)clientMethod.Invoke(proxyObject, AddCancellationTokenToParametersArray(parameterArray));
						});
			}

			foreach (var inheritedInterface in proxyType.GetInterfaces())
				ProxyOn(hubConnection, inheritedInterface, proxyObject);
		}
	}
}
