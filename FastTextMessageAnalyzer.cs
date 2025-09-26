using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.RegularExpressions;
using static myChatGptTelegramBot.Model;

public class FastTextMessageAnalyzer : IDisposable
{
    private readonly MLContext _mlContext;
    private readonly Dictionary<string, float[]> _embeddings;
    private ITransformer? _mainClassifier;
    private ITransformer? _purposePredictor;
    private ITransformer? _reminderPredictor;

    // Регулярки (остаются без изменений)
    private static readonly Regex CostRegex = new(@"(?:стоит|цена|за|купил|купила|заплатил|потратил)\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"\b(\d{1,2}[./-]\d{1,2}(?:[./-]\d{2,4})?|\d{4}-\d{2}-\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex PurchaseKeywords = new(@"\b(купил|купила|заказал|заказала|приобрел|приобрела|покупка|потратил)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TaskKeywords = new(@"\b(задача|сделать|нужно|дедлайн|срок|напомни|встреч|отчёт)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public FastTextMessageAnalyzer(string fastTextPath)
    {
        _mlContext = new MLContext(seed: 42);
        Console.WriteLine("Загрузка FastText эмбеддингов...");
        _embeddings = LoadFastTextEmbeddings(fastTextPath);
        Console.WriteLine($"Загружено {_embeddings.Count:N0} слов.");
    }

    // === ЗАГРУЗКА EMBEDDINGS (без изменений) ===
    private static Dictionary<string, float[]> LoadFastTextEmbeddings(string path)
    {
        var embeddings = new Dictionary<string, float[]>();
        var lines = File.ReadLines(path).Skip(1);
        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 301) continue;
            var word = parts[0];
            var vector = new float[300];
            for (int i = 0; i < 300; i++)
                vector[i] = float.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture);
            embeddings[word] = vector;
        }
        return embeddings;
    }

    // === ВЕКТОРИЗАЦИЯ ТЕКСТА (без изменений) ===
    private float[] GetSentenceEmbedding(string text)
    {
        var words = text.ToLower()
                       .Split(new char[] { ' ', ',', '.', '!', '?', '\t', '\n', ':', ';' },
                              StringSplitOptions.RemoveEmptyEntries);
        var validVectors = new List<float[]>();
        foreach (var word in words)
        {
            if (_embeddings.TryGetValue(word, out var vec))
                validVectors.Add(vec);
        }
        if (validVectors.Count == 0)
            return new float[300];
        var avg = new float[300];
        foreach (var vec in validVectors)
            for (int i = 0; i < 300; i++)
                avg[i] += vec[i];
        for (int i = 0; i < 300; i++)
            avg[i] /= validVectors.Count;
        return avg;
    }

    // === НОВЫЙ ПОДХОД: ЗАГРУЗКА ИЗ CSV + ДОБАВЛЕНИЕ ВЕКТОРОВ ===
    public void TrainMainClassifier(string csvPath)
    {
        // 1. Загружаем текст и метку
        var dataView = _mlContext.Data.LoadFromTextFile<TextRow>(csvPath, hasHeader: true, separatorChar: ',');

        // 2. Преобразуем в список, добавляем векторы
        var enumerable = _mlContext.Data.CreateEnumerable<TextRow>(dataView, reuseRowObject: false);
        var withVectors = enumerable.Select(row => new MainInput
        {
            Features = GetSentenceEmbedding(row.Text),
            Label = row.Label
        });

        var dataViewWithVectors = _mlContext.Data.LoadFromEnumerable(withVectors);

        // 3. Обучаем с MapValueToKey
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _mainClassifier = pipeline.Fit(dataViewWithVectors);
        Console.WriteLine("Основная модель обучена.");
    }

    public void TrainPurposePredictor(string csvPath)
    {
        var dataView = _mlContext.Data.LoadFromTextFile<PurposeRow>(csvPath, hasHeader: true, separatorChar: ',');
        var enumerable = _mlContext.Data.CreateEnumerable<PurposeRow>(dataView, reuseRowObject: false);
        var withVectors = enumerable.Select(row => new PurposeInput
        {
            Features = GetSentenceEmbedding(row.Text),
            Label = row.Purpose
        });

        var dataViewWithVectors = _mlContext.Data.LoadFromEnumerable(withVectors);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _purposePredictor = pipeline.Fit(dataViewWithVectors);
        Console.WriteLine("Модель назначения покупки обучена.");
    }

    public void TrainReminderPredictor(string csvPath)
    {
        var dataView = _mlContext.Data.LoadFromTextFile<ReminderRow>(csvPath, hasHeader: true, separatorChar: ',');
        var enumerable = _mlContext.Data.CreateEnumerable<ReminderRow>(dataView, reuseRowObject: false);
        var withVectors = enumerable.Select(row => new ReminderInput
        {
            Features = GetSentenceEmbedding(row.Text),
            Label = row.Frequency
        });

        var dataViewWithVectors = _mlContext.Data.LoadFromEnumerable(withVectors);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _reminderPredictor = pipeline.Fit(dataViewWithVectors);
        Console.WriteLine("Модель периодичности напоминаний обучена.");
    }

    // === ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ CSV ===
    private class TextRow
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Label { get; set; } = string.Empty;
    }

    private class PurposeRow
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Purpose { get; set; } = string.Empty;
    }

    private class ReminderRow
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Frequency { get; set; } = string.Empty;
    }

    // === КЛАССЫ ДЛЯ ВЕКТОРИЗОВАННЫХ ДАННЫХ ===
    private class MainInput
    {
        [VectorType(300)]
        public float[] Features { get; set; } = new float[300];
        public string Label { get; set; } = string.Empty;
    }

    private class PurposeInput
    {
        [VectorType(300)]
        public float[] Features { get; set; } = new float[300];
        public string Label { get; set; } = string.Empty;
    }

    private class ReminderInput
    {
        [VectorType(300)]
        public float[] Features { get; set; } = new float[300];
        public string Label { get; set; } = string.Empty;
    }

    // === ПРЕДСКАЗАНИЯ (без изменений) ===
    private string PredictMainCategory(string message)
    {
        if (_mainClassifier == null)
            throw new InvalidOperationException("Основная модель не обучена!");
        
        var input = new MainInput { Features = GetSentenceEmbedding(message) };
        var pred = _mlContext.Model.CreatePredictionEngine<MainInput, MainPrediction>(_mainClassifier);
        
        return pred.Predict(input).PredictedLabel;
    }

    private string PredictPurpose(string message)
    {
        if (_purposePredictor == null)
            return "прочее";
        
        var input = new PurposeInput { Features = GetSentenceEmbedding(message) };
        var pred = _mlContext.Model.CreatePredictionEngine<PurposeInput, PurposePrediction>(_purposePredictor);
        
        return pred.Predict(input).PredictedLabel;
    }

    private string PredictReminder(string message)
    {
        if (_reminderPredictor == null)
            return "нет";

        var input = new ReminderInput { Features = GetSentenceEmbedding(message) };
        var pred = _mlContext.Model.CreatePredictionEngine<ReminderInput, ReminderPrediction>(_reminderPredictor);
        
        return pred.Predict(input).PredictedLabel;
    }

    private class MainPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }

    private class PurposePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }

    private class ReminderPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }

    // === ИЗВЛЕЧЕНИЕ ДАННЫХ (без изменений) ===
    internal ParsedItem? Analyze(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;
        
        var category = PredictMainCategory(message);
        return category.ToLower() switch
        {
            "purchase" or "покупка" => ExtractPurchase(message),
            "task" or "задача" => ExtractTask(message),
            _ => null
        };
    }

    private Purchase ExtractPurchase(string message)
    {
        string? name = null;
        decimal? cost = null;
        
        var costMatch = CostRegex.Match(message);
        if (costMatch.Success && decimal.TryParse(costMatch.Groups[1].Value.Replace(',', '.'), out var c))
            cost = c;
        
        var words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (PurchaseKeywords.IsMatch(words[i]))
            {
                name = words[i + 1];
                break;
            }
        }
        
        var purpose = PredictPurpose(message);
        
        return new Purchase(name, cost, purpose);
    }

    private TaskItem ExtractTask(string message)
    {
        string? title = null;
        DateTime? deadline = null;
        
        var dateMatch = DateRegex.Match(message);
        if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var dt))
            deadline = dt;
        
        var clean = TaskKeywords.Replace(message, "").Trim();
        clean = DateRegex.Replace(clean, "").Trim();
        
        var words = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        title = string.Join(" ", words.Take(6));
        
        var reminder = PredictReminder(message);
        if (reminder == "нет") 
            reminder = null;

        return new TaskItem(title, deadline, reminder);
    }

    public void Dispose() { }
}