using myChatGptTelegramBot;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Images;
using OpenAI_API.Models;
using System.Net;

namespace TelegramBot_Chat_GPT
{
    internal class OpenAI_ChatGPT
    {
        private string OPENAI_API_KEY { get; set; }
        private string OPENAI_API_URL { get; set; }
        public double? OPENAI_CHAT_TEMPERATURE { get; set; }

        private OpenAIAPI? API = default;

        public OpenAI_ChatGPT()
        {
            InitChatGPT(new Settings());
        }

        public OpenAI_ChatGPT(Settings settings)
        {
            InitChatGPT(settings);
        }

        private void InitChatGPT(Settings settings)
        {
            OPENAI_API_KEY = settings.OPENAI_API_KEY;
            OPENAI_API_URL = settings.OPENAI_API_URL + "/{0}/{1}";

            API = new OpenAIAPI(OPENAI_API_KEY);
            API.ApiUrlFormat = OPENAI_API_URL;
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
                    var chatMessage = new ChatMessage()
                    {
                        Role = ChatMessageRole.User,
                        TextContent = question
                    };

                    var request = new ChatRequest()
                    {
                        ResponseFormat = ChatRequest.ResponseFormats.JsonObject,
                        Model = Model.ChatGPTTurbo,
                        Temperature = OPENAI_CHAT_TEMPERATURE,
                        Messages = new ChatMessage[] { chatMessage }
                    };

                    await foreach (var chatResult in API.Chat.StreamChatEnumerableAsync(request).ConfigureAwait(false))
                    {
                        foreach (var choice in chatResult.Choices)
                            response += choice.Delta.TextContent;
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

            if (API == null)
                return null;

            var response = default(ImageResult);

            try
            {
                response = await API.ImageGenerations
                .CreateImageAsync(new OpenAI_API.Images.ImageGenerationRequest
                {
                    Prompt = question,
                    Size = OpenAI_API.Images.ImageSize._1024,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return response.ToString() ?? "В данный момент сервис OpenAI не может обработать ваш запрос.";
        }
    }
}
