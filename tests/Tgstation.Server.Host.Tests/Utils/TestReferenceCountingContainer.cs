using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Utils.Tests
{
	[TestClass]
	public sealed class TestReferenceCountingContainer
	{
		interface IInterface
		{
		}

		sealed class Implementation : IInterface
		{
		}

		sealed class Lock : ReferenceCounter<IInterface>
		{
			public IInterface PubInstance => Instance;
		}


		[TestMethod]
		public void TestReferenceCounting()
		{
			var impl = new Implementation();
			var container = new ReferenceCountingContainer<IInterface, Lock>(impl);

			Assert.IsTrue(container.OnZeroReferences.IsCompleted);

			var ref1 = container.AddReference();

			Assert.AreSame(ref1.PubInstance, impl);

			var task = container.OnZeroReferences;

			Assert.AreSame(task, container.OnZeroReferences);
			Assert.IsFalse(task.IsCompleted);

			ref1.Dispose();

			Assert.IsTrue(task.IsCompleted);
			Assert.IsTrue(container.OnZeroReferences.IsCompleted);

			ref1.Dispose();
			Assert.IsTrue(container.OnZeroReferences.IsCompleted);

			var ref2 = container.AddReference();
			Assert.IsFalse((task = container.OnZeroReferences).IsCompleted);


			ref2.Dispose();
			Assert.IsTrue(task.IsCompleted);
			Assert.IsTrue(container.OnZeroReferences.IsCompleted);
		}
	}
}
