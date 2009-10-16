//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

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
