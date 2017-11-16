using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IRepositoryManager : ITGRepository
	{
		/// <summary>
		/// Copy the repository (without git 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="ignore"></param>
		void CopyTo(string destination, IEnumerable<string> ignorePaths);
}
