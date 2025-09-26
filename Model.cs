namespace myChatGptTelegramBot
{
    internal class Model
    {
        public abstract record ParsedItem(string Type);

        public record Purchase(
            string? Name = null,
            decimal? Cost = null,
            string? Purpose = null // "еда", "транспорт", "подарок", "прочее"
        ) : ParsedItem("Purchase");

        public record TaskItem(
            string? Title = null,
            DateTime? Deadline = null,
            string? ReminderFrequency = null // "ежедневно", "еженедельно", "нет"
        ) : ParsedItem("Task");
    }
}
