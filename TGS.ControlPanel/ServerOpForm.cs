using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGS.ControlPanel
{
	/// <summary>
	/// Used to provide an ATP function for calls into an <see cref="TGS.Interface.IClient"/>
	/// </summary>
#if !DEBUG
	abstract
#endif
	class ServerOpForm : Form
	{
		/// <summary>
		/// Used to wrap <see cref="TGS.Interface.IClient"/> calls in a non-blocking fashion while disabling the <see cref="Form"/> and enabling the wait cursor
		/// </summary>
		/// <param name="action">The <see cref="TGS.Interface.IClient"/> operation to wrap</param>
		/// <returns>A <see cref="Task"/> wrapping <paramref name="action"/></returns>
		protected Task WrapServerOp(Action action)
		{
			Enabled = false;
			UseWaitCursor = true;
			try
			{
				return Task.Run(action);
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
			}
		}
	}
}
