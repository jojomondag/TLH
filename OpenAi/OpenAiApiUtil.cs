using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using TLH.Data;
using TLH.Utilities;

namespace TLH.OpenAi
{
    public class OpenAiApiUtil
    {
        public static IOpenAIService Connect()
        {
            // Get the API key from the environment variable "OPENAI_API_KEY".
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                // Throw an exception if the environment variable is not set.
                throw new Exception("OPENAI_API_KEY environment variable is not set.");
            }

            // Create an OpenAI service with the API key.
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            return openAiService;
        }

        public static async Task RunSimpleCompletionStreamTest(IOpenAIService sdk)
        {
            ConsoleUtil.WriteLine("Chat Completion Stream Testing is starting:", ConsoleColor.Cyan);
            try
            {
                ConsoleUtil.WriteLine("Chat Completion Stream Test:", ConsoleColor.DarkCyan);
                var completionResult = sdk.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                {
                    new(StaticValues.ChatMessageRoles.System, PromptAnalysis.TeacherFörhållningsätt),
                    new(StaticValues.ChatMessageRoles.Assistant, PromptAnalysis.Uppgiften),
                    new(StaticValues.ChatMessageRoles.User, PromptAnalysis.ElevensInlämnadeUppgift),
                },
                    MaxTokens = 3760,
                    Model = Models.ChatGpt3_5Turbo
                });

                await foreach (var completion in completionResult)
                {
                    if (completion.Successful)
                    {
                        Console.Write(completion.Choices.First().Message.Content);
                    }
                    else
                    {
                        if (completion.Error == null)
                        {
                            throw new Exception("Unknown Error");
                        }

                        Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                    }
                }

                Console.WriteLine("");
                Console.WriteLine("Complete");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}