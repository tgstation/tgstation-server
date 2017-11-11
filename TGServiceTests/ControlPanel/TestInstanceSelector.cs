using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel.Tests
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
			var mockInter = new Mock<IInterface>();
			mockInter.Setup(x => x.GetComponent<ITGLanding>()).Returns(mockLanding.Object);
		
			new InstanceSelector(mockInter.Object).Dispose();
		}
	}
}
