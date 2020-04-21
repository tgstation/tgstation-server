using Microsoft.AspNetCore.Hosting.Server.Features;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerPortProivder : IServerPortProvider
	{
		/// <inheritdoc />
		public Task<ushort> HttpApiPort => taskCompletionSource.Task;

		/// <summary>
		/// Backing <see cref="TaskCompletionSource{TResult}"/> field for <see cref="HttpApiPort"/>/
		/// </summary>
		readonly TaskCompletionSource<ushort> taskCompletionSource;

		/// <summary>
		/// In
		/// </summary>
		public ServerPortProivder()
		{
			taskCompletionSource = new TaskCompletionSource<ushort>();
		}

		/// <inheritdoc />
		public void Configure(IServerAddressesFeature addressFeature)
		{
			if (addressFeature == null)
				throw new ArgumentNullException(nameof(addressFeature));

			var enumerator = addressFeature.Addresses.Select(GetPortFromAddress);
			var newPort = enumerator.FirstOrDefault(x => x.HasValue);

			if(!newPort.HasValue)
				throw new InvalidOperationException("At least one plain HTTP endpoint must be configured. Neded for BYOND -> Server communications!");

			if (!addressFeature.Addresses.Select(GetPortFromAddress).All(x => !x.HasValue || x == newPort))
				throw new InvalidOperationException("All configured HTTP server addresses must use the same port!");

			// Will fail if set twice
			taskCompletionSource.SetResult(newPort.Value);
		}

		/// <summary>
		/// Convert a given <paramref name="address"/> to its port.
		/// </summary>
		/// <param name="address">The address <see cref="string"/>.</param>
		/// <returns>The parsed port.</returns>
		static ushort? GetPortFromAddress(string address)
		{
			var splits = address.Split(":", StringSplitOptions.RemoveEmptyEntries);
			if (splits.First().Equals("https", StringComparison.OrdinalIgnoreCase))
				return null;

			var portString = splits.Last();
			portString = portString.TrimEnd('/');
			if (UInt16.TryParse(portString, out var result))
				return result;

			return null;
		}
	}
}
