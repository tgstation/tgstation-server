﻿using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// Factory for scoping usage of <see cref="IDatabaseContext"/>s. Meant for use by <see cref="Components"/>
	/// </summary>
	public interface IDatabaseContextFactory
	{
		/// <summary>
		/// Run an <paramref name="operation"/> in the scope of an <see cref="IDatabaseContext"/>
		/// </summary>
		/// <param name="operation">The operation to run</param>
		/// <returns>A <see cref="Task"/> representing the running <paramref name="operation"/></returns>
		Task UseContext(Func<IDatabaseContext, Task> operation);
	}
}
