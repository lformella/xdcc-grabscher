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
using System.IO;
using System.Linq;
using System.Reflection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Responses;
using Nancy.TinyIoc;
using XG.Config.Properties;
using XG.Plugin.Webserver.Nancy.Authentication;

namespace XG.Plugin.Webserver.Nancy
{
	public class CustomBootstrapper : DefaultNancyBootstrapper
	{
		byte[] _favicon;
		string _basePath = "/" + Settings.Default.XgVersion;

		protected override byte[] FavIcon
		{
			get { return _favicon?? (_favicon = LoadFavIcon()); }
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);

			pipelines.EnableApiKeyAuthentication();
		}

		byte[] LoadFavIcon()
		{
			using (var resourceStream = GetType().Assembly.GetManifestResourceStream("XG.Plugin.Webserver.Resources.favicon.ico"))
			{
				var tempFavicon = new byte[resourceStream.Length];
				resourceStream.Read(tempFavicon, 0, (int)resourceStream.Length);
				return tempFavicon;
			}
		}

		string[] resourceNames;

		bool ResourceExists(string resourceName)
		{
			if (resourceNames == null)
			{
				resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			}

			return resourceNames.Contains(resourceName);
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);

#if DEBUG && __MonoCS__
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Content", "Content"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("fonts", "fonts"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Resources", "Resources"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Scripts", "Scripts"));
#else
			nancyConventions.StaticContentsConventions.Add((ctx, rootPath) => GetResource(ctx.Request.Url.Path));
#endif
		}

		EmbeddedFileResponse GetResource(string aPath)
		{
			if (aPath.StartsWith(_basePath, StringComparison.CurrentCulture))
			{
				aPath = aPath.Substring(_basePath.Length);
			}

#if DEBUG
			if (aPath == "/Resources/xg.appcache")
			{
				return null;
			}
#endif
			try
			{
				var directoryName = "XG.Plugin.Webserver" + Path.GetDirectoryName(aPath).Replace(Path.DirectorySeparatorChar, '.');
#if !__MonoCS__
				directoryName = directoryName.Replace("-", "_");
#endif
				var fileName = Path.GetFileName(aPath);
				if (ResourceExists(directoryName + "." + fileName))
				{
					return new EmbeddedFileResponse(GetType().Assembly, directoryName, fileName);
				}
			}
			catch(Exception)
			{
				return null;
			}
			return null;
		}
	}
}
