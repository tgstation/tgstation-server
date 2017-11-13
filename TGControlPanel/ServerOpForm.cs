using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGControlPanel
{
	/// <summary>
	/// Used to provide an ATP function for calls into an <see cref="TGServiceInterface.IServerInterface"/>
	/// </summary>
#if !DEBUG
	abstract class ServerOpForm : Form
#else
	class ServerOpForm : Form
#endif
	{
		/// <summary>
		/// Used to wrap <see cref="TGServiceInterface.IServerInterface"/> calls in a non-blocking fashion while disabling the <see cref="Form"/> and enabling the wait cursor
		/// </summary>
		/// <param name="action">The <see cref="TGServiceInterface.IServerInterface"/> operation to wrap</param>
		/// <returns>A <see cref="Task"/> wrapping <paramref name="action"/></returns>
		protected Task WrapServerOp(Action action)
		{
			Enabled = false;
			UseWaitCursor = true;
			try
			{
				return Task.Factory.StartNew(action);
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
			}
		}
	}
}
