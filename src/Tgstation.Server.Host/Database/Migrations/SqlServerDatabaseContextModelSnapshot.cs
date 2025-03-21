﻿// <auto-generated />
using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tgstation.Server.Host.Database
{
	[DbContext(typeof(SqlServerDatabaseContext))]
	partial class SqlServerDatabaseContextModelSnapshot : ModelSnapshot
	{
		protected override void BuildModel(ModelBuilder modelBuilder)
		{
#pragma warning disable 612, 618
			modelBuilder
				.HasAnnotation("ProductVersion", "8.0.13")
				.HasAnnotation("Relational:MaxIdentifierLength", 128);

			SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<int>("ChannelLimit")
					.HasColumnType("int");

				b.Property<string>("ConnectionString")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<bool?>("Enabled")
					.HasColumnType("bit");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<int>("Provider")
					.HasColumnType("int");

				b.Property<long>("ReconnectionInterval")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("InstanceId", "Name")
					.IsUnique();

				b.ToTable("ChatBots");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<long>("ChatSettingsId")
					.HasColumnType("bigint");

				b.Property<decimal?>("DiscordChannelId")
					.HasColumnType("decimal(20,0)");

				b.Property<string>("IrcChannel")
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<bool?>("IsAdminChannel")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("IsSystemChannel")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("IsUpdatesChannel")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("IsWatchdogChannel")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<string>("Tag")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.HasKey("Id");

				b.HasIndex("ChatSettingsId", "DiscordChannelId")
					.IsUnique()
					.HasFilter("[DiscordChannelId] IS NOT NULL");

				b.HasIndex("ChatSettingsId", "IrcChannel")
					.IsUnique()
					.HasFilter("[IrcChannel] IS NOT NULL");

				b.ToTable("ChatChannels");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<int?>("DMApiMajorVersion")
					.HasColumnType("int");

				b.Property<int?>("DMApiMinorVersion")
					.HasColumnType("int");

				b.Property<int?>("DMApiPatchVersion")
					.HasColumnType("int");

				b.Property<Guid?>("DirectoryName")
					.IsRequired()
					.HasColumnType("uniqueidentifier");

				b.Property<string>("DmeName")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<string>("EngineVersion")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<long?>("GitHubDeploymentId")
					.HasColumnType("bigint");

				b.Property<long?>("GitHubRepoId")
					.HasColumnType("bigint");

				b.Property<long>("JobId")
					.HasColumnType("bigint");

				b.Property<int?>("MinimumSecurityLevel")
					.HasColumnType("int");

				b.Property<string>("Output")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<string>("RepositoryOrigin")
					.HasColumnType("nvarchar(max)");

				b.Property<long>("RevisionInformationId")
					.HasColumnType("bigint");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("AdditionalParameters")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<bool?>("AllowWebClient")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("AutoStart")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("DumpOnHealthCheckRestart")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long>("HealthCheckSeconds")
					.HasColumnType("bigint");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<bool?>("LogOutput")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long>("MapThreads")
					.HasColumnType("bigint");

				b.Property<bool?>("Minidumps")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<int>("OpenDreamTopicPort")
					.HasColumnType("int");

				b.Property<int>("Port")
					.HasColumnType("int");

				b.Property<int>("SecurityLevel")
					.HasColumnType("int");

				b.Property<bool?>("StartProfiler")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long>("StartupTimeout")
					.HasColumnType("bigint");

				b.Property<long>("TopicRequestTimeout")
					.HasColumnType("bigint");

				b.Property<int>("Visibility")
					.HasColumnType("int");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamDaemonSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<int>("ApiValidationPort")
					.HasColumnType("int");

				b.Property<int>("ApiValidationSecurityLevel")
					.HasColumnType("int");

				b.Property<string>("CompilerAdditionalArguments")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<int>("DMApiValidationMode")
					.HasColumnType("int");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("ProjectName")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<TimeSpan?>("Timeout")
					.IsRequired()
					.HasColumnType("time");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamMakerSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Instance", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<string>("AutoStartCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("nvarchar(1000)");

				b.Property<string>("AutoStopCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("nvarchar(1000)");

				b.Property<string>("AutoUpdateCron")
					.IsRequired()
					.HasMaxLength(1000)
					.HasColumnType("nvarchar(1000)");

				b.Property<long>("AutoUpdateInterval")
					.HasColumnType("bigint");

				b.Property<int>("ChatBotLimit")
					.HasColumnType("int");

				b.Property<int>("ConfigurationType")
					.HasColumnType("int");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<bool?>("Online")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<string>("Path")
					.IsRequired()
					.HasColumnType("nvarchar(450)");

				b.Property<string>("SwarmIdentifer")
					.HasColumnType("nvarchar(450)");

				b.HasKey("Id");

				b.HasIndex("Path", "SwarmIdentifer")
					.IsUnique()
					.HasFilter("[SwarmIdentifer] IS NOT NULL");

				b.ToTable("Instances");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstancePermissionSet", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<decimal>("ChatBotRights")
					.HasColumnType("decimal(20,0)");

				b.Property<decimal>("ConfigurationRights")
					.HasColumnType("decimal(20,0)");

				b.Property<decimal>("DreamDaemonRights")
					.HasColumnType("decimal(20,0)");

				b.Property<decimal>("DreamMakerRights")
					.HasColumnType("decimal(20,0)");

				b.Property<decimal>("EngineRights")
					.HasColumnType("decimal(20,0)");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<decimal>("InstancePermissionSetRights")
					.HasColumnType("decimal(20,0)");

				b.Property<long>("PermissionSetId")
					.HasColumnType("bigint");

				b.Property<decimal>("RepositoryRights")
					.HasColumnType("decimal(20,0)");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<decimal?>("CancelRight")
					.HasColumnType("decimal(20,0)");

				b.Property<decimal?>("CancelRightsType")
					.HasColumnType("decimal(20,0)");

				b.Property<bool?>("Cancelled")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long?>("CancelledById")
					.HasColumnType("bigint");

				b.Property<string>("Description")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<long?>("ErrorCode")
					.HasColumnType("bigint");

				b.Property<string>("ExceptionDetails")
					.HasColumnType("nvarchar(max)");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<byte>("JobCode")
					.HasColumnType("tinyint");

				b.Property<DateTimeOffset?>("StartedAt")
					.IsRequired()
					.HasColumnType("datetimeoffset");

				b.Property<long>("StartedById")
					.HasColumnType("bigint");

				b.Property<DateTimeOffset?>("StoppedAt")
					.HasColumnType("datetimeoffset");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("ExternalUserId")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<int>("Provider")
					.HasColumnType("int");

				b.Property<long>("UserId")
					.HasColumnType("bigint");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("ExternalUserId")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<string>("SchemeKey")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<long>("UserId")
					.HasColumnType("bigint");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<decimal>("AdministrationRights")
					.HasColumnType("decimal(20,0)");

				b.Property<long?>("GroupId")
					.HasColumnType("bigint");

				b.Property<decimal>("InstanceManagerRights")
					.HasColumnType("decimal(20,0)");

				b.Property<long?>("UserId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("GroupId")
					.IsUnique()
					.HasFilter("[GroupId] IS NOT NULL");

				b.HasIndex("UserId")
					.IsUnique()
					.HasFilter("[UserId] IS NOT NULL");

				b.ToTable("PermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<string>("AccessIdentifier")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<long>("CompileJobId")
					.HasColumnType("bigint");

				b.Property<long?>("InitialCompileJobId")
					.HasColumnType("bigint");

				b.Property<int>("LaunchSecurityLevel")
					.HasColumnType("int");

				b.Property<int>("LaunchVisibility")
					.HasColumnType("int");

				b.Property<int>("Port")
					.HasColumnType("int");

				b.Property<int>("ProcessId")
					.HasColumnType("int");

				b.Property<int>("RebootState")
					.HasColumnType("int");

				b.Property<int?>("TopicPort")
					.HasColumnType("int");

				b.HasKey("Id");

				b.HasIndex("CompileJobId");

				b.HasIndex("InitialCompileJobId");

				b.ToTable("ReattachInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("AccessToken")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<string>("AccessUser")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<bool?>("AutoUpdatesKeepTestMerges")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("AutoUpdatesSynchronize")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<string>("CommitterEmail")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<string>("CommitterName")
					.IsRequired()
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<bool?>("CreateGitHubDeployments")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<bool?>("PostTestMergeComment")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("PushTestMergeCommits")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("ShowTestMergeCommitters")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<bool?>("UpdateSubmodules")
					.IsRequired()
					.HasColumnType("bit");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("RepositorySettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<long>("RevisionInformationId")
					.HasColumnType("bigint");

				b.Property<long>("TestMergeId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("RevisionInformationId");

				b.HasIndex("TestMergeId");

				b.ToTable("RevInfoTestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("CommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("nvarchar(40)");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("OriginCommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("nvarchar(40)");

				b.Property<DateTimeOffset>("Timestamp")
					.HasColumnType("datetimeoffset");

				b.HasKey("Id");

				b.HasIndex("InstanceId", "CommitSha")
					.IsUnique();

				b.ToTable("RevisionInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

				b.Property<string>("Author")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<string>("BodyAtMerge")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<string>("Comment")
					.HasMaxLength(10000)
					.HasColumnType("nvarchar(max)");

				b.Property<DateTimeOffset>("MergedAt")
					.HasColumnType("datetimeoffset");

				b.Property<long>("MergedById")
					.HasColumnType("bigint");

				b.Property<int>("Number")
					.HasColumnType("int");

				b.Property<long?>("PrimaryRevisionInformationId")
					.IsRequired()
					.HasColumnType("bigint");

				b.Property<string>("TargetCommitSha")
					.IsRequired()
					.HasMaxLength(40)
					.HasColumnType("nvarchar(40)");

				b.Property<string>("TitleAtMerge")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

				b.Property<string>("Url")
					.IsRequired()
					.HasColumnType("nvarchar(max)");

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
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<string>("CanonicalName")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<DateTimeOffset?>("CreatedAt")
					.IsRequired()
					.HasColumnType("datetimeoffset");

				b.Property<long?>("CreatedById")
					.HasColumnType("bigint");

				b.Property<bool?>("Enabled")
					.IsRequired()
					.HasColumnType("bit");

				b.Property<long?>("GroupId")
					.HasColumnType("bigint");

				b.Property<DateTimeOffset?>("LastPasswordUpdate")
					.HasColumnType("datetimeoffset");

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.Property<string>("PasswordHash")
					.HasColumnType("nvarchar(max)");

				b.Property<string>("SystemIdentifier")
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

				b.HasKey("Id");

				b.HasIndex("CanonicalName")
					.IsUnique();

				b.HasIndex("CreatedById");

				b.HasIndex("GroupId");

				b.HasIndex("SystemIdentifier")
					.IsUnique()
					.HasFilter("[SystemIdentifier] IS NOT NULL");

				b.ToTable("Users");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.UserGroup", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long?>("Id"));

				b.Property<string>("Name")
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnType("nvarchar(100)");

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
