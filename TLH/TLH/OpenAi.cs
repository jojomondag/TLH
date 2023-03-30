using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace TLH
{
    internal class OpenAi
    {
        async public Task<string> ConnectAsync()
        {
            // Hämtar API-nyckeln från miljövariabeln "OPENAI_API_KEY".
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                // Kastar undantag om miljövariabeln inte är satt.
                throw new Exception("OPENAI_API_KEY environment variable is not set.");
            }

            // Skapar en OpenAI-tjänst med API-nyckeln.
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            // Skapar en ChatCompletionCreateRequest med en lista av ChatMessage-objekt.
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    // Systemmeddelande som informerar användaren om att det är en hjälpfull assistent.
                    ChatMessage.FromSystem("You are a helpful assistant."),
                    // Användarmeddelande som frågar om vem som vann World Series 2020.
                    ChatMessage.FromUser("Who won the world series in 2020?"),
                    // Assistentmeddelande som svarar på användarens fråga.
                    ChatMessage.FromAssistant("The Los Angeles Dodgers won the World Series in 2020."),
                    // Användarmeddelande som frågar var World Series 2020 spelades.
                    ChatMessage.FromUser("Where was it played?")
                },
                // Modellen som ska användas för att generera svaret.
                Model = Models.ChatGpt3_5Turbo,
                // Maximalt antal tokens som kan genereras i svaret (valfritt).
                MaxTokens = 50
            });

            if (completionResult.Successful)
            {
                // Returnerar det första valet av svar från ChatCompletionResult-objektet.
                return completionResult.Choices.First().Message.Content;
            }
            else
            {
                // Kastar undantag om det inte gick att generera ett svar från AI-chatboten.
                throw new Exception("Chat completion request failed: " + completionResult.Error);
            }
        }
    }
}
