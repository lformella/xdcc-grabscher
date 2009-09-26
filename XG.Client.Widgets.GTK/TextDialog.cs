using System;

namespace XG.Client.Widgets.GTK
{
	[Flags]
	public enum DialogResult : short
	{
		OK,
		Cancel
	}

	public partial class TextDialog : Gtk.Dialog
	{
		private DialogResult result;
		public DialogResult Result
		{
			get { return result; }
			set { result = value; }
		}

		public TextDialog(string aText)
		{
			this.Build();
			this.Title = aText;
			this.lblInfo.Text = aText;
		}

		public string GetInput()
		{
			return this.entryText.Text;
		}

		public void SetInput(string aText)
		{
			this.entryText.Text = aText;
		}

		protected virtual void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.result = DialogResult.Cancel;
		}

		protected virtual void btnOk_Click(object sender, System.EventArgs e)
		{
			this.result = DialogResult.OK;
		}

		protected virtual void entryText_Activated(object sender, System.EventArgs e)
		{
			this.btnOk_Click(sender, e);
		}
	}
}
