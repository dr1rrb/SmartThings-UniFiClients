using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Framework.Serialization
{
	public class JsonConverterObjetSerializer : IObjectSerializer
	{
		public object FromStream(Stream stream, Type targetType)
		{
			using (var reader = new StreamReader(stream))
			{
				return JsonConvert.DeserializeObject(reader.ReadToEnd(), targetType);
			}
		}

		public void WriteToStream(object value, Type targetType, Stream stream, bool canDisposeStream)
		{
			var writer = new StreamWriter(stream);
			try
			{
				writer.Write(JsonConvert.SerializeObject(value));
				writer.Flush();
			}
			finally
			{
				if (canDisposeStream)
				{
					writer.Dispose();
					stream.Dispose();
				}
			}
		}
	}
}
