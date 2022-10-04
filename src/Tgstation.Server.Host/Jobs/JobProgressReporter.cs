using System;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Progress reporter for a <see cref="Job"/>.
	/// </summary>
	public sealed class JobProgressReporter
	{
		/// <summary>
		/// The name of the current stage.
		/// </summary>
		public string StageName { get; set; }

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="JobProgressReporter"/>.
		/// </summary>
		readonly ILogger<JobProgressReporter> logger;

		/// <summary>
		/// Progress reporter callback taking a description of what the job is currently doing and the (optional) progress of the job on a scale from 0.0-1.0.
		/// </summary>
		readonly Action<string, double?> callback;

		/// <summary>
		/// The total progress reported so far in this section.
		/// </summary>
		double sectionProgression;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobProgressReporter"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="stageName">The value of <see cref="StageName"/>.</param>
		/// <param name="callback">The value of <see cref="callback"/>.</param>
		public JobProgressReporter(ILogger<JobProgressReporter> logger, string stageName, Action<string, double?> callback)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
			StageName = stageName;

			logger.LogDebug("Job progress reporter created. Stage: {stageName}", stageName ?? "(null)");
		}

		/// <summary>
		/// Report progress.
		/// </summary>
		/// <param name="progress">A percentage value from 0.0f-1.0f.</param>
		public void ReportProgress(double? progress)
		{
			var clampedProgress = progress;
			if (progress.HasValue)
				if (progress > 1 || progress < 0)
				{
					logger.LogError(
						new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a value from 0-1!"),
						"Invalid progress value for stage {stageName}",
						StageName ?? "(null)");
					clampedProgress = null;
				}
				else
					sectionProgression = progress.Value;

			callback(StageName, clampedProgress);
		}

		/// <summary>
		/// Create a subsection of the <see cref="JobProgressReporter"/> with its optional own stage name.
		/// </summary>
		/// <param name="newStageName">The optional <see cref="StageName"/> of the new <see cref="JobProgressReporter"/>.</param>
		/// <param name="percentage">The 0.0f-1.0f percentage of the current <see cref="JobProgressReporter"/>'s percentage should be given to the section.</param>
		/// <returns>A new <see cref="JobProgressReporter"/> that is a subsection of this one.</returns>
		/// <remarks>A <see cref="JobProgressReporter"/> should only have one active child at a time.</remarks>
		public JobProgressReporter CreateSection(string newStageName, double percentage)
		{
			if (percentage > 1 || percentage < 0)
			{
				logger.LogError(
					new ArgumentOutOfRangeException(nameof(percentage), percentage, "Percentage must be a value from 0-1!"),
					"Invalid percentage value for stage {newStageName}! Clamping...",
					newStageName ?? "(null)");

				percentage = Math.Min(Math.Max(percentage, 0.0), 1.0);
			}

			var childBaseProgress = sectionProgression;
			if (percentage + childBaseProgress > 1.0)
			{
				var remainingPercentage = 1.0 - childBaseProgress;
				logger.LogError(
					"Stage {newStageName} is overbudgeted ({budget}/{remainingPercentage})! Clamping...",
					newStageName,
					percentage,
					remainingPercentage);
				percentage = remainingPercentage;
			}

			var newReporter = new JobProgressReporter(
				logger,
				newStageName,
				(currentStage, progress) =>
				{
					currentStage ??= StageName;
					if (!progress.HasValue)
					{
						callback(currentStage, null);
						return;
					}

					var childLocalProgress = progress.Value * percentage;

					sectionProgression = childLocalProgress + childBaseProgress;
					callback(currentStage, sectionProgression);
				});

			newReporter.ReportProgress(0);
			return newReporter;
		}
	}
}
