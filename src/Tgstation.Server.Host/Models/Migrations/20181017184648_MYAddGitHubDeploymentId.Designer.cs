﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Models.Migrations
{
	[DbContext(typeof(MySqlDatabaseContext))]
	[Migration("20181017184648_MYAddGitHubDeploymentId")]
	partial class MYAddGitHubDeploymentId
	{
		/// <summary>
		/// Builds the target model
		/// </summary>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/> to use</param>
		protected override void BuildTargetModel(ModelBuilder modelBuilder)
		{
#pragma warning disable 612, 618
			modelBuilder
				.HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
				.HasAnnotation("Relational:MaxIdentifierLength", 64);

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("ConnectionString")
						.IsRequired();

					b.Property<bool?>("Enabled");

					b.Property<long>("InstanceId");

					b.Property<string>("Name")
						.IsRequired();

					b.Property<int?>("Provider");

					b.HasKey("Id");

					b.HasIndex("InstanceId");

					b.HasIndex("Name")
						.IsUnique();

					b.ToTable("ChatBots");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<long>("ChatSettingsId");

					b.Property<ulong?>("DiscordChannelId");

					b.Property<string>("IrcChannel");

					b.Property<bool?>("IsAdminChannel")
						.IsRequired();

					b.Property<bool?>("IsUpdatesChannel")
						.IsRequired();

					b.Property<bool?>("IsWatchdogChannel")
						.IsRequired();

					b.Property<string>("Tag");

					b.HasKey("Id");

					b.HasIndex("ChatSettingsId", "DiscordChannelId")
						.IsUnique();

					b.HasIndex("ChatSettingsId", "IrcChannel")
						.IsUnique();

					b.ToTable("ChatChannels");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("ByondVersion")
						.IsRequired();

					b.Property<Guid?>("DirectoryName")
						.IsRequired();

					b.Property<string>("DmeName")
						.IsRequired();

					b.Property<int?>("GitHubDeploymentId");

					b.Property<long>("JobId");

					b.Property<int>("MinimumSecurityLevel");

					b.Property<string>("Output")
						.IsRequired();

					b.Property<long>("RevisionInformationId");

					b.HasKey("Id");

					b.HasIndex("DirectoryName");

					b.HasIndex("JobId")
						.IsUnique();

					b.HasIndex("RevisionInformationId");

					b.ToTable("CompileJobs");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamDaemonSettings", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("AccessToken");

					b.Property<bool?>("AllowWebClient")
						.IsRequired();

					b.Property<bool?>("AutoStart")
						.IsRequired();

					b.Property<long>("InstanceId");

					b.Property<ushort?>("PrimaryPort")
						.IsRequired();

					b.Property<int?>("ProcessId");

					b.Property<ushort?>("SecondaryPort")
						.IsRequired();

					b.Property<int>("SecurityLevel");

					b.Property<bool?>("SoftRestart")
						.IsRequired();

					b.Property<bool?>("SoftShutdown")
						.IsRequired();

					b.Property<uint?>("StartupTimeout")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("InstanceId")
						.IsUnique();

					b.ToTable("DreamDaemonSettings");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<ushort?>("ApiValidationPort")
						.IsRequired();

					b.Property<int>("ApiValidationSecurityLevel");

					b.Property<long>("InstanceId");

					b.Property<string>("ProjectName");

					b.HasKey("Id");

					b.HasIndex("InstanceId")
						.IsUnique();

					b.ToTable("DreamMakerSettings");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Instance", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<uint?>("AutoUpdateInterval")
						.IsRequired();

					b.Property<int>("ConfigurationType");

					b.Property<string>("Name")
						.IsRequired();

					b.Property<bool?>("Online")
						.IsRequired();

					b.Property<string>("Path")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("Path")
						.IsUnique();

					b.ToTable("Instances");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstanceUser", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<ulong>("ByondRights");

					b.Property<ulong>("ChatBotRights");

					b.Property<ulong>("ConfigurationRights");

					b.Property<ulong>("DreamDaemonRights");

					b.Property<ulong>("DreamMakerRights");

					b.Property<long>("InstanceId");

					b.Property<ulong>("InstanceUserRights");

					b.Property<ulong>("RepositoryRights");

					b.Property<long?>("UserId")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("InstanceId");

					b.HasIndex("UserId", "InstanceId")
						.IsUnique();

					b.ToTable("InstanceUsers");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<ulong?>("CancelRight");

					b.Property<ulong?>("CancelRightsType");

					b.Property<bool?>("Cancelled")
						.IsRequired();

					b.Property<long?>("CancelledById");

					b.Property<string>("Description")
						.IsRequired();

					b.Property<string>("ExceptionDetails");

					b.Property<long>("InstanceId");

					b.Property<DateTimeOffset?>("StartedAt")
						.IsRequired();

					b.Property<long>("StartedById");

					b.Property<DateTimeOffset?>("StoppedAt");

					b.HasKey("Id");

					b.HasIndex("CancelledById");

					b.HasIndex("InstanceId");

					b.HasIndex("StartedById");

					b.ToTable("Jobs");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("AccessIdentifier")
						.IsRequired();

					b.Property<string>("ChatChannelsJson")
						.IsRequired();

					b.Property<string>("ChatCommandsJson")
						.IsRequired();

					b.Property<long>("CompileJobId");

					b.Property<bool>("IsPrimary");

					b.Property<ushort>("Port");

					b.Property<int>("ProcessId");

					b.Property<int>("RebootState");

					b.Property<string>("ServerCommandsJson")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("CompileJobId");

					b.ToTable("ReattachInformations");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("AccessToken");

					b.Property<string>("AccessUser");

					b.Property<bool?>("AutoUpdatesKeepTestMerges")
						.IsRequired();

					b.Property<bool?>("AutoUpdatesSynchronize")
						.IsRequired();

					b.Property<string>("CommitterEmail")
						.IsRequired();

					b.Property<string>("CommitterName")
						.IsRequired();

					b.Property<long>("InstanceId");

					b.Property<bool?>("PushTestMergeCommits")
						.IsRequired();

					b.Property<bool?>("ShowTestMergeCommitters")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("InstanceId")
						.IsUnique();

					b.ToTable("RepositorySettings");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<long>("RevisionInformationId");

					b.Property<long>("TestMergeId");

					b.HasKey("Id");

					b.HasIndex("RevisionInformationId");

					b.HasIndex("TestMergeId");

					b.ToTable("RevInfoTestMerges");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("CommitSha")
						.IsRequired()
						.HasMaxLength(40);

					b.Property<long>("InstanceId");

					b.Property<string>("OriginCommitSha")
						.IsRequired()
						.HasMaxLength(40);

					b.HasKey("Id");

					b.HasIndex("CommitSha")
						.IsUnique();

					b.HasIndex("InstanceId");

					b.ToTable("RevisionInformations");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<string>("Author")
						.IsRequired();

					b.Property<string>("BodyAtMerge")
						.IsRequired();

					b.Property<string>("Comment");

					b.Property<DateTimeOffset>("MergedAt");

					b.Property<long>("MergedById");

					b.Property<int?>("Number")
						.IsRequired();

					b.Property<long?>("PrimaryRevisionInformationId")
						.IsRequired();

					b.Property<string>("PullRequestRevision")
						.IsRequired();

					b.Property<string>("TitleAtMerge")
						.IsRequired();

					b.Property<string>("Url")
						.IsRequired();

					b.HasKey("Id");

					b.HasIndex("MergedById");

					b.HasIndex("PrimaryRevisionInformationId")
						.IsUnique();

					b.ToTable("TestMerges");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<ulong>("AdministrationRights");

					b.Property<string>("CanonicalName")
						.IsRequired();

					b.Property<DateTimeOffset?>("CreatedAt")
						.IsRequired();

					b.Property<long?>("CreatedById");

					b.Property<bool?>("Enabled")
						.IsRequired();

					b.Property<ulong>("InstanceManagerRights");

					b.Property<DateTimeOffset?>("LastPasswordUpdate");

					b.Property<string>("Name")
						.IsRequired();

					b.Property<string>("PasswordHash");

					b.Property<string>("SystemIdentifier");

					b.HasKey("Id");

					b.HasIndex("CanonicalName")
						.IsUnique();

					b.HasIndex("CreatedById");

					b.ToTable("Users");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.WatchdogReattachInformation", b =>
				{
					b.Property<long>("Id")
						.ValueGeneratedOnAdd();

					b.Property<long?>("AlphaId");

					b.Property<bool>("AlphaIsActive");

					b.Property<long?>("BravoId");

					b.Property<long>("InstanceId");

					b.HasKey("Id");

					b.HasIndex("AlphaId");

					b.HasIndex("BravoId");

					b.HasIndex("InstanceId")
						.IsUnique();

					b.ToTable("WatchdogReattachInformations");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithMany("ChatSettings")
						.HasForeignKey("InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.ChatBot", "ChatSettings")
						.WithMany("Channels")
						.HasForeignKey("ChatSettingsId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Job", "Job")
						.WithOne()
						.HasForeignKey("Tgstation.Server.Host.Models.CompileJob", "JobId")
						.OnDelete(DeleteBehavior.Restrict);

					b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
						.WithMany("CompileJobs")
						.HasForeignKey("RevisionInformationId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamDaemonSettings", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithOne("DreamDaemonSettings")
						.HasForeignKey("Tgstation.Server.Host.Models.DreamDaemonSettings", "InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithOne("DreamMakerSettings")
						.HasForeignKey("Tgstation.Server.Host.Models.DreamMakerSettings", "InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstanceUser", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithMany("InstanceUsers")
						.HasForeignKey("InstanceId")
						.OnDelete(DeleteBehavior.Cascade);

					b.HasOne("Tgstation.Server.Host.Models.User")
						.WithMany("InstanceUsers")
						.HasForeignKey("UserId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.User", "CancelledBy")
						.WithMany()
						.HasForeignKey("CancelledById");

					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithMany("Jobs")
						.HasForeignKey("InstanceId")
						.OnDelete(DeleteBehavior.Cascade);

					b.HasOne("Tgstation.Server.Host.Models.User", "StartedBy")
						.WithMany()
						.HasForeignKey("StartedById")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.CompileJob", "CompileJob")
						.WithMany()
						.HasForeignKey("CompileJobId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithOne("RepositorySettings")
						.HasForeignKey("Tgstation.Server.Host.Models.RepositorySettings", "InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
						.WithMany("ActiveTestMerges")
						.HasForeignKey("RevisionInformationId")
						.OnDelete(DeleteBehavior.Cascade);

					b.HasOne("Tgstation.Server.Host.Models.TestMerge", "TestMerge")
						.WithMany("RevisonInformations")
						.HasForeignKey("TestMergeId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
						.WithMany("RevisionInformations")
						.HasForeignKey("InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.User", "MergedBy")
						.WithMany("TestMerges")
						.HasForeignKey("MergedById")
						.OnDelete(DeleteBehavior.Restrict);

					b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "PrimaryRevisionInformation")
						.WithOne("PrimaryTestMerge")
						.HasForeignKey("Tgstation.Server.Host.Models.TestMerge", "PrimaryRevisionInformationId")
						.OnDelete(DeleteBehavior.Restrict);
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.User", "CreatedBy")
						.WithMany("CreatedUsers")
						.HasForeignKey("CreatedById");
				});

			modelBuilder.Entity("Tgstation.Server.Host.Models.WatchdogReattachInformation", b =>
				{
					b.HasOne("Tgstation.Server.Host.Models.ReattachInformation", "Alpha")
						.WithMany()
						.HasForeignKey("AlphaId");

					b.HasOne("Tgstation.Server.Host.Models.ReattachInformation", "Bravo")
						.WithMany()
						.HasForeignKey("BravoId");

					b.HasOne("Tgstation.Server.Host.Models.Instance")
						.WithOne("WatchdogReattachInformation")
						.HasForeignKey("Tgstation.Server.Host.Models.WatchdogReattachInformation", "InstanceId")
						.OnDelete(DeleteBehavior.Cascade);
				});
#pragma warning restore 612, 618
		}
	}
}
