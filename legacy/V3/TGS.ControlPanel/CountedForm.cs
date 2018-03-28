using System.Windows.Forms;

namespace TGS.ControlPanel
{
	/// <summary>
	/// Calls <see cref="Application.Exit()"/> when all <see cref="CountedForm"/>s are <see cref="Form.Close"/>d
	/// </summary>
#if !DEBUG
	abstract
#endif
	class CountedForm : ServerOpForm
	{
		/// <summary>
		/// The current number of active <see cref="CountedForm"/>s
		/// </summary>
		static uint FormCount;

		/// <summary>
		/// Construct a <see cref="CountedForm"/>. Increments <see cref="FormCount"/>
		/// </summary>
		public CountedForm()
		{
			FormClosed += CountedForm_FormClosed;
			++FormCount;
		}

		/// <summary>
		/// Decrements <see cref="FormCount"/>. Calls <see cref="Application.Exit()"/> if it reaches 0
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="FormClosedEventArgs"/></param>
		private void CountedForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (--FormCount == 0)
				Application.Exit();
		}
	}
}
