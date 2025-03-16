﻿// <auto-generated />
using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	[DbContext(typeof(SqliteDatabaseContext))]
	[Migration("20250302232622_SLAddOidcConnections")]
	partial class SLAddOidcConnections
	{
		/// <inheritdoc />
		protected override void BuildTargetModel(ModelBuilder modelBuilder)
		{
#pragma warning disable 612, 618
			modelBuilder.HasAnnotation("ProductVersion", "8.0.13");

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<ushort?>("ChannelLimit")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<string>("ConnectionString")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<bool?>("Enabled")
					.HasColumnType("INTEGER");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<int>("Provider")
					.HasColumnType("INTEGER");

				b.Property<uint?>("ReconnectionInterval")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("InstanceId", "Name")
					.IsUnique();

				b.ToTable("ChatBots");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<long>("ChatSettingsId")
					.HasColumnType("INTEGER");

				b.Property<ulong?>("DiscordChannelId")
					.HasColumnType("INTEGER");

				b.Property<string>("IrcChannel")
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<bool?>("IsAdminChannel")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("IsSystemChannel")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("IsUpdatesChannel")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("IsWatchdogChannel")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<string>("Tag")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("ChatSettingsId", "DiscordChannelId")
					.IsUnique();

				b.HasIndex("ChatSettingsId", "IrcChannel")
					.IsUnique();

				b.ToTable("ChatChannels");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<int?>("DMApiMajorVersion")
					.HasColumnType("INTEGER");

				b.Property<int?>("DMApiMinorVersion")
					.HasColumnType("INTEGER");

				b.Property<int?>("DMApiPatchVersion")
					.HasColumnType("INTEGER");

				b.Property<Guid?>("DirectoryName")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("DmeName")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("EngineVersion")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<long?>("GitHubDeploymentId")
					.HasColumnType("INTEGER");

				b.Property<long?>("GitHubRepoId")
					.HasColumnType("INTEGER");

				b.Property<long>("JobId")
					.HasColumnType("INTEGER");

				b.Property<int?>("MinimumSecurityLevel")
					.HasColumnType("INTEGER");

				b.Property<string>("Output")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("RepositoryOrigin")
					.HasColumnType("TEXT");

				b.Property<long>("RevisionInformationId")
					.HasColumnType("INTEGER");

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
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("AdditionalParameters")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<bool?>("AllowWebClient")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("AutoStart")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("DumpOnHealthCheckRestart")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<uint?>("HealthCheckSeconds")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<bool?>("LogOutput")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<uint?>("MapThreads")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("Minidumps")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<ushort?>("OpenDreamTopicPort")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<ushort?>("Port")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<int>("SecurityLevel")
					.HasColumnType("INTEGER");

				b.Property<bool?>("StartProfiler")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<uint?>("StartupTimeout")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<uint?>("TopicRequestTimeout")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<int>("Visibility")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamDaemonSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<ushort?>("ApiValidationPort")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<int>("ApiValidationSecurityLevel")
					.HasColumnType("INTEGER");

				b.Property<string>("CompilerAdditionalArguments")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<int>("DMApiValidationMode")
					.HasColumnType("INTEGER");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<string>("ProjectName")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<TimeSpan?>("Timeout")
					.IsRequired()
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamMakerSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Instance", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("AutoStartCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("TEXT");

				b.Property<string>("AutoStopCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("TEXT");

				b.Property<string>("AutoUpdateCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("TEXT");

				b.Property<uint?>("AutoUpdateInterval")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<ushort?>("ChatBotLimit")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<int>("ConfigurationType")
					.HasColumnType("INTEGER");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<bool?>("Online")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<string>("Path")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("SwarmIdentifer")
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("Path", "SwarmIdentifer")
					.IsUnique();

				b.ToTable("Instances");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstancePermissionSet", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<ulong>("ChatBotRights")
					.HasColumnType("INTEGER");

				b.Property<ulong>("ConfigurationRights")
					.HasColumnType("INTEGER");

				b.Property<ulong>("DreamDaemonRights")
					.HasColumnType("INTEGER");

				b.Property<ulong>("DreamMakerRights")
					.HasColumnType("INTEGER");

				b.Property<ulong>("EngineRights")
					.HasColumnType("INTEGER");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<ulong>("InstancePermissionSetRights")
					.HasColumnType("INTEGER");

				b.Property<long>("PermissionSetId")
					.HasColumnType("INTEGER");

				b.Property<ulong>("RepositoryRights")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("InstanceId");

				b.HasIndex("PermissionSetId", "InstanceId")
					.IsUnique();

				b.ToTable("InstancePermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<ulong?>("CancelRight")
					.HasColumnType("INTEGER");

				b.Property<ulong?>("CancelRightsType")
					.HasColumnType("INTEGER");

				b.Property<bool?>("Cancelled")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<long?>("CancelledById")
					.HasColumnType("INTEGER");

				b.Property<string>("Description")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<uint?>("ErrorCode")
					.HasColumnType("INTEGER");

				b.Property<string>("ExceptionDetails")
					.HasColumnType("TEXT");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<byte>("JobCode")
					.HasColumnType("INTEGER");

				b.Property<DateTimeOffset?>("StartedAt")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<long>("StartedById")
					.HasColumnType("INTEGER");

				b.Property<DateTimeOffset?>("StoppedAt")
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("CancelledById");

				b.HasIndex("InstanceId");

				b.HasIndex("StartedById");

				b.ToTable("Jobs");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OAuthConnection", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("ExternalUserId")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<int>("Provider")
					.HasColumnType("INTEGER");

				b.Property<long>("UserId")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("UserId");

				b.HasIndex("Provider", "ExternalUserId")
					.IsUnique();

				b.ToTable("OAuthConnections");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OidcConnection", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("ExternalUserId")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<string>("SchemeKey")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<long>("UserId")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("UserId");

				b.HasIndex("SchemeKey", "ExternalUserId")
					.IsUnique();

				b.ToTable("OidcConnections");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.PermissionSet", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<ulong>("AdministrationRights")
					.HasColumnType("INTEGER");

				b.Property<long?>("GroupId")
					.HasColumnType("INTEGER");

				b.Property<ulong>("InstanceManagerRights")
					.HasColumnType("INTEGER");

				b.Property<long?>("UserId")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("GroupId")
					.IsUnique();

				b.HasIndex("UserId")
					.IsUnique();

				b.ToTable("PermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("AccessIdentifier")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<long>("CompileJobId")
					.HasColumnType("INTEGER");

				b.Property<long?>("InitialCompileJobId")
					.HasColumnType("INTEGER");

				b.Property<int>("LaunchSecurityLevel")
					.HasColumnType("INTEGER");

				b.Property<int>("LaunchVisibility")
					.HasColumnType("INTEGER");

				b.Property<ushort>("Port")
					.HasColumnType("INTEGER");

				b.Property<int>("ProcessId")
					.HasColumnType("INTEGER");

				b.Property<int>("RebootState")
					.HasColumnType("INTEGER");

				b.Property<ushort?>("TopicPort")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("CompileJobId");

				b.HasIndex("InitialCompileJobId");

				b.ToTable("ReattachInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("AccessToken")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<string>("AccessUser")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<bool?>("AutoUpdatesKeepTestMerges")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("AutoUpdatesSynchronize")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<string>("CommitterEmail")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<string>("CommitterName")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<bool?>("CreateGitHubDeployments")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<bool?>("PostTestMergeComment")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("PushTestMergeCommits")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("ShowTestMergeCommitters")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<bool?>("UpdateSubmodules")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("RepositorySettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<long>("RevisionInformationId")
					.HasColumnType("INTEGER");

				b.Property<long>("TestMergeId")
					.HasColumnType("INTEGER");

				b.HasKey("Id");

				b.HasIndex("RevisionInformationId");

				b.HasIndex("TestMergeId");

				b.ToTable("RevInfoTestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("CommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("TEXT");

				b.Property<long>("InstanceId")
					.HasColumnType("INTEGER");

				b.Property<string>("OriginCommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("TEXT");

				b.Property<DateTimeOffset>("Timestamp")
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("InstanceId", "CommitSha")
					.IsUnique();

				b.ToTable("RevisionInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("Author")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("BodyAtMerge")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("Comment")
					.HasMaxLength(10000)
					.HasColumnType("TEXT");

				b.Property<DateTimeOffset>("MergedAt")
					.HasColumnType("TEXT");

				b.Property<long>("MergedById")
					.HasColumnType("INTEGER");

				b.Property<int>("Number")
					.HasColumnType("INTEGER");

				b.Property<long?>("PrimaryRevisionInformationId")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<string>("TargetCommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("TEXT");

				b.Property<string>("TitleAtMerge")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<string>("Url")
					.IsRequired()
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("MergedById");

				b.HasIndex("PrimaryRevisionInformationId")
					.IsUnique();

				b.ToTable("TestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("CanonicalName")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<DateTimeOffset?>("CreatedAt")
					.IsRequired()
					.HasColumnType("TEXT");

				b.Property<long?>("CreatedById")
					.HasColumnType("INTEGER");

				b.Property<bool?>("Enabled")
					.IsRequired()
					.HasColumnType("INTEGER");

				b.Property<long?>("GroupId")
					.HasColumnType("INTEGER");

				b.Property<DateTimeOffset?>("LastPasswordUpdate")
					.HasColumnType("TEXT");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.Property<string>("PasswordHash")
					.HasColumnType("TEXT");

				b.Property<string>("SystemIdentifier")
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("CanonicalName")
					.IsUnique();

				b.HasIndex("CreatedById");

				b.HasIndex("GroupId");

				b.HasIndex("SystemIdentifier")
					.IsUnique();

				b.ToTable("Users");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.UserGroup", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("INTEGER");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("TEXT");

				b.HasKey("Id");

				b.HasIndex("Name")
					.IsUnique();

				b.ToTable("Groups");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("ChatSettings")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.ChatBot", "ChatSettings")
					.WithMany("Channels")
					.HasForeignKey("ChatSettingsId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("ChatSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Job", "Job")
					.WithOne()
					.HasForeignKey("Tgstation.Server.Host.Models.CompileJob", "JobId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
					.WithMany("CompileJobs")
					.HasForeignKey("RevisionInformationId")
					.OnDelete(DeleteBehavior.ClientNoAction)
					.IsRequired();

				b.Navigation("Job");

				b.Navigation("RevisionInformation");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamDaemonSettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("DreamDaemonSettings")
					.HasForeignKey("Tgstation.Server.Host.Models.DreamDaemonSettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("DreamMakerSettings")
					.HasForeignKey("Tgstation.Server.Host.Models.DreamMakerSettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstancePermissionSet", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("InstancePermissionSets")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.PermissionSet", "PermissionSet")
					.WithMany("InstancePermissionSets")
					.HasForeignKey("PermissionSetId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");

				b.Navigation("PermissionSet");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "CancelledBy")
					.WithMany()
					.HasForeignKey("CancelledById");

				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("Jobs")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.User", "StartedBy")
					.WithMany()
					.HasForeignKey("StartedById")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("CancelledBy");

				b.Navigation("Instance");

				b.Navigation("StartedBy");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OAuthConnection", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "User")
					.WithMany("OAuthConnections")
					.HasForeignKey("UserId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("User");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OidcConnection", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "User")
					.WithMany("OidcConnections")
					.HasForeignKey("UserId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("User");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.PermissionSet", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.UserGroup", "Group")
					.WithOne("PermissionSet")
					.HasForeignKey("Tgstation.Server.Host.Models.PermissionSet", "GroupId")
					.OnDelete(DeleteBehavior.Cascade);

				b.HasOne("Tgstation.Server.Host.Models.User", "User")
					.WithOne("PermissionSet")
					.HasForeignKey("Tgstation.Server.Host.Models.PermissionSet", "UserId")
					.OnDelete(DeleteBehavior.Cascade);

				b.Navigation("Group");

				b.Navigation("User");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.CompileJob", "CompileJob")
					.WithMany()
					.HasForeignKey("CompileJobId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.CompileJob", "InitialCompileJob")
					.WithMany()
					.HasForeignKey("InitialCompileJobId");

				b.Navigation("CompileJob");

				b.Navigation("InitialCompileJob");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("RepositorySettings")
					.HasForeignKey("Tgstation.Server.Host.Models.RepositorySettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
					.WithMany("ActiveTestMerges")
					.HasForeignKey("RevisionInformationId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.TestMerge", "TestMerge")
					.WithMany("RevisonInformations")
					.HasForeignKey("TestMergeId")
					.OnDelete(DeleteBehavior.ClientNoAction)
					.IsRequired();

				b.Navigation("RevisionInformation");

				b.Navigation("TestMerge");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("RevisionInformations")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("Instance");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "MergedBy")
					.WithMany("TestMerges")
					.HasForeignKey("MergedById")
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "PrimaryRevisionInformation")
					.WithOne("PrimaryTestMerge")
					.HasForeignKey("Tgstation.Server.Host.Models.TestMerge", "PrimaryRevisionInformationId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.Navigation("MergedBy");

				b.Navigation("PrimaryRevisionInformation");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "CreatedBy")
					.WithMany("CreatedUsers")
					.HasForeignKey("CreatedById");

				b.HasOne("Tgstation.Server.Host.Models.UserGroup", "Group")
					.WithMany("Users")
					.HasForeignKey("GroupId");

				b.Navigation("CreatedBy");

				b.Navigation("Group");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.Navigation("Channels");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Instance", b =>
			{
				b.Navigation("ChatSettings");

				b.Navigation("DreamDaemonSettings");

				b.Navigation("DreamMakerSettings");

				b.Navigation("InstancePermissionSets");

				b.Navigation("Jobs");

				b.Navigation("RepositorySettings");

				b.Navigation("RevisionInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.PermissionSet", b =>
			{
				b.Navigation("InstancePermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.Navigation("ActiveTestMerges");

				b.Navigation("CompileJobs");

				b.Navigation("PrimaryTestMerge");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.Navigation("RevisonInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
			{
				b.Navigation("CreatedUsers");

				b.Navigation("OAuthConnections");

				b.Navigation("OidcConnections");

				b.Navigation("PermissionSet");

				b.Navigation("TestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.UserGroup", b =>
			{
				b.Navigation("PermissionSet")
					.IsRequired();

				b.Navigation("Users");
			});
#pragma warning restore 612, 618
		}
	}
}
