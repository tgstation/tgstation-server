using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGS.Interface;

namespace TGServiceTests
{
	/// <summary>
	/// For tests that need to override <see cref="Command.OutputProcVar"/>
	/// </summary>
	public abstract class OutputProcOverriderTest
	{
		/// <summary>
		/// Set <see cref="Command.OutputProcVar"/> to <see cref="OutputProc(string)"/>
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			Command.OutputProcVar.Value = OutputProc;
		}

		/// <summary>
		/// Whe
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OutputProc(string message) { }
	}
}
