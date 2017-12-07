using Newtonsoft.Json;
using System;

namespace TGS.Interface.Proxying
{
	public static class Serializer
	{
		public static object DeserializeObject(object obj, Type outputType)
		{
			if (outputType.IsValueType || outputType == typeof(string))
				return obj;
			return JsonConvert.DeserializeObject((string)obj, outputType);
		}

		public static object SerializeObject(object obj)
		{
			if (obj is string asString || obj.GetType().IsValueType)
				return obj;
			return JsonConvert.SerializeObject(obj);
		}
	}
}
