//  
//  Copyright (C) 2010 Lars Formella <ich@larsformella.de>
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
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using XG.Core;

namespace XG.Server
{
	[Flags]
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

		SpeedMax,
		SpeedMin,
		SpeedAvg
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
 
				this.Add(key, value);
 
				reader.ReadEndElement();
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}
 
		public void WriteXml(System.Xml.XmlWriter writer)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
 
			foreach (TKey key in this.Keys)
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

	[Serializable()]
	public class Statistic
	{
		[field: NonSerialized()]
		private static Statistic instance = null;
		[field: NonSerialized()]
		private static object StatisticLock = new object();

		private SerializableDictionary<StatisticType, Int64> myValuesInt = new SerializableDictionary<StatisticType, Int64>();
		public SerializableDictionary<StatisticType, Int64> ValuesInt
		{
			get { return myValuesInt; }
			set { myValuesInt = value; }
		}

		private SerializableDictionary<StatisticType, double> myValuesDouble = new SerializableDictionary<StatisticType, double>();
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

		private static Statistic Deserialize()
		{
			try
			{
				XmlSerializer ser = new XmlSerializer(typeof(Statistic));
				StreamReader sr = new StreamReader("./statistics.xml");
				Statistic statistic = (Statistic)ser.Deserialize(sr);
				sr.Close();
				return statistic;
			}
			catch (Exception ex)
			{
				XGHelper.Log("Settings.Instance: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				return new Statistic();
			}
		}

		private static void Serialize()
		{
			try
			{
				XmlSerializer ser = new XmlSerializer(typeof(Statistic));
				StreamWriter sw = new StreamWriter("./statistics.xml");
				ser.Serialize(sw, instance);
				sw.Close();
			}
			catch (Exception ex)
			{
				XGHelper.Log("Statistic.Instance: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
		}

		private Statistic()
		{
		}

		public void Save()
		{
			Serialize();
		}

		public void Increase(StatisticType aType)
		{
			this.Increase(aType, 1);
		}

		public void Increase(StatisticType aType, Int64 aValue)
		{
			lock(StatisticLock)
			{
				if(!this.myValuesInt.ContainsKey(aType)) { this.myValuesInt.Add(aType, aValue); }
				else { this.myValuesInt[aType] += aValue; }
			}
		}

		public double Get(StatisticType aType)
		{
			if(!this.myValuesDouble.ContainsKey(aType)) { return 0; }
			else { return this.myValuesDouble[aType]; }
		}

		public void Set(StatisticType aType, double aValue)
		{
			lock(StatisticLock)
			{
				if(!this.myValuesDouble.ContainsKey(aType)) { this.myValuesDouble.Add(aType, aValue); }
				else { this.myValuesDouble[aType] = aValue; }
			}
		}
	}
}
