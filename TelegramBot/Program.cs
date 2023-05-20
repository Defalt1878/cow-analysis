using Core;
using Vostok.Hosting;

namespace TelegramBot;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		var mainApplication = new TelegramApplication();
		var setupBuilder = new EnvironmentSetupBuilder("telegram", args);
		var hostSettings = new VostokHostSettings(mainApplication, setupBuilder.EnvironmentSetup);
		var host = new VostokHost(hostSettings);
		await host.WithConsoleCancellation().RunAsync();
	}
}