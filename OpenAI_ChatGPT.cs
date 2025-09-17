using myChatGptTelegramBot;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace TelegramBot_Chat_GPT
{
    internal class OpenAI_ChatGPT
    {
        public string? MODEL_NAME { get; set; }
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

        private void InitChatGPT(Settings? settings)
        {
            MODEL_NAME = "gpt-5";

            if (settings == null)
            {
                Console.WriteLine("Инициализация ChatGPT прервана по причине: не удалось получить данные из файла настроек.");
                return;
            }

            API = new(model: MODEL_NAME,
                      credential: new ApiKeyCredential(settings.OPENAI_API_KEY),
                      options: new OpenAIClientOptions() { Endpoint = new Uri(settings.OPENAI_API_URL) });
        }

        public async Task<string?> ChatWithOpenAI(string question)
        {
            if (API == null || string.IsNullOrWhiteSpace(question))
                return null;

            var response = string.Empty;
            var errorResponseCount = 0;

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
                    Console.WriteLine($"\n{ex.Message}\n");
                    errorResponseCount++;
                }
            }

            return string.IsNullOrEmpty(response) ? "В данный момент сервис OpenAI не может обработать ваш запрос." : response;
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
