namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Server response for <see cref="Instance"/>s.
	/// </summary>
	public sealed class InstanceResponse : Instance
	{
		/// <summary>
		/// The <see cref="JobResponse"/> representing a change of <see cref="Instance.Path"/>.
		/// </summary>
		/// <remarks>Due to how <see cref="JobResponse"/>s are children of <see cref="Instance"/>s but moving one requires the <see cref="Instance"/> to be offline, interactions with this <see cref="JobResponse"/> are performed in a non-standard fashion. The <see cref="JobResponse"/> is read by querying the <see cref="Instance"/> again (either via list or ID lookup) and cancelled by making any sort of update to the <see cref="Instance"/>. Once the <see cref="Instance"/> comes back <see cref="Instance.Online"/> it can be queried like a normal job.</remarks>
		[ResponseOptions]
		public JobResponse? MoveJob { get; set; }
	}
}
