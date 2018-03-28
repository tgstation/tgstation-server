using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TGS.Server.IoC.Tests
{
	/// <summary>
	/// Unit tests for <see cref="Instance"/>
	/// </summary>
	[TestClass]
	public class TestDependencyInjector
	{
		interface IDepA { }
		interface IDepB { }
		class ImplA : IDepA { }
		class ImplB : IDepB
		{
			public readonly IDepA depA;
			public ImplB(IDepA _depA)
			{
				depA = _depA;
			}
		}
		class ImplC : IDepA
		{
			public readonly IDepB depB;
			public ImplC(IDepB _depB)
			{
				depB = _depB;
			}
		}

		[TestMethod]
		public void TestValidation()
		{
			using (var DI = new DependencyInjector())
			{
				var A = new ImplA();

				DI.Register<IDepA>(A);
				DI.Register<IDepB, ImplB>();

				DI.Setup();

				Assert.AreSame(A, DI.GetComponent<IDepA>());
				Assert.AreEqual(typeof(ImplB), DI.GetComponent<IDepB>().GetType());

				var B = (ImplB)DI.GetComponent<IDepB>();

				Assert.AreSame(A, B.depA);
			}
		}

		[TestMethod]
		public void TestInvalidation()
		{
			using (var DI = new DependencyInjector())
			{
				var A = new ImplA();

				DI.Register<IDepA, ImplC>();
				DI.Register<IDepB, ImplB>();

				Assert.ThrowsException<InvalidOperationException>(() => DI.Setup());
			}
		}

		[TestMethod]
		public void TestServiceHosting()
		{
			using (var DI = new DependencyInjector())
			{
				var A = new ImplA();

				DI.Register<IDepA>(A);
				DI.Register<IDepB, ImplB>();

				DI.Setup();

				var B = DI.GetComponent<IDepB>();

				using (var sc = DI.CreateServiceHost(typeof(ImplB), new Uri[] { new Uri("http://tempuri.org") }))
					Assert.AreSame(B, sc.SingletonInstance);
			}
		}
	}
}
