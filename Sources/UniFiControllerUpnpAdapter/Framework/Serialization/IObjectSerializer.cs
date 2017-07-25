using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Serialization
{
    public interface IObjectSerializer
    {
	    object FromStream(Stream stream, Type targetType);

		void WriteToStream(object value, Type targetType, Stream stream, bool canDisposeStream = true);
	}
}
