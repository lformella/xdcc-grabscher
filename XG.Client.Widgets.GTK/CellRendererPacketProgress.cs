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
using Gdk;
using Gtk;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	public class CellRendererPacketProgress : CellRenderer
	{
		private XGObject obj;
		public XGObject Object
		{
			get { return obj; }
			set { obj = value; }
		}

		private string text;
		public string Text
		{
			get { return text; }
			set { text = value; }
		}

		public override void GetSize(Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			int calc_width = cell_area.Width;//(int)this.Xpad * 2 + 100;
			int calc_height = cell_area.Height;//(int)this.Ypad * 2 + 10;

			width = calc_width;
			height = calc_height;

			x_offset = 0;
			y_offset = 0;
			if (!cell_area.Equals(Rectangle.Zero))
			{
				x_offset = (int)(this.Xalign * (cell_area.Width - calc_width));
				x_offset = Math.Max(x_offset, 0);

				y_offset = (int)(this.Yalign * (cell_area.Height - calc_height));
				y_offset = Math.Max(y_offset, 0);
			}
		}

		protected override void Render(Drawable window, Widget widget, Rectangle background_area, Rectangle cell_area, Rectangle expose_area, CellRendererState flags)
		{
			SegmentedBar bar = new SegmentedBar();
			bar.BarHeight = cell_area.Height / 2 + 4;

			if (this.obj != null)
			{
				if (this.obj.GetType() == typeof(XGFilePart) && this.obj.Parent != null)
				{
					XGFilePart part = obj as XGFilePart;
					double pos_1 = (double)((double)part.StartSize / (double)part.Parent.Size);
					bar.AddSegment(pos_1, bar.RemainderColor);
					this.RenderPart(part, bar);
					bar.AddSegment(1, bar.RemainderColor);
				}

				if (this.obj.GetType() == typeof(XGFile))
				{
					XGFile file = obj as XGFile;
					foreach (XGFilePart part in file.Children)
					{
						this.RenderPart(part, bar);
					}
				}
			}
#if !DEBUG
			try {
#endif
				bar.Draw(window, widget, cell_area, text);
#if !DEBUG
			} catch (Exception) { }
#endif
		}

		private void RenderPart(XGFilePart aPart, SegmentedBar aBar)
		{
			double pos_1 = (double)((double)(aPart.CurrentSize - aPart.StartSize) / (double)aPart.Parent.Size);
			double pos_2 = (double)((double)(aPart.StopSize - aPart.CurrentSize) / (double)aPart.Parent.Size);

			if (aPart.PartState == FilePartState.Ready)
			{
				aBar.AddSegmentRgb(pos_1, (uint)(aPart.IsChecked ? 0x8ae234 : 0x4e9a06));
			}
			if (aPart.PartState == FilePartState.Broken)
			{
				aBar.AddSegmentRgb(pos_1, 0xa40000);
				aBar.AddSegmentRgb(pos_2, 0xef2929);
			}
			if (aPart.PartState == FilePartState.Closed)
			{
				aBar.AddSegmentRgb(pos_1, 0x555753);
				aBar.AddSegmentRgb(pos_2, 0xbabdb6);
			}
			if (aPart.PartState == FilePartState.Open)
			{
				aBar.AddSegmentRgb(pos_1, (uint)(aPart.IsChecked ? 0x204a87 : 0x5c3566));
				aBar.AddSegmentRgb(pos_2, (uint)(aPart.IsChecked ? 0x729fcf : 0xad7fa8));
			}
		}
	}
}
