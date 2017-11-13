using Moq;
using TGServiceInterface;
using TGServiceInterface.Components;
using TGServiceTests;

namespace TGCommandLine.Commands.Repository.Tests
{
	public abstract class RepositoryCommandTest : OutputProcOverriderTest
	{
		/// <summary>
		/// Creates a mock <see cref="IServerInterface"/> that returns a specific <see cref="ITGRepository"/> when that component is requested
		/// </summary>
		/// <param name="repo">The <see cref="ITGRepository"/> the resulting <see cref="IServerInterface.GetComponent{T}"/> should return</param>
		/// <returns>A mock <see cref="IServerInterface"/></returns>
		protected IServerInterface MockInterfaceToRepo(ITGRepository repo)
		{
			var mock = new Mock<IServerInterface>();
			mock.Setup(foo => foo.GetComponent<ITGRepository>()).Returns(repo);
			return mock.Object;
		}
	}
}
