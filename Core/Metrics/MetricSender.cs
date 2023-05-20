using System.Reflection;
using StatsdClient;
using Vostok.Logging.Abstractions;

namespace Core.Metrics
{
	public class MetricSender
	{
		private static ILog Log => LogProvider.Get().ForContext(typeof(MetricSender));

		private readonly string? _prefix;
		private readonly string? _service;
		private static string MachineName { get; } = Environment.MachineName.Replace(".", "_").ToLower();
		private readonly Statsd? _statsd;
		private bool IsEnabled => _statsd != null;

		public MetricSender(string? service)
		{
			var connectionString = ApplicationConfiguration.Read<CowConfiguration>()!.StatsdConnectionString;
			if (string.IsNullOrEmpty(connectionString))
				return;

			var config = StatsdConfiguration.CreateFrom(connectionString);
			_prefix = config.Prefix;
			_service = service ?? Assembly.GetExecutingAssembly().GetName().Name!.ToLower();

			_statsd = CreateStatsd(config);
		}

		private static Statsd CreateStatsd(StatsdConfiguration config)
		{
			var client = config.IsTCP
				? (IStatsdClient)new StatsdTCPClient(config.Address, config.Port)
				: new StatsdUDPClient(config.Address, config.Port);
			return new Statsd(client, new RandomGenerator(), new StopWatchFactory());
		}

		/* Builds key "{prefix}.{service}.{machine_name}.{key}" */
		public static string BuildKey(string prefix, string service, string key)
		{
			var parts = new[] { prefix, service, MachineName, key }
				.Where(s => !string.IsNullOrEmpty(s))
				.ToArray();
			return string.Join(".", parts);
		}

		public void SendCount(string key, int value = 1)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(_prefix, _service, key);
			Log.Info($"Send count metric {builtKey}, value {value}");
			try
			{
				_statsd.Send<Statsd.Counting>(builtKey, value);
			}
			catch (Exception e)
			{
				Log.Warn(e, $"Can't send count metric {builtKey}, value {value}");
			}
		}

		public void SendTiming(string key, int value)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(_prefix, _service, key);
			Log.Info($"Send timing metric {builtKey}, value {value}");
			try
			{
				_statsd.Send<Statsd.Timing>(builtKey, value);
			}
			catch (Exception e)
			{
				Log.Warn(e, $"Can't send timing metric {builtKey}, value {value}");
			}
		}

		public void SendGauge(string key, double value)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(_prefix, _service, key);
			Log.Info($"Send gauge metric {builtKey}, value {value}");
			try
			{
				_statsd.Send<Statsd.Gauge>(builtKey, value);
			}
			catch (Exception e)
			{
				Log.Warn(e, $"Can't send gauge metric {builtKey}, value {value}");
			}
		}
	}
}