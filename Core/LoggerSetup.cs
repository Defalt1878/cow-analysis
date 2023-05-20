﻿using System.Text.RegularExpressions;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;
using Vostok.Logging.Console;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.Formatting;
using LogEvent = Vostok.Logging.Abstractions.LogEvent;

namespace Core
{
	public static class LoggerSetup
	{
		public static readonly OutputTemplate OutputTemplate
			= OutputTemplate.Parse(
				"{Timestamp:HH:mm:ss.fff} {Level:u5} {traceContext:w}{operationContext:w}{sourceContext:w}{threadId:w}{address:w}{user:w} {Message}{NewLine}{Exception}");

		public static ILog Setup(HostLogConfiguration hostLog, string? subdirectory = null,
			bool addInLogProvider = true)
		{
			var (minimumLevel, dbMinimumLevel) = GetMinimumLevels(hostLog);
			var min = dbMinimumLevel > minimumLevel ? minimumLevel : dbMinimumLevel;

			ILog? consoleLog = null;
			if (hostLog.Console)
				consoleLog = new ConsoleLog(new ConsoleLogSettings {OutputTemplate = OutputTemplate});

			ILog? fileLog = null;
			if (!string.IsNullOrEmpty(hostLog.PathFormat))
				fileLog = SetupFileLog(subdirectory!, hostLog.PathFormat);

			var log = new CompositeLog(new[] {fileLog, consoleLog}.Where(l => l != null).ToArray())
				.WithProperty("threadId", () => Environment.CurrentManagedThreadId);

			if (hostLog.DropRequestRegex != null)
				DropRequestRegex = new Regex(hostLog.DropRequestRegex);
			log = FilterLogs(log, minimumLevel, dbMinimumLevel)
				.WithMinimumLevel(min);

			if (addInLogProvider)
				LogProvider.Configure(log);
			return log;
		}

		private static ILog SetupFileLog(string? subdirectory, string pathFormat)
		{
			pathFormat =
				pathFormat.Replace("{Date}",
					"{RollingSuffix}"); // Для совместимости с настройками appsettings.json, написанными для серилога
			if (Path.IsPathRooted(pathFormat) && subdirectory != null)
			{
				var directory = Path.GetDirectoryName(pathFormat)!;
				var fileName = Path.GetFileName(pathFormat);
				pathFormat = Path.Combine(directory, subdirectory, fileName);
			}

			var fileLogSettings = new FileLogSettings
			{
				FilePath = pathFormat,
				RollingStrategy = new RollingStrategyOptions
				{
					MaxFiles = 0,
					Type = RollingStrategyType.Hybrid,
					Period = RollingPeriod.Day,
					MaxSize = 4 * 1073741824L,
				},
				OutputTemplate = OutputTemplate
			};
			return new FileLog(fileLogSettings);
		}

		public static (LogLevel MinimumLevel, LogLevel DbMinimumLevel) GetMinimumLevels(HostLogConfiguration hostLog)
		{
			var minimumLevelString = hostLog.MinimumLevel ?? "debug";
			var dbMinimumLevelString = hostLog.DbMinimumLevel ?? "";
			if (!TryParseLogLevel(minimumLevelString, out var minimumLevel))
				minimumLevel = LogLevel.Debug;
			if (!TryParseLogLevel(dbMinimumLevelString, out var dbMinimumLevel))
				dbMinimumLevel = minimumLevel;
			return (minimumLevel, dbMinimumLevel);
		}

		public static ILog FilterLogs(ILog log, LogLevel minimumLevel, LogLevel dbMinimumLevel)
		{
			return log.WithMinimumLevelForSourceContext("CowDb", dbMinimumLevel) // Database
				.DropEvents(e => IsDropDatabaseCoreLogEventForDrop(e, dbMinimumLevel))
				.DropEvents(IfShouldBeDroppedByRegex);
		}

		private static bool IsDropDatabaseCoreLogEventForDrop(LogEvent e, LogLevel dbMinimumLevel)
		{
			if (dbMinimumLevel < LogLevel.Info || e.Level >= dbMinimumLevel || e.Properties == null)
				return false;
			var sourceContext = GetSourceContext(e);
			if (sourceContext == null)
				return false;
			return sourceContext.Contains("Microsoft.EntityFrameworkCore.Database.Command")
			       || sourceContext.Contains("Microsoft.EntityFrameworkCore.Infrastructure");
		}

		private static Regex? DropRequestRegex;

		private static bool IfShouldBeDroppedByRegex(LogEvent e)
		{
			if (DropRequestRegex is null || e.Exception is not null) 
				return false;

			var isLoggingMiddleware = GetSourceContext(e)?.Contains("LoggingMiddleware") ?? false;
			if (!isLoggingMiddleware)
				return false;

			var operationContext = GetOperationContext(e);
			if (operationContext == null)
				return false;

			var requestPath = operationContext.FirstOrDefault(c => c.StartsWith("RequestPath"));

			return requestPath != null && DropRequestRegex.IsMatch(requestPath);
		}

		public static SourceContextValue? GetSourceContext(LogEvent? @event)
		{
			if (@event?.Properties == null)
				return null;
			return @event.Properties.TryGetValue(WellKnownProperties.SourceContext, out var value)
				? value as SourceContextValue
				: null;
		}

		public static OperationContextValue? GetOperationContext(LogEvent? @event)
		{
			if (@event?.Properties == null)
				return null;
			return @event.Properties.TryGetValue(WellKnownProperties.OperationContext, out var value)
				? value as OperationContextValue
				: null;
		}

		// Для совместимости с настройками appsettings.json, написанными для серилога
		public static bool TryParseLogLevel(string str, out LogLevel level)
		{
			if (Enum.TryParse(str, true, out level) && Enum.IsDefined(typeof(LogLevel), level))
				return true;
			str = str.ToLowerInvariant();
			switch (str)
			{
				case "verbose":
					level = LogLevel.Debug;
					return true;
				case "debug":
					level = LogLevel.Debug;
					return true;
				case "information":
					level = LogLevel.Info;
					return true;
				case "warning":
					level = LogLevel.Warn;
					return true;
				case "error":
					level = LogLevel.Error;
					return true;
				case "fatal":
					level = LogLevel.Fatal;
					return true;
				default:
					return false;
			}
		}
	}
}