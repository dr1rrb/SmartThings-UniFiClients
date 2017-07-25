using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Framework.Extensions
{
	public static class HttpContentExtensions
	{
		private static readonly MethodInfo _tryComputeLength = typeof(HttpContent).GetMethod("TryComputeLength", BindingFlags.Instance | BindingFlags.NonPublic);

		public static bool TryComputeLength(this HttpContent content, out long length)
		{
			var parameters = new object[1];
			var hasComputedLength = (bool) _tryComputeLength.Invoke(content, parameters);

			if (hasComputedLength)
			{
				length = (long) parameters[0];
				return true;
			}
			else
			{
				length = default(long);
				return false;
			}
		}

		public static bool TrySetContentLength(this HttpContent content)
		{
			long length;
			if (content.TryComputeLength(out length))
			{
				content.Headers.ContentLength = length;
				return true;
			}
			else
			{
				return false;
			}
		}

	}
}