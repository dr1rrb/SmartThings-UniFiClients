using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Torick.Extensions;
using Torick.Web;
using Newtonsoft.Json;

namespace Torick.Smartthings.Devices.UniFi
{
	public class UniFiController : IUniFiController, IDisposable
	{
		private readonly HttpClient _client = new HttpClient(new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => certificate.Thumbprint == "713C359A4F9C5706F2CDBDC79BF7D0EC4C6B2788",
		});

		private readonly Uri _controllerApiBaseUri;
		private readonly string _username;
		private readonly string _password;
		private readonly IScheduler _scheduler;
		private readonly IObservable<ImmutableList<Client>> _clients;

		public UniFiController(Uri controllerApiBaseUri, string username, string password, IScheduler scheduler)
		{
			_controllerApiBaseUri = controllerApiBaseUri;
			_username = username;
			_password = password;
			_scheduler = scheduler;

			_clients = Observable
				// Once authenticated, ensure to stay authenticated and pull the controller each 1 sec
				.Using(
					EnsureAuthentication,
					async (_, ct) => Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1), _scheduler))
				.Select(_ => Observable.FromAsync(GetClients))
				.Switch()

				// If no value is produced for more than a minute (eg. controller offline), assume that all devices are offline, 
				// and fail subscription itself in order to retry the whole sequence.
				.Timeout(
					TimeSpan.FromMinutes(1),
					Observable
						.Return(ImmutableList<Client>.Empty, _scheduler)
						.Concat(Observable.Throw<ImmutableList<Client>>(new TimeoutException())),
					_scheduler)

				// In any case, retry every 5 sec
				.Retry(TimeSpan.FromSeconds(5), _scheduler)

				// Share the same subscription for each devices !
				.Replay(1)
				.RefCount();
		}

		public IObservable<ImmutableList<Client>> GetAndObserveClients() => _clients;

		private async Task<IDisposable> EnsureAuthentication(CancellationToken ct)
		{
			try
			{
				await Authorize(ct, tries: 10);
			}
			catch (Exception)
			{
				Console.Error.WriteLine("Failed to maintain authenticated context");
			}

			return _scheduler.ScheduleAsync(TimeSpan.FromHours(1), async (s, ct2) => await EnsureAuthentication(ct2));
		}

		private async Task Authorize(CancellationToken ct, int tries = 1)
		{
			var tentative = 0;
			while (true)
			{
				try
				{
					tentative++;

					var response = await _client.PostAsync(
						_controllerApiBaseUri + "login",
						new JsonContent(new LoginRequest
						{
							UserName = _username,
							Password = _password
						}),
						ct);

					response.EnsureSuccessStatusCode();

					Console.WriteLine($"Authenticated to the UniFi controller");

					break;
				}
				catch (Exception e) when (tentative < tries)
				{
					Console.WriteLine($"Failed to autenticate: {e}");
				}
			}
		}

		private async Task<ImmutableList<Client>> GetClients(CancellationToken ct)
		{
			try
			{
				var uri = _controllerApiBaseUri + "s/default/stat/sta/";
				var response = await _client.GetAsync(uri, ct);
				if (response.StatusCode == HttpStatusCode.Unauthorized)
				{
					await Authorize(ct);
					response = await _client.GetAsync(uri, ct);
				}

				var responseContent = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

				return JsonConvert.DeserializeObject<ApiResponse<ImmutableList<Client>>>(responseContent).Data ?? ImmutableList<Client>.Empty;
			}
			catch (Exception)
			{
				return ImmutableList<Client>.Empty;
			}
		}

		public IObservable<bool> GetAndObserveIsConnected(string clientId) => _clients
			.Select(clients => clients.Any(client => client.Id.Equals(clientId, StringComparison.OrdinalIgnoreCase)))
			.DistinctUntilChanged();

		public void Dispose() => _client.Dispose();
	}
}