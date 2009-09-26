using System;

namespace XG.Client.Widgets.GTK
{
	public static class WidgetHelper
	{
		public static string Size2Human(long aSize)
		{
			if (aSize == 0) { return ""; }
			if (aSize < 1024) { return aSize + " B"; }
			else if (aSize < 1024 * 1024) { return (aSize / 1024).ToString() + " KB"; }
			else if (aSize < 1024 * 1024 * 1024) { return (aSize / (1024 * 1024)).ToString() + " MB"; }
			else { return (aSize / (1024 * 1024 * 1024)).ToString() + " GB"; }
		}
		
		public static string Speed2Human(double aSpeed)
		{
			if (aSpeed == 0) { return ""; }
			if (aSpeed < 1024) { return aSpeed.ToString("0.00") + " B"; }
			else if (aSpeed < 1024 * 1024) { return (aSpeed / 1024).ToString("0.00") + " KB"; }
			else { return (aSpeed / (1024 * 1024)).ToString("0.00") + " MB"; }
		}
		
		public static string Time2Human(Int64 aTime)
		{
			string str = "";
			if(aTime == Int64.MaxValue) { return str; }
		
			int buff = 0;
		
			if (aTime > 86400)
			{
				buff = (int)(aTime / 86400);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 86400;
			}
			//else { str += "00:"; }
		
			if (aTime > 3600)
			{
				buff = (int)(aTime / 3600);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 3600;
			}
			//else { str += "00:"; }
		
			if (aTime > 60)
			{
				buff = (int)(aTime / 60);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 60;
			}
			//else { str += "00:"; }
		
			if (aTime > 0)
			{
				buff = (int)aTime;
				str += buff >= 10 ? "" + buff : "0" + buff;
				aTime -= buff;
			}
			else { str += "00"; }
		
			return str;
		}
	}
}
