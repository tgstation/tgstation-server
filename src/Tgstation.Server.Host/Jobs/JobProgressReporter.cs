using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Progress reporter for a <see cref="Job"/>.
	/// </summary>
	/// <param name="stage">A description of what the job is currently doing.</param>
	/// <param name="progress">The progress of the job on a scale from 0-100.</param>
	public delegate void JobProgressReporter(
		string? stage,
		int? progress);
}
