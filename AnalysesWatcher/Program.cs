using AnalysesWatcher;
using Core;
using Vostok.Hosting;

var mainApplication = new AnalysesWatcherApplication();
var setupBuilder = new EnvironmentSetupBuilder("watcher", args);
var hostSettings = new VostokHostSettings(mainApplication, setupBuilder.EnvironmentSetup);
var host = new VostokHost(hostSettings);
await host.WithConsoleCancellation().RunAsync();