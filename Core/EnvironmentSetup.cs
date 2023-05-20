using Vostok.Configuration;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Sources;
using Vostok.Configuration.Sources.CommandLine;
using Vostok.Configuration.Sources.Json;
using Vostok.Hosting.Setup;

namespace Core
{
	public class EnvironmentSetupBuilder
	{
		private readonly string _application;
		private readonly string[] _commandLineArguments;

		public EnvironmentSetupBuilder(string application, string[] commandLineArguments)
		{
			_application = application;
			_commandLineArguments = commandLineArguments;
		}

		public void EnvironmentSetup(IVostokHostingEnvironmentBuilder builder)
		{
			var configurationProvider = new ConfigurationProvider(new ConfigurationProviderSettings());
			var configurationSource = GetConfigurationSource();
			configurationProvider.SetupSourceFor<CowConfiguration>(configurationSource);
			var cowConfiguration = configurationProvider.Get<CowConfiguration>();
			var environment = cowConfiguration.Environment ?? "dev";

			builder.SetupApplicationIdentity(identityBuilder => identityBuilder
					.SetProject("cowAnalysis")
					.SetApplication(_application)
					.SetEnvironment(environment)
					.SetInstance(Environment.MachineName.Replace(".", "_").ToLower())
				)
				.SetupConfiguration(configurationBuilder => configurationBuilder.AddSecretSource(configurationSource));
			builder
				.DisableServiceBeacon()
				.SetupLog((logBuilder, _) => SetupLog(logBuilder, cowConfiguration));
		}

		private static void SetupLog(IVostokCompositeLogBuilder logBuilder, CowConfiguration cowConfiguration)
		{
			var log = LoggerSetup.Setup(cowConfiguration.HostLog, cowConfiguration.GraphiteServiceName, false);
			logBuilder.AddLog(log);
		}

		private IConfigurationSource GetConfigurationSource()
		{
			var configurationSource = new CommandLineSource(_commandLineArguments)
				.CombineWith(new JsonFileSource("appsettings.json"))
				.CombineWith(new JsonFileSource(
					"appsettings." +
					(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production") +
					".json"
				));
			var environmentName = Environment.GetEnvironmentVariable("CowEnvironmentName");
			if (environmentName != null && environmentName.ToLower().Contains("local"))
				configurationSource = configurationSource.CombineWith(new JsonFileSource("appsettings.local.json"));
			return configurationSource;
		}
	}
}