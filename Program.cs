using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using OpenAI_API;
using myChatGptTelegramBot;

namespace TelegramBot_Chat_GPT
{
    class Program
    {
        private static OpenAI_ChatGPT? chatGPT = default;
        private static bool NeedSetNewApiKey = false;
        private static bool NeedSetNewApiUrl = false;
        private static List<string> ALLOWED_USERS { get; set; }

        private static void StartBot()
        {
            var settings = new Settings();
            var botToken = settings?.BOT_TOKEN;

            if (string.IsNullOrEmpty(botToken))
                return;

            ALLOWED_USERS = settings?.ALLOWED_USERS;

            if (ALLOWED_USERS == null || ALLOWED_USERS.Count == 0)
            {
                Console.WriteLine("Не удалось получить список пользователей с правом доступа к боту.");
                return;
            }    

            var bot = new TelegramBotClient(botToken);
            var cancellationToken = new CancellationTokenSource().Token;

            if (bot.TestApiAsync(cancellationToken).Result)
            {
                var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

                bot.DeleteWebhookAsync(true);
                bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
                chatGPT = new OpenAI_ChatGPT(settings);

                Console.WriteLine($"Запуск бота {bot.GetMeAsync().Result.FirstName} завершен успешно.");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Не удалось запустить бот, проверьте корректность токена бота.");
                Console.ReadLine();
            }
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update?.Message == null)
                return;

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var messageText = message.Text;
                var messageChat = message.Chat;
                var userName = messageChat.Username;

                // Логирование всех входящих сообщений.
                Console.WriteLine($"[{DateTime.Now}] {userName} : {messageText}");

                // Проверка пользователя.
                if (!ALLOWED_USERS.Contains(userName))
                {
                    Console.WriteLine($"Зафиксирован пользователь без права доступа - {userName}");
                    var answer = $"User named {userName} is not included in the list of users with allowed access.";
                    await botClient.SendTextMessageAsync(messageChat, answer);

                    return;
                }

                var firstWord = string.IsNullOrEmpty(messageText) ? string.Empty :
                    messageText.Substring(0, messageText.IndexOf(' ') < 0 ? 0 : messageText.IndexOf(' '));

                if (messageText == "/help")
                {
                    var answer = "Бот поддерживает два режима: рисование и чат.\n" +
                        "Для вызова режима рисования ваш запрос должен начинаться со слова Нарисуй.\n" +
                        "Для режима Чат доступна настройка креативности ответа ИИ: /creativity_level\n" +
                        "Для установки нового KEY для доступа к API: /set_new_api_key\n" +
                        "Для установки нового URL для доступа к API: /set_new_api_url\n" +
                        "Для перезапуска бота: /restart";

                    await botClient.SendTextMessageAsync(messageChat, answer);
                }
                else if (messageText == "/restart")
                {
                    botClient.CloseAsync();
                    StartBot();
                }
                else if (messageText == "/set_new_api_key")
                {
                    await botClient.SendTextMessageAsync(messageChat, "Введите новый API KEY: ");
                    NeedSetNewApiKey = true;
                }
                else if (messageText == "/set_new_api_url")
                {
                    await botClient.SendTextMessageAsync(messageChat, "Введите новый API URL: ");
                    NeedSetNewApiUrl = true;
                }
                else if (messageText == "/creativity_level")
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData(text: "0", callbackData: "0"),
                            InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "0,1"),
                            InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "0,2"),
                            InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "0,3"),
                            InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "0,4"),
                            InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "0,5")
                        },
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "0,6"),
                            InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "0,7"),
                            InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "0,8"),
                            InlineKeyboardButton.WithCallbackData(text: "9", callbackData: "0,9"),
                            InlineKeyboardButton.WithCallbackData(text: "10", callbackData: "1")
                        }
                    });

                    await botClient.SendTextMessageAsync(messageChat, "Выберите уровень креативности ИИ:", replyMarkup: keyboard);
                }
                else if (firstWord.ToLower() == "нарисуй")
                {
                    var answer = await chatGPT.PaintWithOpenAI(messageText.Substring(firstWord.Length));
                    await botClient.SendTextMessageAsync(messageChat, answer);
                }
                else
                {
                    if (NeedSetNewApiKey)
                    {
                        NeedSetNewApiKey = false;
                        _ = new Settings(messageText, string.Empty);

                        return;
                    }

                    if (NeedSetNewApiUrl)
                    {
                        NeedSetNewApiUrl = false;
                        _ = new Settings(string.Empty, messageText);

                        return;
                    }

                    var answer = await chatGPT.ChatWithOpenAI(messageText);
                    await botClient.SendTextMessageAsync(messageChat, answer);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var openai_chat_temperature = default(double);
                var callbackData = update.CallbackQuery?.Data;
                var success = double.TryParse(callbackData, out openai_chat_temperature);

                chatGPT.OPENAI_CHAT_TEMPERATURE = success ? openai_chat_temperature : 1;
                _ = await botClient.SendTextMessageAsync(update.CallbackQuery?.Message?.Chat, "Выбран уровень креативности = " + (int)(openai_chat_temperature * 10));
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));
        }

        static void Main(string[] args)
        {
            StartBot();
        }
    }
}