namespace Core.Metrics
{
	public class StatsdConfiguration
	{
		public string Address { get; set; }
		public int Port { get; set; }
		public string Prefix { get; set; }
		public bool IsTCP { get; set; }

		/// <summary>
		/// </summary>
		/// <param name="connectionString">e.g. "address=graphite-test;port=8125;transport=Tcp;prefixKey=ulearn.local"</param>
		/// <returns></returns>
		public static StatsdConfiguration CreateFrom(string connectionString)
		{
			var result = new StatsdConfiguration();

			var connectionConfig = ParseConnectionString(connectionString);
			if (connectionConfig.TryGetValue("address", out var address))
				result.Address = address;
			if (connectionConfig.TryGetValue("port", out var portString) && int.TryParse(portString, out var port))
				result.Port = port;
			if (connectionConfig.TryGetValue("prefixKey", out var prefixKey))
				result.Prefix = prefixKey;
			result.IsTCP = connectionConfig.TryGetValue("transport", out var transport)
							&& string.Compare(transport, "tcp", StringComparison.InvariantCultureIgnoreCase) != 0;

			return result;
		}

		private static IDictionary<string, string> ParseConnectionString(string connectionString)
		{
			return connectionString
				.Split(';')
				.Select(x => x.Split('='))
				.Where(x => x.Length == 2)
				.ToDictionary(x => x[0], x => x[1]);
		}
	}
}