using System;
using System.ComponentModel;
using System.Linq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.Job" />
#pragma warning disable CA1724 // naming conflict with gitlab package
	public sealed class Job : Api.Models.Internal.Job, ILegacyApiTransformable<JobResponse>
#pragma warning restore CA1724
	{
		/// <summary>
		/// See <see cref="JobResponse.StartedBy"/>.
		/// </summary>
		public User StartedBy { get; set; } = null!; // recommended by EF

		/// <summary>
		/// See <see cref="JobResponse.CancelledBy"/>.
		/// </summary>
		public User? CancelledBy { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the job belongs to if any.
		/// </summary>
		public Instance Instance { get; set; } = null!; // recommended by EF

		/// <summary>
		/// Creates a new job for registering in the <see cref="Jobs.IJobService"/>.
		/// </summary>
		/// <typeparam name="TRight">The <see cref="RightsType"/> of <paramref name="cancelRight"/>.</typeparam>
		/// <param name="code">The value of <see cref="Api.Models.Internal.Job.JobCode"/>. <see cref="Api.Models.Internal.Job.Description"/> will be derived from this.</param>
		/// <param name="startedBy">The value of <see cref="StartedBy"/>. If <see langword="null"/>, the <see cref="User.TgsSystemUserName"/> user will be used.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> used to generate the value of <see cref="Instance"/>.</param>
		/// <param name="cancelRight">The value of <see cref="Api.Models.Internal.Job.CancelRight"/>. <see cref="Api.Models.Internal.Job.CancelRightsType"/> will be derived from this.</param>
		/// <returns>A new <see cref="Job"/> ready to be registered with the <see cref="Jobs.IJobService"/>.</returns>
		public static Job Create<TRight>(JobCode code, User? startedBy, Api.Models.Instance instance, TRight cancelRight)
			where TRight : Enum
			=> new(
				code,
				startedBy,
				instance,
				RightsHelper.TypeToRight<TRight>(),
				(ulong)(object)cancelRight);

		/// <summary>
		/// Creates a new job for registering in the <see cref="Jobs.IJobService"/>.
		/// </summary>
		/// <param name="code">The value of <see cref="Api.Models.Internal.Job.JobCode"/>. <see cref="Api.Models.Internal.Job.Description"/> will be derived from this.</param>
		/// <param name="startedBy">The value of <see cref="StartedBy"/>. If <see langword="null"/>, the <see cref="User.TgsSystemUserName"/> user will be used.</param>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> used to generate the value of <see cref="Instance"/>.</param>
		/// <returns>A new <see cref="Job"/> ready to be registered with the <see cref="Jobs.IJobService"/>.</returns>
		public static Job Create(JobCode code, User? startedBy, Api.Models.Instance instance)
			=> new(
				code,
				startedBy,
				instance,
				null,
				null);

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		[Obsolete("For use by EFCore only", true)]
		public Job()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		/// <param name="id">The value of <see cref="EntityId.Id"/>.</param>
		public Job(long id)
		{
			Id = id;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		/// <param name="code">The value of <see cref="Api.Models.Internal.Job.JobCode"/>.</param>
		/// <param name="startedBy">The value of <see cref="StartedBy"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		/// <param name="cancelRightsType">The value of <see cref="Api.Models.Internal.Job.CancelRightsType"/>.</param>
		/// <param name="cancelRight">The value of <see cref="Api.Models.Internal.Job.CancelRight"/>.</param>
		Job(JobCode code, User? startedBy, Api.Models.Instance instance, RightsType? cancelRightsType, ulong? cancelRight)
		{
			StartedBy = startedBy!; // allowed to be null here, set to TGS user if so later
			ArgumentNullException.ThrowIfNull(instance);
			Instance = new Instance
			{
				Id = instance.Id ?? throw new InvalidOperationException("Instance associated with job does not have an Id!"),
			};
			Description = typeof(JobCode)
				.GetField(code.ToString())!
				.GetCustomAttributes(false)
				.OfType<DescriptionAttribute>()
				.First()
				.Description;
			JobCode = code;
			CancelRight = cancelRight;
			CancelRightsType = cancelRightsType;
		}

		/// <inheritdoc />
		public JobResponse ToApi() => new()
		{
			Id = Id,
			JobCode = this.Require(x => x.JobCode),
			InstanceId = (Instance ?? throw new InvalidOperationException("Instance needs to be set!")).Require(x => x.Id),
			StartedAt = StartedAt,
			StoppedAt = StoppedAt,
			Cancelled = Cancelled,
			CancelledBy = CancelledBy?.CreateUserName(),
			CancelRight = CancelRight,
			CancelRightsType = CancelRightsType,
			Description = Description,
			ExceptionDetails = ExceptionDetails,
			ErrorCode = ErrorCode,
			StartedBy = (StartedBy ?? throw new InvalidOperationException("StartedBy needs to be set!")).CreateUserName(),
		};
	}
}
