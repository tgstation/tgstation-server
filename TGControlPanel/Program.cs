﻿using System;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Server.SetBadCertificateHandler(BadCertificateHandler);
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					Properties.Settings.Default.Upgrade();
					Properties.Settings.Default.UpgradeRequired = false;
					Properties.Settings.Default.Save();
				}
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				using(var L = new Login())
					Application.Run(L);
			}
			catch (Exception e)
			{
				ServiceDisconnectException(e);
			}
			finally
			{
				Properties.Settings.Default.Save();
			}
		}
		static bool SSLErrorPromptResult = false;
		static bool BadCertificateHandler(string message)
		{
			if (!SSLErrorPromptResult)
			{
				var result = MessageBox.Show(message + " IT IS HIGHLY RECCOMENDED YOU DO NOT PROCEED! Continue?", "SSL Error", MessageBoxButtons.YesNo) == DialogResult.Yes;
				SSLErrorPromptResult = result;
				return result;
			}
			return true;
		}

		public static bool CheckAdminWithWarning()
		{
			if (!Server.AuthenticateAdmin())
			{
				MessageBox.Show("Only system administrators may use this command!");
				return false;
			}
			return true;
		}

		public static void ServiceDisconnectException(Exception e)
		{
			MessageBox.Show("An unhandled exception occurred. This usually means we lost connection to the service. Error" + e.ToString());
		}

		public static string TextPrompt(string caption, string text)
		{
			Form prompt = new Form()
			{
				Width = 500,
				Height = 150,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				Text = caption,
				StartPosition = FormStartPosition.CenterScreen
			};
			Label textLabel = new Label() { Left = 50, Top = 20, Text = text, AutoSize = true };
			TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
			Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
			confirmation.Click += (sender, e) => { prompt.Close(); };
			prompt.Controls.Add(textBox);
			prompt.Controls.Add(confirmation);
			prompt.Controls.Add(textLabel);
			prompt.AcceptButton = confirmation;

			return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
		}
	}
}
