using Microsoft.Extensions.Configuration;

namespace Core;

public static class ApplicationConfiguration
{
	private static readonly Lazy<IConfigurationRoot> Configuration = new(GetConfiguration);

	public static T? Read<T>() where T : CowConfigurationBase
	{
		return Configuration.Value.Get<T>();
	}

	public static IConfigurationRoot GetConfiguration()
	{
		var applicationPath = AppDomain.CurrentDomain.BaseDirectory;
		var configurationBuilder = new ConfigurationBuilder()
			.SetBasePath(applicationPath);
		configurationBuilder.AddEnvironmentVariables();
		BuildAppSettingsConfiguration(configurationBuilder);
		return configurationBuilder.Build();
	}

	public static void BuildAppSettingsConfiguration(IConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
	}
}

public class CowConfigurationBase
{
	public void SetFrom(CowConfigurationBase other)
	{
		var thisProperties = GetType().GetProperties();
		var otherProperties = other.GetType().GetProperties();

		foreach (var otherProperty in otherProperties)
		{
			foreach (var thisProperty in thisProperties)
			{
				if (otherProperty.Name == thisProperty.Name && otherProperty.PropertyType == thisProperty.PropertyType)
				{
					thisProperty.SetValue(this, otherProperty.GetValue(other));
					break;
				}
			}
		}
	}
}

public class CowConfiguration : CowConfigurationBase
{
	public string Database { get; set; } = null!;

	// Имя окружения. Например, чтобы отличать логи и метрики тетсовых сервисов от боевых
	public string Environment { get; set; } = null!;

	// ConnectionString для подключения к Graphite-relay в формате "address=graphite-relay.com;port=8125;prefixKey=ulearn.local". Можно оставить пустой, чтобы не отправлять метрики
	public string StatsdConnectionString { get; set; } = null!;

	// Некоторые сервисы регулярно посылают пинг в сборщик метрик, по отсутствию пингов можно определить, что сервис умер
	public int? KeepAliveInterval { get; set; }

	// Имя сервиса. Используется в метриках и др.
	public string GraphiteServiceName { get; set; } = null!;

	public TelegramConfiguration Telegram { get; set; } = null!;
	public int? NotificationsUpdateDelay { get; set; }
	public int AnalysesUpdateIntervalInSeconds { get; set; }
	public HostLogConfiguration HostLog { get; set; } = null!;
}

public class DatabaseConfiguration : CowConfigurationBase
{
	public string Database { get; set; } = null!;
}

public class TelegramConfiguration
{
	public string BotToken { get; set; } = null!;
}

public class HostLogConfiguration
{
	// Печатать ли логи на консоль
	public bool Console { get; set; }

	// Какие логи запросов не логировать (например notifications)
	public string? DropRequestRegex { get; set; }

	// Путь до файла с логами
	public string? PathFormat { get; set; }

	// Минимальный уровень логирования
	public string? MinimumLevel { get; set; }

	// Минимальный уровень логирования событий, связанных с базой данных. Debug заставляет вываодть SQL код отправленных запросов
	public string? DbMinimumLevel { get; set; }
}