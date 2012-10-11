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
using System.Xml.Serialization;
using log4net;

namespace XG.Server
{
	public enum StatisticType : int
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
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}
 
		public void ReadXml(System.Xml.XmlReader reader)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
 
			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();
 
			if (wasEmpty) { return; }
 
			while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
			{
				reader.ReadStartElement("item");
 
				reader.ReadStartElement("key");
				TKey key = (TKey)keySerializer.Deserialize(reader);
				reader.ReadEndElement();
 
				reader.ReadStartElement("value");
				TValue value = (TValue)valueSerializer.Deserialize(reader);
				reader.ReadEndElement();
 
				Add(key, value);
 
				reader.ReadEndElement();
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}
 
		public void WriteXml(System.Xml.XmlWriter writer)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
 
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
		static readonly ILog _log = LogManager.GetLogger(typeof(Statistic));

		static object locked = new object();
		static XmlSerializer serializer = new XmlSerializer(typeof(Statistic));

		[NonSerialized]
		static Statistic instance = null;
		[NonSerialized]
		static object statisticLock = new object();

		SerializableDictionary<StatisticType, Int64> myValuesInt = new SerializableDictionary<StatisticType, Int64>();
		public SerializableDictionary<StatisticType, Int64> ValuesInt
		{
			get { return myValuesInt; }
			set { myValuesInt = value; }
		}

		SerializableDictionary<StatisticType, double> myValuesDouble = new SerializableDictionary<StatisticType, double>();
		public SerializableDictionary<StatisticType, double> ValuesDouble
		{
			get { return myValuesDouble; }
			set { myValuesDouble = value; }
		}

		public static Statistic Instance
		{
			get
			{
				if (instance == null)
				{
					instance = Deserialize();
					Serialize();
				}
				return instance;
			}
		}

		static Statistic Deserialize()
		{
			lock(locked)
			{
				try
				{
					Stream streamRead = File.OpenRead("./statistics.xml");
					Statistic statistic = (Statistic)serializer.Deserialize(streamRead);
					streamRead.Close();
					return statistic;
				}
				catch (Exception ex)
				{
					_log.Fatal("Statistic.Deserialize() ", ex);
					return new Statistic();
				}
			}
		}

		static void Serialize()
		{
			lock(locked)
			{
				try
				{
					Stream streamWrite = File.Create("./statistics.xml");
					serializer.Serialize(streamWrite, instance);
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
					_log.Fatal("Statistic.Serialize() ", ex);
				}
			}
		}

		Statistic()
		{
		}

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
			lock(statisticLock)
			{
				if(!myValuesInt.ContainsKey(aType)) { myValuesInt.Add(aType, aValue); }
				else { myValuesInt[aType] += aValue; }
			}
		}

		public double Get(StatisticType aType)
		{
			if(!myValuesDouble.ContainsKey(aType))
			{
				if(!myValuesInt.ContainsKey(aType))
				{
					return 0;
				}
				else
				{
					return myValuesInt[aType];
				}
			}
			else
			{
				return myValuesDouble[aType];
			}
		}

		public void Set(StatisticType aType, double aValue)
		{
			lock(statisticLock)
			{
				if(!myValuesDouble.ContainsKey(aType)) { myValuesDouble.Add(aType, aValue); }
				else { myValuesDouble[aType] = aValue; }
			}
		}
	}
}
