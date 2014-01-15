// 
//  XgSettingsProvider.cs
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
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace XG.Config
{
	public class XgSettingsProvider : SettingsProvider
	{
		const string Xmlroot = "configuration";
		const string Confignode = "configSections";
		const string Groupnode = "sectionGroup";
		const string Usernode = "userSettings";

		readonly string _appnode = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Properties.Settings";
		private XmlDocument _xmlDoc;

		public override void Initialize(string name, NameValueCollection config)
		{
			base.Initialize(ApplicationName, config);
		}

		public override string ApplicationName
		{
			get
			{
				return "xg";
			}
			set
			{
			}
		}

		public virtual string GetSettingsFilename()
		{
			return ApplicationName + ".config";
		}

		public virtual string GetAppPath()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			appDataPath = appDataPath + (appDataPath.EndsWith("" + Path.DirectorySeparatorChar) ? "" : "" + Path.DirectorySeparatorChar) + "XG";
			return appDataPath;
		}

		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext sContext, SettingsPropertyCollection settingsColl)
		{
			var retValues = new SettingsPropertyValueCollection();

			SettingsPropertyValue setVal;
			foreach (SettingsProperty sProp in settingsColl)
			{
				setVal = new SettingsPropertyValue(sProp);
				setVal.IsDirty = false;
				setVal.SerializedValue = GetSetting(sProp);
				retValues.Add(setVal);
			}
			return retValues;
		}

		public override void SetPropertyValues(SettingsContext sContext, SettingsPropertyValueCollection settingsColl)
		{
			foreach (SettingsPropertyValue spVal in settingsColl)
			{
				SetSetting(spVal);
			}

			try
			{
				XmlConfig.Save(Path.Combine(GetAppPath(), GetSettingsFilename()));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error writing configuration file to disk: " + ex.Message);
			}
		}

		private XmlDocument XmlConfig
		{
			get
			{
				if (_xmlDoc == null)
				{
					_xmlDoc = new XmlDocument();

					try
					{
						_xmlDoc.Load(Path.Combine(GetAppPath(), GetSettingsFilename()));
					}
					catch (Exception)
					{
						XmlDeclaration dec = _xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
						_xmlDoc.AppendChild(dec);

						XmlElement rootNode = _xmlDoc.CreateElement(Xmlroot);
						_xmlDoc.AppendChild(rootNode);

						XmlElement configNode = _xmlDoc.CreateElement(Confignode);
						_xmlDoc.DocumentElement.PrependChild(configNode);

						XmlElement groupNode = _xmlDoc.CreateElement(Groupnode);
						groupNode.SetAttribute("name", Usernode);
						groupNode.SetAttribute("type", "System.Configuration.UserSettingsGroup");
						configNode.AppendChild(groupNode);

						XmlElement newSection = _xmlDoc.CreateElement("section");
						newSection.SetAttribute("name", _appnode);
						newSection.SetAttribute("type", "System.Configuration.ClientSettingsSection");
						groupNode.AppendChild(newSection);

						XmlElement userNode = _xmlDoc.CreateElement(Usernode);
						_xmlDoc.DocumentElement.AppendChild(userNode);

						XmlElement appNode = _xmlDoc.CreateElement(_appnode);
						userNode.AppendChild(appNode);
					}
				}
				return _xmlDoc;
			}
		}

		private object GetSetting(SettingsProperty setProp)
		{
			object retVal;
			try
			{
				if (setProp.SerializeAs.ToString() == "String")
				{
					return XmlConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild.InnerText;
				}
				else
				{
					string settingType = setProp.PropertyType.ToString();
					string xmlData = XmlConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild.InnerXml;
					XmlSerializer xs = new XmlSerializer(typeof(string[]));
					string[] data = (string[])xs.Deserialize(new XmlTextReader(xmlData, XmlNodeType.Element, null));

					switch (settingType)
					{
						case "System.Collections.Specialized.StringCollection":
							StringCollection sc = new StringCollection();
							sc.AddRange(data);
							return sc;
						default:
							return "";
					}
				}
			}
			catch (Exception)
			{
				if ((setProp.DefaultValue != null))
				{
					if (setProp.SerializeAs.ToString() == "String")
					{
						retVal = setProp.DefaultValue.ToString();
					}
					else
					{
						string settingType = setProp.PropertyType.ToString();
						string xmlData = setProp.DefaultValue.ToString();
						XmlSerializer xs = new XmlSerializer(typeof(string[]));
						string[] data = (string[])xs.Deserialize(new XmlTextReader(xmlData, XmlNodeType.Element, null));

						switch (settingType)
						{
							case "System.Collections.Specialized.StringCollection":
								StringCollection sc = new StringCollection();
								sc.AddRange(data);
								return sc;

							default: return "";
						}
					}
				}
				else
				{
					retVal = "";
				}
			}
			return retVal;
		}

		private void SetSetting(SettingsPropertyValue setProp)
		{
			XmlNode settingNode;

			try
			{
				settingNode = XmlConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild;
			}
			catch (Exception)
			{
				settingNode = null;
			}

			if ((settingNode != null))
			{
				if (setProp.Property.SerializeAs.ToString() == "String")
				{
					settingNode.InnerText = setProp.SerializedValue.ToString();
				}
				else
				{
					settingNode.InnerXml = setProp.SerializedValue.ToString().Replace(@"<?xml version=""1.0"" encoding=""utf-16""?>", "");
				}
			}
			else
			{
				XmlNode tmpNode = XmlConfig.SelectSingleNode("//" + _appnode);

				XmlElement newSetting = _xmlDoc.CreateElement("setting");
				newSetting.SetAttribute("name", setProp.Name);

				if (setProp.Property.SerializeAs.ToString() == "String")
				{
					newSetting.SetAttribute("serializeAs", "String");
				}
				else
				{
					newSetting.SetAttribute("serializeAs", "Xml");
				}

				tmpNode.AppendChild(newSetting);

				XmlElement valueElement = _xmlDoc.CreateElement("value");
				if (setProp.Property.SerializeAs.ToString() == "String")
				{
					valueElement.InnerText = setProp.SerializedValue.ToString();
				}
				else
				{
					valueElement.InnerXml = setProp.SerializedValue.ToString().Replace(@"<?xml version=""1.0"" encoding=""utf-16""?>", "");
				}

				newSetting.AppendChild(valueElement);
			}
		}
	}
}