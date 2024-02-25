using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace myChatGptTelegramBot
{
    internal class Settings
    {
        public string OPENAI_API_KEY { get; private set; }
        public string OPENAI_API_URL { get; private set; }
        public string BOT_TOKEN { get; private set; }

        private const string SETTINGS_FILE_NAME = "settings.json";

        /// <summary>
        /// Записать данные в файл настроек.
        /// </summary>
        /// <param name="ApiKey">Ключ для доступа к API.</param>
        /// <param name="ApiUrl">URL для доступа к API.</param>
        public Settings(string newApiKey, string newApiUrl)
        {
            TrySetSettings(newApiKey, newApiUrl);

            OPENAI_API_KEY = string.IsNullOrEmpty(newApiKey) ? OPENAI_API_KEY : newApiKey;
            OPENAI_API_URL = string.IsNullOrEmpty(newApiUrl) ? OPENAI_API_URL : newApiUrl;
        }

        /// <summary>
        /// Получить данные из файла настроек.
        /// </summary>
        public Settings()
        {
            var settings = TryGetSettings();
            var currentApiKey = settings?.ApiKey;
            var currentApiUrl = settings?.ApiUrl;
            var botToken = settings?.BotToken;

            OPENAI_API_KEY = string.IsNullOrEmpty(currentApiKey) ? OPENAI_API_KEY : currentApiKey;
            OPENAI_API_URL = string.IsNullOrEmpty(currentApiUrl) ? OPENAI_API_URL : currentApiUrl;
            BOT_TOKEN = string.IsNullOrEmpty(botToken) ? BOT_TOKEN : botToken;
        }

        private _Settings? TryGetSettings()
        {
            var settingsJsonString = string.Empty;
            var fullPathForSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), SETTINGS_FILE_NAME);

            if (!File.Exists(fullPathForSettingsFile))
            {
                Console.WriteLine("Не найден файл с настройками!");
                return null;
            }

            try
            {
                using (var settingsFile = File.OpenRead(fullPathForSettingsFile))
                {
                    var fileBody = new byte[settingsFile.Length];

                    settingsFile.Read(fileBody);
                    settingsJsonString = Encoding.UTF8.GetString(fileBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Попытка получить данные из файла настроек завершилась с ошибкой - {ex.Message}");
                return null;
            }

            var settings = default(_Settings);
            try
            {
                settings = JsonConvert.DeserializeObject<_Settings>(settingsJsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Попытка десериализовать json строку в объект настроек завершилась с ошибкой - {ex.Message}");
                return null;
            }

            return settings;
        }

        private void TrySetSettings(string newApiKey, string newApiUrl)
        {
            if (string.IsNullOrEmpty(newApiKey) && string.IsNullOrEmpty(newApiUrl))
            {
                Console.WriteLine("Нет данных для записи в файл настроек.");
                return;
            }

            var currentSettings = TryGetSettings();
            var currentApiKey = currentSettings?.ApiKey;
            var currentApiUrl = currentSettings?.ApiUrl;
            var needUpdateApiKey = !string.IsNullOrEmpty(newApiKey) && newApiKey != currentApiKey;
            var needUpdateApiUrl = !string.IsNullOrEmpty(newApiUrl) && newApiUrl != currentApiUrl;

            if (!needUpdateApiKey && !needUpdateApiUrl)
                return;

            var settingsObject = new _Settings()
            {
                ApiKey = needUpdateApiKey ? newApiKey : currentApiKey,
                ApiUrl = needUpdateApiUrl ? newApiUrl : currentApiUrl,
                BotToken = currentSettings?.BotToken
            };

            var settingsFile = default(FileStream);
            try
            {
                var fullPathForSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), SETTINGS_FILE_NAME);
                if (!File.Exists(fullPathForSettingsFile))
                    settingsFile = File.Create(fullPathForSettingsFile);
                else
                    settingsFile = File.OpenWrite(fullPathForSettingsFile);

                var settingsJsonString = JsonConvert.SerializeObject(settingsObject);
                if (!string.IsNullOrEmpty(settingsJsonString))
                    settingsFile.Write(Encoding.UTF8.GetBytes(settingsJsonString));

            }
            catch (Exception ex)
            {
                if (ex is Newtonsoft.Json.JsonSerializationException)
                    Console.WriteLine($"Сериализация объекта настроек завершилась с ошибкой - {ex.Message}");
                else
                    Console.WriteLine($"Попытка записать новые данные в файл настроек завершилась с ошибкой - {ex.Message}");
            }
            finally
            {
                settingsFile?.Dispose();
            }
        }
    }

    sealed class _Settings
    {
        public string? ApiKey { get; set; }
        public string? ApiUrl { get; set; }
        public string? BotToken { get; set;}
    }
}
