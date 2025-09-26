using myChatGptTelegramBot;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace TelegramBot_Chat_GPT
{
    internal class OpenAI_ChatGPT
    {
        public string? MODEL_NAME { get; set; }
        private string? OPENAI_API_KEY { get; set; }
        private string? OPENAI_API_URL { get; set; }
        public double? OPENAI_CHAT_TEMPERATURE { get; set; }
        private ChatClient? API = default;

        public OpenAI_ChatGPT()
        {
            InitChatGPT(new Settings());
        }

        public OpenAI_ChatGPT(Settings? settings)
        {
            InitChatGPT(settings);
        }

        private void InitChatGPT(Settings? settings, string? modelName = null)
        {
            MODEL_NAME = modelName ?? "unknown_model";
            OPENAI_API_KEY = settings?.OPENAI_API_KEY ?? OPENAI_API_KEY ?? "empty_api_key";
            OPENAI_API_URL = settings?.OPENAI_API_URL ?? OPENAI_API_URL ?? "empty_api_url";

            if (OPENAI_API_KEY == "empty_api_key" || OPENAI_API_URL == "empty_api_url")
            {
                Console.WriteLine("Инициализация ChatGPT прервана по причине: не удалось получить данные из файла настроек.");
                return;
            }

            API = new(model: MODEL_NAME,
                      credential: new ApiKeyCredential(OPENAI_API_KEY),
                      options: new OpenAIClientOptions() { Endpoint = new Uri(OPENAI_API_URL) });
        }

        public void InitNewModel(string modelName)
        {
            InitChatGPT(null, modelName);
        }

        public async Task<string?> ChatWithOpenAI(string question)
        {
            if (API == null || string.IsNullOrWhiteSpace(question))
                return null;

            var response = string.Empty;
            var errorResponseCount = 0;
            var errors = new List<string>();

            while (string.IsNullOrWhiteSpace(response) && errorResponseCount < 10)
            {
                try
                {
                    var chatMessage = ChatMessage.CreateUserMessage(question);
                    var chatResult = await API.CompleteChatAsync(chatMessage);
                    var resultContent = chatResult?.Value?.Content;

                    if (resultContent != null)
                        foreach (var chatChoice in resultContent)
                        {
                            response += chatChoice.Text;
                        }
                }
                catch (Exception ex)
                {
                    errorResponseCount++;
                    if (ex.Message != errors.LastOrDefault())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Ошибка!");
                        Console.ResetColor();
                        Console.WriteLine($" {ex.Message}");
                        errors.Add(ex.Message);
                    }
                }
            }

            return (string.IsNullOrEmpty(response) ? null : response) ?? 
                (errors.Count == 0 ? null : string.Join('\n', errors)) ?? 
                "В данный момент сервис OpenAI не может обработать ваш запрос.";
        }

        public async Task<string> PaintWithOpenAI(string question)
        {
            return "В данный момент бот не имеет возможности отправлять запросы для получения изображений.";

            //if (API == null)
            //    return null;

            //var response = default(ImageResult);

            //try
            //{
            //    response = await API.ImageGenerations
            //    .CreateImageAsync(new OpenAI_API.Images.ImageGenerationRequest
            //    {
            //        Prompt = question,
            //        Size = OpenAI_API.Images.ImageSize._1024,
            //    });
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            //return response.ToString() ?? "В данный момент сервис OpenAI не может обработать ваш запрос.";
        }
    }
}