// 
//  RemoteSettings.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System.Collections.Generic;

namespace XG.Plugin.Webserver
{
	public class RemoteSettings
	{
		public Version Version { get; set; }
		public ExternalSearch ExternalSearch { get; set; }
		public IEnumerable<Message> Messages { get; set; }
	}

	public class Version
	{
		public string Latest { get; set; }
		public string Name { get; set; }
		public string Url { get; set; }
		public bool ShowWarning { get; set; }
	}

	public class ExternalSearch
	{
		public bool Enabled { get; set; }
		public string Url { get; set; }
	}

	public class Message
	{
		public bool Enabled { get; set; }
		public string Text { get; set; }
		public string Type { get; set; }
	}
}
