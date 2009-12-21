// This file is taken from banshee http://banshee-project.org/
// Simplified and modified for XG by Lars Formella <ich@larsformella.de>
// 
// Original copyright: 
//
// SegmentedBar.cs
//
// Author:
//	Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Cairo;
using Gdk;
using Gtk;

namespace XG.Client.Widgets.GTK
{
	[Flags]
	public enum CairoCorners
	{
		None = 0,
		TopLeft = 1,
		TopRight = 2,
		BottomLeft = 4,
		BottomRight = 8,
		All = 15
	}

	public static class CairoExtensions
	{

		public static Cairo.Color RgbToColor(uint rgbColor)
		{
			return RgbaToColor((rgbColor << 8) | 0x000000ff);
		}

		public static Cairo.Color RgbaToColor(uint rgbaColor)
		{
			return new Cairo.Color(
				(byte)(rgbaColor >> 24) / 255.0,
				(byte)(rgbaColor >> 16) / 255.0,
				(byte)(rgbaColor >> 8) / 255.0,
				(byte)(rgbaColor & 0x000000ff) / 255.0);
		}

		public static void HsbFromColor(Cairo.Color color, out double hue, out double saturation, out double brightness)
		{
			double min, max, delta;
			double red = color.R;
			double green = color.G;
			double blue = color.B;

			hue = 0;
			saturation = 0;
			brightness = 0;

			if (red > green)
			{
				max = Math.Max(red, blue);
				min = Math.Min(green, blue);
			}
			else
			{
				max = Math.Max(green, blue);
				min = Math.Min(red, blue);
			}

			brightness = (max + min) / 2;

			if (Math.Abs(max - min) < 0.0001)
			{
				hue = 0;
				saturation = 0;
			}
			else
			{
				saturation = brightness <= 0.5 ? (max - min) / (max + min) : (max - min) / (2 - max - min);
				delta = max - min;

				if (red == max)
				{
					hue = (green - blue) / delta;
				}
				else if (green == max)
				{
					hue = 2 + (blue - red) / delta;
				}
				else if (blue == max)
				{
					hue = 4 + (red - green) / delta;
				}

				hue *= 60;
				if (hue < 0)
				{
					hue += 360;
				}
			}
		}

		private static double Modula(double number, double divisor)
		{
			return ((int)number % divisor) + (number - (int)number);
		}

		public static Cairo.Color ColorFromHsb(double hue, double saturation, double brightness)
		{
			int i;
			double[] hue_shift = { 0, 0, 0 };
			double[] color_shift = { 0, 0, 0 };
			double m1, m2, m3;

			m2 = brightness <= 0.5 ? brightness * (1 + saturation) : brightness + saturation - brightness * saturation;
			m1 = 2 * brightness - m2;

			hue_shift[0] = hue + 120;
			hue_shift[1] = hue;
			hue_shift[2] = hue - 120;

			color_shift[0] = color_shift[1] = color_shift[2] = brightness;

			i = saturation == 0 ? 3 : 0;

			for (; i < 3; i++)
			{
				m3 = hue_shift[i];

				if (m3 > 360)
				{
					m3 = Modula(m3, 360);
				}
				else if (m3 < 0)
				{
					m3 = 360 - Modula(Math.Abs(m3), 360);
				}

				if (m3 < 60)
				{
					color_shift[i] = m1 + (m2 - m1) * m3 / 60;
				}
				else if (m3 < 180)
				{
					color_shift[i] = m2;
				}
				else if (m3 < 240)
				{
					color_shift[i] = m1 + (m2 - m1) * (240 - m3) / 60;
				}
				else
				{
					color_shift[i] = m1;
				}
			}

			return new Cairo.Color(color_shift[0], color_shift[1], color_shift[2]);
		}

		public static Cairo.Color ColorShade(Cairo.Color @base, double ratio)
		{
			double h, s, b;

			HsbFromColor(@base, out h, out s, out b);

			b = Math.Max(Math.Min(b * ratio, 1), 0);
			s = Math.Max(Math.Min(s * ratio, 1), 0);

			Cairo.Color color = ColorFromHsb(h, s, b);
			color.A = @base.A;
			return color;
		}

		public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h, double r)
		{
			RoundedRectangle(cr, x, y, w, h, r, CairoCorners.All, false);
		}

		public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h,
			double r, CairoCorners corners)
		{
			RoundedRectangle(cr, x, y, w, h, r, corners, false);
		}

		public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h,
			double r, CairoCorners corners, bool topBottomFallsThrough)
		{
			if (topBottomFallsThrough && corners == CairoCorners.None)
			{
				cr.MoveTo(x, y - r);
				cr.LineTo(x, y + h + r);
				cr.MoveTo(x + w, y - r);
				cr.LineTo(x + w, y + h + r);
				return;
			}
			else if (r < 0.0001 || corners == CairoCorners.None)
			{
				cr.Rectangle(x, y, w, h);
				return;
			}

			if ((corners & (CairoCorners.TopLeft | CairoCorners.TopRight)) == 0 && topBottomFallsThrough)
			{
				y -= r;
				h += r;
				cr.MoveTo(x + w, y);
			}
			else
			{
				if ((corners & CairoCorners.TopLeft) != 0)
				{
					cr.MoveTo(x + r, y);
				}
				else
				{
					cr.MoveTo(x, y);
				}

				if ((corners & CairoCorners.TopRight) != 0)
				{
					cr.Arc(x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
				}
				else
				{
					cr.LineTo(x + w, y);
				}
			}

			if ((corners & (CairoCorners.BottomLeft | CairoCorners.BottomRight)) == 0 && topBottomFallsThrough)
			{
				h += r;
				cr.LineTo(x + w, y + h);
				cr.MoveTo(x, y + h);
				cr.LineTo(x, y + r);
				cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
			}
			else
			{
				if ((corners & CairoCorners.BottomRight) != 0)
				{
					cr.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
				}
				else
				{
					cr.LineTo(x + w, y + h);
				}

				if ((corners & CairoCorners.BottomLeft) != 0)
				{
					cr.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
				}
				else
				{
					cr.LineTo(x, y + h);
				}

				if ((corners & CairoCorners.TopLeft) != 0)
				{
					cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
				}
				else
				{
					cr.LineTo(x, y);
				}
			}
		}

		public static void DisposeContext(Cairo.Context cr)
		{
			((IDisposable)cr.Target).Dispose();
			((IDisposable)cr).Dispose();
		}

		private struct CairoInteropCall
		{
			public string Name;
			public MethodInfo ManagedMethod;
			public bool CallNative;

			public CairoInteropCall(string name)
			{
				Name = name;
				ManagedMethod = null;
				CallNative = false;
			}
		}

		private static bool CallCairoMethod(Cairo.Context cr, ref CairoInteropCall call)
		{
			if (call.ManagedMethod == null && !call.CallNative)
			{
				MemberInfo[] members = typeof(Cairo.Context).GetMember(call.Name, MemberTypes.Method,
					BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public);

				if (members != null && members.Length > 0 && members[0] is MethodInfo)
				{
					call.ManagedMethod = (MethodInfo)members[0];
				}
				else
				{
					call.CallNative = true;
				}
			}

			if (call.ManagedMethod != null)
			{
				call.ManagedMethod.Invoke(cr, null);
				return true;
			}

			return false;
		}

		private static bool native_push_pop_exists = true;

		[DllImport("libcairo-2.dll")]
		private static extern void cairo_push_group(IntPtr ptr);
		private static CairoInteropCall cairo_push_group_call = new CairoInteropCall("PushGroup");

		public static void PushGroup(Cairo.Context cr)
		{
			if (!native_push_pop_exists)
			{
				return;
			}

			try
			{
				if (!CallCairoMethod(cr, ref cairo_push_group_call))
				{
					cairo_push_group(cr.Handle);
				}
			}
			catch
			{
				native_push_pop_exists = false;
			}
		}

		[DllImport("libcairo-2.dll")]
		private static extern void cairo_pop_group_to_source(IntPtr ptr);
		private static CairoInteropCall cairo_pop_group_to_source_call = new CairoInteropCall("PopGroupToSource");

		public static void PopGroupToSource(Cairo.Context cr)
		{
			if (!native_push_pop_exists)
			{
				return;
			}

			try
			{
				if (!CallCairoMethod(cr, ref cairo_pop_group_to_source_call))
				{
					cairo_pop_group_to_source(cr.Handle);
				}
			}
			catch (EntryPointNotFoundException)
			{
				native_push_pop_exists = false;
			}
		}
	}

	public class SegmentedBar
	{
		public delegate string BarValueFormatHandler(Segment segment);

		public class Segment
		{
			private double percent;
			private Cairo.Color color;

			public Segment(double percent, Cairo.Color color)
			{
				this.percent = percent;
				this.color = color;
			}

			public double Percent
			{
				get { return percent; }
				set { percent = value; }
			}

			public Cairo.Color Color
			{
				get { return color; }
				set { color = value; }
			}
		}

		// State
		private List<Segment> segments = new List<Segment>();

		// Properties
		private int bar_height = 26;
		private Cairo.Color remainder_color = CairoExtensions.RgbToColor(0xeeeeee);

		public SegmentedBar()
		{
		}

		#region Public Methods

		public void AddSegmentRgba(double percent, uint rgbaColor)
		{
			AddSegment(percent, CairoExtensions.RgbaToColor(rgbaColor));
		}

		public void AddSegmentRgb(double percent, uint rgbColor)
		{
			AddSegment(percent, CairoExtensions.RgbToColor(rgbColor));
		}

		public void AddSegment(double percent, Cairo.Color color)
		{
			AddSegment(new Segment(percent, color));
		}

		public void AddSegment(Segment segment)
		{
			lock (segments)
			{
				segments.Add(segment);
			}
		}

		#endregion

		#region Public Properties


		public Cairo.Color RemainderColor
		{
			get { return remainder_color; }
			set { remainder_color = value; }
		}

		public int BarHeight
		{
			get { return bar_height; }
			set
			{
				if (bar_height != value)
				{
					bar_height = value;
				}
			}
		}

		#endregion

		#region Rendering

		public void Draw(Drawable window, Widget widget, Gdk.Rectangle rec, string text)
		{
			Cairo.Context cr = Gdk.CairoHelper.Create(window);

			cr.Operator = Operator.Over;
			cr.Translate(rec.X, rec.Y);
			cr.Rectangle(0, 0, rec.Width, rec.Height);
			cr.Clip();

			if (text != null & text != "")
			{
				int lw, lh;
				Pango.Layout layout = new Pango.Layout(widget.PangoContext);
				layout.FontDescription = widget.Style.FontDescription;
				layout.SetText(text);
				layout.GetPixelSize(out lw, out lh);

				if (this.segments.Count == 0)
				{
					cr.Translate(0, (rec.Height - lh) / 2);
				}

				window.DrawLayout(widget.Style.TextGC(StateType.Normal), rec.X, (this.segments.Count == 0) ? rec.Y + (rec.Height - lh) / 2 : rec.Y, layout);
				layout.Dispose();

				cr.Translate(0, lh);
				bar_height = rec.Height - lh;
			}
			else
			{
				cr.Translate(0, (rec.Height - bar_height) / 2);
			}

			if (this.segments.Count > 0)
			{
				Pattern bar = RenderBar(rec.Width, bar_height);

				cr.Save();
				cr.Source = bar;
				cr.Paint();
				cr.Restore();

				bar.Destroy();
			}

			((IDisposable)cr.Target).Dispose();
			((IDisposable)cr).Dispose();
		}

		private Pattern RenderBar(int w, int h)
		{
			ImageSurface s = new ImageSurface(Format.Argb32, w, h);
			Context cr = new Context(s);
			RenderBar(cr, w, h, h / 2);
			Pattern pattern = new SurfacePattern(s);
			s.Destroy();
			((IDisposable)cr).Dispose();
			return pattern;
		}

		private void RenderBar(Context cr, int w, int h, int r)
		{
			RenderBarSegments(cr, w, h, r);
			RenderBarStrokes(cr, w, h, r);
		}

		private void RenderBarSegments(Context cr, int w, int h, int r)
		{
			LinearGradient grad = new LinearGradient(0, 0, w, 0);
			double last = 0.0;

			foreach (Segment segment in segments)
			{
				if (segment.Percent > 0)
				{
					grad.AddColorStop(last, segment.Color);
					grad.AddColorStop(last += segment.Percent, segment.Color);
				}
			}

			CairoExtensions.RoundedRectangle(cr, 0, 0, w, h, r);
			cr.Pattern = grad;
			cr.FillPreserve();
			cr.Pattern.Destroy();

			grad = new LinearGradient(0, 0, 0, h);
			grad.AddColorStop(0.0, new Cairo.Color(1, 1, 1, 0.125));
			grad.AddColorStop(0.35, new Cairo.Color(1, 1, 1, 0.255));
			grad.AddColorStop(1, new Cairo.Color(0, 0, 0, 0.4));

			cr.Pattern = grad;
			cr.Fill();
			cr.Pattern.Destroy();
		}

		private void RenderBarStrokes(Context cr, int w, int h, int r)
		{
			LinearGradient stroke = MakeSegmentGradient(h, CairoExtensions.RgbaToColor(0x00000040));
			LinearGradient seg_sep_light = MakeSegmentGradient(h, CairoExtensions.RgbaToColor(0xffffff20));
			LinearGradient seg_sep_dark = MakeSegmentGradient(h, CairoExtensions.RgbaToColor(0x00000020));

			cr.LineWidth = 1;

			double seg_w = 20;
			double x = seg_w > r ? seg_w : r;

			while (x <= w - r)
			{
				cr.MoveTo(x - 0.5, 1);
				cr.LineTo(x - 0.5, h - 1);
				cr.Pattern = seg_sep_light;
				cr.Stroke();

				cr.MoveTo(x + 0.5, 1);
				cr.LineTo(x + 0.5, h - 1);
				cr.Pattern = seg_sep_dark;
				cr.Stroke();

				x += seg_w;
			}

			CairoExtensions.RoundedRectangle(cr, 0.5, 0.5, w - 1, h - 1, r);
			cr.Pattern = stroke;
			cr.Stroke();

			stroke.Destroy();
			seg_sep_light.Destroy();
			seg_sep_dark.Destroy();
		}

		private LinearGradient MakeSegmentGradient(int h, Cairo.Color color)
		{
			return MakeSegmentGradient(h, color, false);
		}

		private LinearGradient MakeSegmentGradient(int h, Cairo.Color color, bool diag)
		{
			LinearGradient grad = new LinearGradient(0, 0, 0, h);
			grad.AddColorStop(0, CairoExtensions.ColorShade(color, 1.1));
			grad.AddColorStop(0.35, CairoExtensions.ColorShade(color, 1.2));
			grad.AddColorStop(1, CairoExtensions.ColorShade(color, 0.8));
			return grad;
		}

		#endregion
	}
}
