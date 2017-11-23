using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGS.Interface;

namespace TGS.TestHelpers
{
	/// <summary>
	/// For tests that need to override <see cref="Command.OutputProcVar"/>
	/// </summary>
	public abstract class OutputProcOverriderTest
	{
		[TestInitialize]
		public void Setup()
		{
			Command.OutputProcVar.Value = OutputProc;
		}

		/// <summary>
		/// Dummy OutputProc implementation
		/// </summary>
		/// <param name="message">Unused</param>
		protected virtual void OutputProc(string message) { }
	}
}
