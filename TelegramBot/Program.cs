﻿using Core;
using TelegramBot;
using Vostok.Hosting;

var mainApplication = new TelegramApplication();
var setupBuilder = new EnvironmentSetupBuilder("telegram", args);
var hostSettings = new VostokHostSettings(mainApplication, setupBuilder.EnvironmentSetup);
var host = new VostokHost(hostSettings);
await host.WithConsoleCancellation().RunAsync();