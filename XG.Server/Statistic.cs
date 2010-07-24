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
	public enum StatisticType : byte
	{
		BytesLoaded,

		PacketsCompleted,
		PacketsRequested,

		FilesCompleted,
		FilesBroken,

		ServerConnectsOk,
		ServerConnectsFailed,

		ChannelConnectsOk,
		ChannelConnectsFailed,

		BotConnectsOk,
		BotConnectsFailed,

		SpeedMax,
		SpeedMin,
		SpeedAvg
	}

	[Serializable()]
	public class Statistic
	{
		[field: NonSerialized()]
		private static Statistic instance = null;
		[field: NonSerialized()]
		private static object StatisticLock = new object();

		private Dictionary<StatisticType, Int64> myValuesInt = new Dictionary<StatisticType, Int64>();
		private Dictionary<StatisticType, double> myValuesDouble = new Dictionary<StatisticType, double>();

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
				StreamReader sr = new StreamReader("./statistic.xml");
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
				StreamWriter sw = new StreamWriter("./statistic.xml");
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
