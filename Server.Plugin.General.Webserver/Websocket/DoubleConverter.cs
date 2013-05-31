using System;

using Newtonsoft.Json;

namespace XG.Server.Plugin.General.Webserver.Websocket
{
	class DoubleConverter : JsonConverter
	{
		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var val = value as double? ?? (double?)(value as float?);
			if (val == null || Double.IsNaN((double)val) || Double.IsInfinity((double)val))
			{
				writer.WriteNull();
				return;
			}
			writer.WriteValue((double)val);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(double) || objectType == typeof(float);
		}
	}
}
