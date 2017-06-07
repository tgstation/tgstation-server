﻿using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	public partial class TGServerService : ServiceBase
	{
		static TGServerService ActiveService;	//So everyone else can write to our eventlog

		public static void WriteLog(string message, EventLogEntryType type = EventLogEntryType.Information)
		{
			ActiveService.EventLog.WriteEntry(message, type);
		}

		public static void LocalStop()
		{
			ThreadPool.QueueUserWorkItem(_ => { ActiveService.Stop(); });
		}

		ServiceHost host;	//the WCF host
		
		//you should seriously not add anything here
		//Use OnStart instead
		public TGServerService()
		{
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					Properties.Settings.Default.Upgrade();
					Properties.Settings.Default.UpgradeRequired = false;
					Properties.Settings.Default.Save();
				}
				InitializeComponent();
				ActiveService = this;
				Run(this);
			}
			finally
			{
				Properties.Settings.Default.Save();
			}
		}

		//when babby is formed
		protected override void OnStart(string[] args)
		{
			var Config = Properties.Settings.Default;
			if (!Directory.Exists(Config.ServerDirectory))
			{
				EventLog.WriteEntry("Creating server directory: " + Config.ServerDirectory);
				Directory.CreateDirectory(Config.ServerDirectory);
			}
			Environment.CurrentDirectory = Config.ServerDirectory;

			host = new ServiceHost(typeof(TGStationServer), new Uri[] { new Uri("net.pipe://localhost") })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			}; //construction runs here

			foreach (var I in Server.ValidInterfaces)
				AddEndpoint(I);

			host.Open();    //...or maybe here, doesn't really matter
		}

		//shorthand for adding the WCF endpoint
		void AddEndpoint(Type typetype)
		{
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding(), Server.MasterPipeName + "/" + typetype.Name);
		}

		//when we is kill
		protected override void OnStop()
		{
			try
			{
				host.Close();   //where TGStationServer.Dispose() is called
				host = null;
			}
			catch { }
		}
	}
}
