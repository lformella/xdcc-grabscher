// 
//  Statistic.cs
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using log4net;

namespace XG.Server
{
	public enum StatisticType
	{
		BytesLoaded,

		PacketsCompleted,
		PacketsIncompleted,
		PacketsBroken,

		PacketsRequested,
		PacketsRemoved,

		FilesCompleted,
		FilesBroken,

		ServerConnectsOk,
		ServerConnectsFailed,

		ChannelConnectsOk,
		ChannelConnectsFailed,
		ChannelsJoined,
		ChannelsParted,
		ChannelsKicked,

		BotConnectsOk,
		BotConnectsFailed,

		SpeedMax
	}

	[XmlRoot("dictionary")]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
	{
		#region IXmlSerializable Members

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			var keySerializer = new XmlSerializer(typeof (TKey));
			var valueSerializer = new XmlSerializer(typeof (TValue));

			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();

			if (wasEmpty)
			{
				return;
			}

			while (reader.NodeType != XmlNodeType.EndElement)
			{
				reader.ReadStartElement("item");

				reader.ReadStartElement("key");
				var key = (TKey) keySerializer.Deserialize(reader);
				reader.ReadEndElement();

				reader.ReadStartElement("value");
				var value = (TValue) valueSerializer.Deserialize(reader);
				reader.ReadEndElement();

				Add(key, value);

				reader.ReadEndElement();
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			var keySerializer = new XmlSerializer(typeof (TKey));
			var valueSerializer = new XmlSerializer(typeof (TValue));

			foreach (TKey key in Keys)
			{
				writer.WriteStartElement("item");

				writer.WriteStartElement("key");
				keySerializer.Serialize(writer, key);
				writer.WriteEndElement();

				writer.WriteStartElement("value");
				TValue value = this[key];
				valueSerializer.Serialize(writer, value);
				writer.WriteEndElement();

				writer.WriteEndElement();
			}
		}

		#endregion
	}

	[Serializable]
	public class Statistic
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static readonly object Locked = new object();
		static readonly XmlSerializer Serializer = new XmlSerializer(typeof (Statistic));

		[NonSerialized]
		static Statistic _instance;

		[NonSerialized]
		static readonly object StatisticLock = new object();

		SerializableDictionary<StatisticType, Int64> _myValuesInt = new SerializableDictionary<StatisticType, Int64>();

		public SerializableDictionary<StatisticType, Int64> ValuesInt
		{
			get { return _myValuesInt; }
			set { _myValuesInt = value; }
		}

		SerializableDictionary<StatisticType, double> _myValuesDouble = new SerializableDictionary<StatisticType, double>();

		public SerializableDictionary<StatisticType, double> ValuesDouble
		{
			get { return _myValuesDouble; }
			set { _myValuesDouble = value; }
		}

		public static Statistic Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Deserialize();
					Serialize();
				}
				return _instance;
			}
		}

		static Statistic Deserialize()
		{
			if (File.Exists(Settings.Instance.AppDataPath + "statistics.xml"))
			{
				lock (Locked)
				{
					try
					{
						Stream streamRead = File.OpenRead(Settings.Instance.AppDataPath + "statistics.xml");
						var statistic = (Statistic) Serializer.Deserialize(streamRead);
						streamRead.Close();
						return statistic;
					}
					catch (Exception ex)
					{
						Log.Fatal("Statistic.Deserialize", ex);
					}
				}
			}
			else
			{
				Log.Error("Statistic.Deserialize found no settings file");
			}
			return new Statistic();
		}

		static void Serialize()
		{
			lock (Locked)
			{
				try
				{
					Stream streamWrite = File.Create(Settings.Instance.AppDataPath + "statistics.xml");
					Serializer.Serialize(streamWrite, _instance);
					streamWrite.Close();
				}
				catch (InvalidOperationException)
				{
					// this is ok and happens once in a while
				}
				catch (IOException)
				{
					// this is not really ok, but happens once in a while
				}
				catch (Exception ex)
				{
					Log.Fatal("Statistic.Serialize", ex);
				}
			}
		}

		Statistic() {}

		public void Save()
		{
			Serialize();
		}

		public void Increase(StatisticType aType)
		{
			Increase(aType, 1);
		}

		public void Increase(StatisticType aType, Int64 aValue)
		{
			lock (StatisticLock)
			{
				if (!_myValuesInt.ContainsKey(aType))
				{
					_myValuesInt.Add(aType, aValue);
				}
				else
				{
					_myValuesInt[aType] += aValue;
				}
			}
		}

		public double Get(StatisticType aType)
		{
			if (!_myValuesDouble.ContainsKey(aType))
			{
				if (!_myValuesInt.ContainsKey(aType))
				{
					return 0;
				}
				return _myValuesInt[aType];
			}
			return _myValuesDouble[aType];
		}

		public void Set(StatisticType aType, double aValue)
		{
			lock (StatisticLock)
			{
				if (!_myValuesDouble.ContainsKey(aType))
				{
					_myValuesDouble.Add(aType, aValue);
				}
				else
				{
					_myValuesDouble[aType] = aValue;
				}
			}
		}
	}
}
