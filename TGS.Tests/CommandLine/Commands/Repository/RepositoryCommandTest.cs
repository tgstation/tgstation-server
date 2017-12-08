using Moq;
using TGS.Interface;
using TGS.Interface.Components;
using TGS.Tests;

namespace TGS.CommandLine.Commands.Repository.Tests
{
	public abstract class RepositoryCommandTest : OutputProcOverriderTest
	{
		/// <summary>
		/// Creates a mock <see cref="IClient"/> that returns a specific <see cref="ITGRepository"/> when that component is requested
		/// </summary>
		/// <param name="repo">The <see cref="ITGRepository"/> the resulting <see cref="IClient.GetComponent{T}"/> should return</param>
		/// <returns>A mock <see cref="IClient"/></returns>
		protected IInstance MockInterfaceToRepo(ITGRepository repo)
		{
			var mock = new Mock<IInstance>();
			mock.Setup(foo => foo.Repository).Returns(repo);
			return mock.Object;
		}
	}
}
