using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGS.Interface;

namespace TGServiceTests
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
		
		protected virtual void OutputProc(string message) { }
	}
}
