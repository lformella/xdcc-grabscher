// 
//  CustomBootstrapper.cs
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

using System;
using Nancy;
using Nancy.Conventions;
using System.Text;
using System.IO;
using Nancy.Responses;
using System.Reflection;

namespace XG.Plugin.Webserver.Nancy
{
	public class CustomBootstrapper : DefaultNancyBootstrapper
	{
		private byte[] favicon;

		protected override byte[] FavIcon
		{
			get { return this.favicon?? (this.favicon= LoadFavIcon()); }
		}

		private byte[] LoadFavIcon()
		{
			using (var resourceStream = GetType().Assembly.GetManifestResourceStream("XG.Plugin.Webserver.Resources.favicon.ico"))
			{
				var tempFavicon = new byte[resourceStream.Length];
				resourceStream.Read(tempFavicon, 0, (int)resourceStream.Length);
				return tempFavicon;
			}
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);

#if DEBUG && __MonoCS__
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("fonts", "fonts"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Resources", "Resources"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Scripts", "Scripts"));
#else
			nancyConventions.StaticContentsConventions.Add((ctx, rootPath) =>
			{
				var directoryName = Path.GetDirectoryName(ctx.Request.Url.Path).Replace(Path.DirectorySeparatorChar, '.').Replace("-","_");
				var fileName = Path.GetFileName(ctx.Request.Url.Path);
				return new EmbeddedFileResponse(GetType().Assembly, "XG.Plugin.Webserver" + directoryName, fileName);
			});
#endif
		}
	}
}

