using Microsoft.ML;
using myChatGptTelegramBot;
using myChatGptTelegramBot.DTO_Models;
using myChatGptTelegramBot.Services;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot_Chat_GPT
{
    class Program
    {
        private static OpenAI_ChatGPT? ChatGPT = default;
        private static bool NeedSetNewModel = false;
        private static bool NeedSetNewApiKey = false;
        private static bool NeedSetNewApiUrl = false;
        private static bool NeedCheckAdminPassword = false;
        private static bool IsAdmin = false;
        private static bool IsTrained = false;
        private static List<string>? ALLOWED_USERS { get; set; }
        private static FastTextMessageAnalyzer Analyzer = default;
        private static AppDbContext DB = default;
        private static PurchaseService PurchaseSrv = default;
        private static TaskItemService TaskItemSrv = default;

        private static void StartBot()
        {
            #region Полчение данных из файла настроек.

            var settings = new Settings();
            if (settings == null)
            {
                Console.WriteLine("Не удалось получить объект с данными файла настроек.");
                return;
            }

            var botToken = settings.BOT_TOKEN;
            if (string.IsNullOrEmpty(botToken))
            {
                Console.WriteLine("Не удалось получить токен для подключения к боту.");
                return;
            }

            ALLOWED_USERS = settings?.ALLOWED_USERS;
            if (ALLOWED_USERS == null || ALLOWED_USERS.Count == 0)
            {
                Console.WriteLine("Не удалось получить список пользователей с правом доступа к боту.");
                return;
            }

            #endregion

            #region Инициализация эмбендингов.

            try
            {
                Analyzer = new FastTextMessageAnalyzer("cc.ru.300.vec");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникла ошибка при инициализации эмбендингов:\n{0}", ex);
            }

            #endregion

            #region Инициализация БД.

            try
            {
                DB = new AppDbContext();
                DB.Database.EnsureCreatedAsync();

                PurchaseSrv = new PurchaseService(DB);
                TaskItemSrv = new TaskItemService(DB);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникла ошибка при инициализации БД:\n{0}", ex);
            }

            #endregion

            #region Запуск бота.

            var bot = new TelegramBotClient(botToken);
            var cancellationToken = new CancellationTokenSource().Token;

            if (bot.TestApi(cancellationToken).Result)
            {
                var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

                bot.DeleteWebhook(true);
                bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
                ChatGPT = new OpenAI_ChatGPT(settings);

                Console.WriteLine($"Запуск бота {bot.GetMe().Result.FirstName} завершен успешно.");
                Console.ReadLine();

            }
            else
            {
                Console.WriteLine("Не удалось запустить бот, проверьте корректность токена бота.");
                Console.ReadLine();
            }

            #endregion
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update?.Message == null && update?.CallbackQuery == null)
                return;

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var messageText = message?.Text;
                var messageChat = message?.Chat;
                var userName = messageChat?.Username;

                // Логирование всех входящих сообщений, кроме пароля администратора.
                if (!NeedCheckAdminPassword)
                    Console.WriteLine($"[{DateTime.Now}] {userName} : {messageText}");

                // Проверка пользователя.
                if (ALLOWED_USERS == null || ALLOWED_USERS.Count == 0 || !ALLOWED_USERS.Contains(userName))
                {
                    Console.Write("Зафиксирован пользователь без права доступа - ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{userName}\n");
                    Console.ResetColor();
                    var answer = $"User named {userName} is not included in the list of users with allowed access.";
                    await botClient.SendMessage(messageChat, answer);

                    return;
                }

                var firstWord = string.IsNullOrEmpty(messageText) ? string.Empty :
                    messageText.Substring(0, messageText.IndexOf(' ') < 0 ? 0 : messageText.IndexOf(' '));

                if (messageText == "/help")
                {
                    var userPartForHelp = string.Empty;
                    //"Бот создан для использования языковой модели ChatGPT.\n" +
                    //"Для получения имени текущей языковой модели: /get_current_model\n" +
                    //"Для установки новой языковой модели: /set_new_model\n";
                    //"Для вызова режима рисования ваш запрос должен начинаться со слова Нарисуй.\n" +
                    //"Для режима Чат доступна настройка креативности ответа ИИ: /creativity_level\n";
                    var adminPartForHelp =
                        "Для установки нового KEY для доступа к API: /set_new_api_key\n" +
                        "Для установки нового URL для доступа к API: /set_new_api_url\n" +
                        "Для очистки консоли бота: /clear_console\n" +
                        "Для перезапуска бота: /restart\n" +
                        "Для понижения уровня доступа: /user" +
                        "Для тренировки моделей анализа: /train";
                    var answer = IsAdmin ? userPartForHelp + adminPartForHelp : userPartForHelp;

                    await botClient.SendMessage(messageChat, answer);
                }
                else if (messageText == "/get_tasks")
                {
                    var tasks = TaskItemSrv.GetAllAsync().Result;
                    foreach (var task in tasks)
                    {
                        var answer = string.Empty;

                        try
                        {
                            answer = Newtonsoft.Json.JsonConvert.SerializeObject(task, Formatting.Indented, new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.Default });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                        if (!string.IsNullOrWhiteSpace(answer))
                            await botClient.SendMessage(messageChat, answer);
                    }
                }
                else if (messageText == "/get_purchases")
                {
                    var purchases = PurchaseSrv.GetAllAsync().Result;
                    foreach (var purchase in purchases)
                    {
                        var answer = string.Empty;

                        try
                        {
                            answer = Newtonsoft.Json.JsonConvert.SerializeObject(purchase, Formatting.Indented, new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.Default });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                        if (!string.IsNullOrWhiteSpace(answer))
                            await botClient.SendMessage(messageChat, answer);
                    }
                }
                else if (messageText == "/train")
                {
                    if (Analyzer == null)
                    {
                        var answer = "";
                        await botClient.SendMessage(messageChat, answer);
                        return;
                    }

                    try
                    {
                        // Обучение моделей
                        Analyzer.TrainMainClassifier("MainTrainData.csv");
                        Analyzer.TrainPurposePredictor("PurposeTrainData.csv");
                        Analyzer.TrainReminderPredictor("ReminderTrainData.csv");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Возникла ошибка при инициализации эмбендингов:\n{0}", ex);
                        return;
                    }

                    await botClient.SendMessage(messageChat, "Модели для анализа успешно обучены.");
                    IsTrained = true;
                }
                else if (messageText == "/get_current_model")
                {
                    await botClient.SendMessage(messageChat, $"В данный момент используется языковая модель - {ChatGPT?.MODEL_NAME}");
                    return;
                }
                else if (messageText == "/set_new_model")
                {
                    await botClient.SendMessage(messageChat, "Укажите название новой языковой модели.");
                    NeedSetNewModel = true;

                    return;
                }
                else if (messageText == "/clear_console")
                {
                    if (IsAdmin)
                    {
                        Console.Clear();
                        await botClient.SendMessage(messageChat, "Консоль бота была очищена!");
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Недостаточный уровень доступа для выполнение данной операции!");
                    }

                    return;
                }
                else if (messageText == "/admin")
                {
                    if (IsAdmin)
                    {
                        await botClient.SendMessage(messageChat, "Уровень доступа 'Администратор' уже активирован.");
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Введите пароль администратора: ");
                        NeedCheckAdminPassword = true;
                    }

                    return;
                }
                else if (messageText == "/user")
                {
                    if (IsAdmin)
                    {
                        IsAdmin = false;
                        await botClient.SendMessage(messageChat, "Активирован уровень доступа 'Пользователь'.");
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Уровень доступа 'Пользователь' уже активирован.");
                    }

                    return;
                }
                else if (messageText == "/restart")
                {
                    if (IsAdmin)
                    {
                        await botClient.SendMessage(messageChat, "Иницирован перезапуск телеграмм бота!");
                        botClient.Close();
                        StartBot();
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Недостаточный уровень доступа для выполнение данной операции!");
                    }

                    return;
                }
                else if (messageText == "/set_new_api_key")
                {
                    if (IsAdmin)
                    {
                        await botClient.SendMessage(messageChat, "Введите новый API KEY: ");
                        NeedSetNewApiKey = true;
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Недостаточный уровень доступа для выполнение данной операции!");
                    }

                    return;
                }
                else if (messageText == "/set_new_api_url")
                {
                    if (IsAdmin)
                    {
                        await botClient.SendMessage(messageChat, "Введите новый API URL: ");
                        NeedSetNewApiUrl = true;
                    }
                    else
                    {
                        await botClient.SendMessage(messageChat, "Недостаточный уровень доступа для выполнение данной операции!");
                    }

                    return;
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

                    await botClient.SendMessage(messageChat, "Выберите уровень креативности ИИ:", replyMarkup: keyboard);
                }
                else if (firstWord.ToLower() == "нарисуй")
                {
                    var answer = await ChatGPT.PaintWithOpenAI(messageText.Substring(firstWord.Length));
                    await botClient.SendMessage(messageChat, answer);
                }
                else
                {
                    if (NeedSetNewModel)
                    {
                        if (string.IsNullOrWhiteSpace(messageText))
                        {
                            await botClient.SendMessage(messageChat, "Введено некорректное имя модели! Введите корректное имя новой языковой модели.");
                            return;
                        }

                        NeedSetNewModel = false;
                        ChatGPT.InitNewModel(messageText);

                        return;
                    }

                    if (NeedCheckAdminPassword)
                    {
                        NeedCheckAdminPassword = false;
                        var settings = new Settings();
                        IsAdmin = settings.CheckAdminPassword(messageText);

                        if (IsAdmin)
                            await botClient.SendMessage(messageChat, "Активирован уровень доступа 'Администратор'.");
                        else
                            await botClient.SendMessage(messageChat, "Введен неверный пароль администратора!");

                        botClient.DeleteMessage(messageChat.Id, message.MessageId);

                        return;
                    }

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

                    if (Analyzer == null || !IsTrained)
                    {
                        await botClient.SendMessage(messageChat, "Модели анализа не обучены или не существуют, попробуйте запустить обучение моделей.");
                        return;
                    }

                    var answer = string.Empty;

                    try
                    {
                        // Анализ
                        var analyzeResult = Analyzer.Analyze(messageText);
                        if (analyzeResult is Model.Purchase)
                        {
                            var purchaseResult = (Model.Purchase)analyzeResult;
                            var newPurchase = new Purchase()
                            {
                                Name = purchaseResult.Name,
                                Cost = purchaseResult.Cost,
                                Purpose = purchaseResult.Purpose
                            };

                            await PurchaseSrv.CreateAsync(newPurchase);
                            answer = $"Создана покупка: {purchaseResult.Name}";
                        }
                        else if (analyzeResult is Model.TaskItem)
                        {
                            var taskResult = (Model.TaskItem)analyzeResult;
                            var newTask = new TaskItem()
                            {
                                Title = taskResult.Title,
                                Deadline = taskResult.Deadline,
                                ReminderFrequency = taskResult.ReminderFrequency
                            };

                            await TaskItemSrv.CreateAsync(newTask);
                            answer = $"Создана задача: {taskResult.Title}";
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendMessage(messageChat, "Возникла ошибка в процессе анализа.");
                        return;
                    }

                    await botClient.SendMessage(messageChat, answer);

                    //var answer = await ChatGPT.ChatWithOpenAI(messageText);
                    //var answerParts = answer?.Split(new string[] { ".\n", ". " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    //foreach (var part in answerParts)
                    //    await botClient.SendMessage(messageChat, $"{part}.");
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var openai_chat_temperature = default(double);
                var callbackData = update.CallbackQuery?.Data;
                var success = double.TryParse(callbackData, out openai_chat_temperature);

                ChatGPT.OPENAI_CHAT_TEMPERATURE = success ? openai_chat_temperature : 1;
                _ = await botClient.SendMessage(update.CallbackQuery?.Message?.Chat, "Выбран уровень креативности = " + (int)(openai_chat_temperature * 10));
            }
        }

        private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));
        }

        public static void Main(string[] args)
        {
            StartBot();
        }
    }
}