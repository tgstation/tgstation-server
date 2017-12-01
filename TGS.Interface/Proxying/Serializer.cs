using Newtonsoft.Json;
using System;

namespace TGS.Interface.Proxying
{
	public static class Serializer
	{
		public static object DeserializeObject(string json, Type outputType)
		{
			if (outputType == typeof(string))
				return json;
			if (outputType.IsValueType)
				return Convert.ChangeType(json, outputType);
			return JsonConvert.DeserializeObject(json, outputType);
		}

		public static string SerializeObject(object obj)
		{
			if (obj is string asString)
				return asString;
			if (obj.GetType().IsValueType)
				return obj.ToString();
			return JsonConvert.SerializeObject(obj);
		}
	}
}
