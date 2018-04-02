using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Basic <see cref="Api"/> client
	/// </summary>
	/// <typeparam name="TRights">The <see cref="Client.Rights"/> for the <see cref="IClient{TRights}"/></typeparam>
	public interface IClient<TRights>
	{
		/// <summary>
		/// The connection timeout in milliseconds
		/// </summary>
		int Timeout { get; set; }

		/// <summary>
		/// The requery rate for job updates in milliseconds
		/// </summary>
		int RequeryRate { get; set; }

		/// <summary>
		/// Get the <typeparamref name="TRights"/> for the <see cref="IClient{TRights}"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <typeparamref name="TRights"/> for the <see cref="IClient{TRights}"/></returns>
		Task<TRights> Rights();
	}
}
