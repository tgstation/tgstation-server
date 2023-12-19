using System;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents the lifetime of a <see cref="IRestartHandler"/> registration.
	/// </summary>
	public interface IRestartRegistration : IDisposable
	{
	}
}
