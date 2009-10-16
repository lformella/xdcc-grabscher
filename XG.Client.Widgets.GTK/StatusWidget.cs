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

using XG.Client.GTK;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class StatusWidget : Gtk.Bin
	{
		private RootObject myRootObject;
		public RootObject RootObject
		{
			set { this.myRootObject = value; }
		}

		public StatusWidget()
		{
			this.Build();

			#region GUI FIXES

			this.imageServer.Pixbuf = ImageLoaderGTK.Instance.pbServer;
			this.imageChannel.Pixbuf = ImageLoaderGTK.Instance.pbChannel;
			this.imageBot.Pixbuf = ImageLoaderGTK.Instance.pbBot;
			this.imagePacket.Pixbuf = ImageLoaderGTK.Instance.pbPacket;
			this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL0;
			this.imageFiles.Pixbuf = ImageLoaderGTK.Instance.pbPacketReady1;

			this.infoServer.SetAlignment(0, 0.5f);
			this.infoChannel.SetAlignment(0, 0.5f);
			this.infoBot.SetAlignment(0, 0.5f);
			this.infoPacket.SetAlignment(0, 0.5f);
			this.infoSpeed.SetAlignment(0, 0.5f);
			this.infoFiles.SetAlignment(0, 0.5f);

			#endregion
		}

		#region HELPER

		public void Update()
		{
			if (this.myRootObject != null)
			{
				int countServOn = 0;
				int countServOff = 0;
				int countChanOn = 0;
				int countChanOff = 0;
				int countBotOn = 0;
				int countBotOff = 0;
				int countPackOn = 0;
				int countPackOff = 0;
				int countFileReady = 0;
				long countFileSizeReady = 0;
				double countSpeedTotal = 0;

				foreach (XGObject tObj in this.myRootObject.Children)
				{
					if(tObj.GetType() == typeof(XGServer))
					{
						XGServer tServ = tObj as XGServer;
						if (tServ.Connected) { countServOn++; }
						else { countServOff++; }
						foreach (XGChannel tChan in tServ.Children)
						{
							if (tChan.Connected) { countChanOn++; }
							else { countChanOff++; }
							foreach (XGBot tBot in tChan.Children)
							{
								if (tBot.Connected) { countBotOn++; }
								else { countBotOff++; }
								foreach (XGPacket tPack in tBot.Children)
								{
									if (tPack.Enabled) { countPackOn++; }
									else { countPackOff++; }
								}
							}
						}
					}
					if(tObj.GetType() == typeof(XGFile))
					{
						XGFile tFile = tObj as XGFile;
						foreach (XGFilePart tPart in tFile.Children)
						{
							countSpeedTotal += tPart.Speed;
						}
						//if(tFile.Enabled == false)
						//{
							countFileSizeReady += tFile.Size;
							countFileReady++;
						//}
					}
				}

				Gtk.Application.Invoke(delegate
				{
					this.infoServer.Text = countServOn + " / " + (countServOn + countServOff);
					this.infoChannel.Text = countChanOn + " / " + (countChanOn + countChanOff);
					this.infoBot.Text = countBotOn + " / " + (countBotOn + countBotOff);
					this.infoPacket.Text = countPackOn + " / " + (countPackOn + countPackOff);
					this.infoSpeed.Text = WidgetHelper.Speed2Human(countSpeedTotal);
					this.infoFiles.Text = countFileReady + " Files (" + WidgetHelper.Size2Human(countFileSizeReady) + ")";

					if (countSpeedTotal < 1024 * 50) { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL0; }
					else if (countSpeedTotal < 1024 * 100) { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL1; }
					else if (countSpeedTotal < 1024 * 150) { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL2; }
					else if (countSpeedTotal < 1024 * 200) { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL3; }
					else if (countSpeedTotal < 1024 * 250) { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL4; }
					else { this.imageDownloadSpeed.Pixbuf = ImageLoaderGTK.Instance.pbPacketDL5; }
				});
			}
		}

		#endregion
	}
}
