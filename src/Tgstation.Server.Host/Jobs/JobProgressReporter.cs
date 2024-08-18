using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Progress reporter for a <see cref="Job"/>.
	/// </summary>
	public sealed class JobProgressReporter : IDisposable
	{
		/// <summary>
		/// The name of the current stage.
		/// </summary>
		public string? StageName
		{
			get => stageName;
			set
			{
				if (stageName == value)
					return;

				stageName = value;
				callback(stageName, lastProgress);
			}
		}

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="JobProgressReporter"/>.
		/// </summary>
		readonly ILogger<JobProgressReporter> logger;

		/// <summary>
		/// Progress reporter callback taking a description of what the job is currently doing and the (optional) progress of the job on a scale from 0.0-1.0.
		/// </summary>
		readonly Action<string?, double?> callback;

		/// <summary>
		/// Backing field for <see cref="StageName"/>.
		/// </summary>
		string? stageName;

		/// <summary>
		/// The last progress value pushed into the <see cref="callback"/>.
		/// </summary>
		double? lastProgress;

		/// <summary>
		/// The total progress reported so far in this section.
		/// </summary>
		double sectionProgression;

		/// <summary>
		/// The total progress reserved for use in this section.
		/// </summary>
		double? sectionReservations;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobProgressReporter"/> class.
		/// </summary>
		/// <remarks>This variant has no function.</remarks>
		public JobProgressReporter()
			: this(
				 NullLogger<JobProgressReporter>.Instance,
				 null,
				 (_, _) => { },
				 false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobProgressReporter"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="stageName">The value of <see cref="StageName"/>.</param>
		/// <param name="callback">The value of <see cref="callback"/>.</param>
		public JobProgressReporter(ILogger<JobProgressReporter> logger, string? stageName, Action<string?, double?> callback)
			: this(
				 logger,
				 stageName,
				 callback,
				 true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobProgressReporter"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="stageName">The value of <see cref="StageName"/>.</param>
		/// <param name="callback">The value of <see cref="callback"/>.</param>
		/// <param name="setStageName">If <see langword="true"/> an initial call to <paramref name="callback"/> will be made with only the <paramref name="stageName"/>.</param>
		private JobProgressReporter(ILogger<JobProgressReporter> logger, string? stageName, Action<string?, double?> callback, bool setStageName)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
			if (setStageName)
			{
				StageName = stageName;
			}
			else
			{
				this.stageName = stageName;
			}

			logger.LogDebug("Job progress reporter created. Stage: {stageName}", stageName ?? "(null)");
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (sectionReservations.HasValue)
				if (sectionReservations.Value != 1.0)
				{
					// not an error, processes can throw
					sectionReservations = null;
				}
				else if (sectionProgression < 1.0)
				{
					logger.LogError(
						new InvalidOperationException($"Parent progress reporter has child sections that didn't complete! Current: {sectionProgression}"),
						"TGS BUG: Progress reporter children didn't complete!");
					sectionReservations = null;
				}

			if (!sectionReservations.HasValue)
				ReportProgress(1);
		}

		/// <summary>
		/// Report progress.
		/// </summary>
		/// <param name="progress">A percentage value from 0.0f-1.0f.</param>
		public void ReportProgress(double? progress)
		{
			if (sectionReservations.HasValue)
				if (progress == 0)
				{
					// might be a stage reset
					sectionReservations = null;
				}
				else
				{
					logger.LogError(
						new InvalidOperationException("Progress reporter is reporting progress with existing nested sections!"),
						"TGS BUG: A progress reporter is using mixed local and nested progress, this is not supported");
				}

			var clampedProgress = progress;
			if (progress.HasValue)
				if (progress > 1 || progress < 0)
				{
					logger.LogError(
						new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a value from 0-1!"),
						"TGS BUG: Invalid progress value for stage {stageName}",
						StageName ?? "(null)");
					clampedProgress = null;
				}
				else
					sectionProgression = progress.Value;

			callback(StageName, clampedProgress);
			lastProgress = clampedProgress;
		}

		/// <summary>
		/// Create a subsection of the <see cref="JobProgressReporter"/> with its optional own stage name.
		/// </summary>
		/// <param name="newStageName">The optional <see cref="StageName"/> of the new <see cref="JobProgressReporter"/>.</param>
		/// <param name="percentage">The 0.0f-1.0f percentage of the current <see cref="JobProgressReporter"/>'s percentage should be given to the section.</param>
		/// <returns>A new <see cref="JobProgressReporter"/> that is a subsection of this one.</returns>
		/// <remarks>A <see cref="JobProgressReporter"/> should only have one active child at a time.</remarks>
		public JobProgressReporter CreateSection(string? newStageName, double percentage)
		{
			if (percentage > 1 || percentage < 0)
			{
				logger.LogError(
					new ArgumentOutOfRangeException(nameof(percentage), percentage, "Percentage must be a value from 0-1!"),
					"TGS BUG: Invalid percentage value for stage {newStageName}! Clamping...",
					newStageName ?? "(null)");

				percentage = Math.Min(Math.Max(percentage, 0.0), 1.0);
			}

			if (!sectionReservations.HasValue)
			{
				if (sectionProgression != 0)
				{
					logger.LogError(
						new InvalidOperationException("Progress reporter is creating a section with local progress!"),
						"TGS BUG: A progress reporter is using mixed local and nested progress, this is not supported");
				}

				sectionReservations = 0;
			}

			if (percentage + sectionReservations.Value > 1.0001) // floating point >.<
			{
				var remainingPercentage = 1.0 - sectionReservations.Value;
				logger.LogError(
					"Stage {newStageName} is overbudgeted ({budget}/{remainingPercentage})! Clamping...",
					newStageName,
					percentage,
					remainingPercentage);
				percentage = remainingPercentage;
			}

			Math.Min(sectionReservations.Value + percentage, 1);

			var childLocalProgress = 0.0;
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

					var progressWithoutChild = sectionProgression - childLocalProgress;
					childLocalProgress = progress.Value * percentage;

					// floating point >.<
					sectionProgression = Math.Min(progressWithoutChild + childLocalProgress, 1);
					if (sectionProgression > 9.9999)
						sectionProgression = 1;

					callback(currentStage, sectionProgression);
				},
				false);

			newReporter.ReportProgress(0);
			return newReporter;
		}
	}
}
