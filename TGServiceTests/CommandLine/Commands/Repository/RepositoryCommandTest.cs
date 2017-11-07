using Moq;
using TGServiceInterface;
using TGServiceInterface.Components;
using TGServiceTests;

namespace TGCommandLine.Commands.Repository.Tests
{
	public abstract class RepositoryCommandTest : OutputProcOverriderTest
	{
		/// <summary>
		/// Creates a mock <see cref="IInterface"/> that returns a specific <see cref="ITGRepository"/> when that component is requested
		/// </summary>
		/// <param name="repo">The <see cref="ITGRepository"/> the resulting <see cref="IInterface.GetComponent{T}"/> should return</param>
		/// <returns>A mock <see cref="IInterface"/></returns>
		protected IInterface MockInterfaceToRepo(ITGRepository repo)
		{
			var mock = new Mock<IInterface>();
			mock.Setup(foo => foo.GetComponent<ITGRepository>()).Returns(repo);
			return mock.Object;
		}
	}
}
