using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Framework.Web
{
	public class JsonContent : HttpContent
	{
		private static readonly Lazy<byte[]> _empty = new Lazy<byte[]>(() => new byte[0], LazyThreadSafetyMode.None);
		private readonly Lazy<byte[]> _content;

		public JsonContent(object value)
		{
			_content= value == null
				? _empty
				: new Lazy<byte[]>(() => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));

			Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") {CharSet = "utf-8"};
		}

		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			var content = _content.Value;
			await stream.WriteAsync(content, 0, content.Length);
		}

		protected override bool TryComputeLength(out long length)
		{
			length = _content.Value.Length;
			return true;
		}
	}
}