using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel.Tests
{
	/// <summary>
	/// Tests for <see cref="InstanceSelector"/>
	/// </summary>
	[TestClass]
	public class TestInstanceSelector
	{
		/// <summary>
		/// Test that an <see cref="InstanceSelector"/> can be created without issue
		/// </summary>
		[TestMethod]
		public void TestInstatiation()
		{
			var mockLanding = new Mock<ITGLanding>();
			mockLanding.Setup(x => x.ListInstances()).Returns(new List<InstanceMetadata>());
			var mockInter = new Mock<IServer>();
		
			new InstanceSelector(mockInter.Object).Dispose();
		}
	}
}
