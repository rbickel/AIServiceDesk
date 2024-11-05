#pragma warning disable SKEXP0001

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins;

namespace AILogic
{
    public class AIAssistant : INotifyPropertyChanged
    {
        private string llmOutput = "";

        // observable string property to store the chat messages
        private ChatHistory chatMessages = new ChatHistory();
        public ChatHistory ChatMessages
        {
            get { return chatMessages; }
            set
            {
                chatMessages = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChatMessages)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task AddUserMessage(string message)
        {
            System.Console.Write("User > ");
            chatMessages.AddUserMessage(message);
            await AssistAsync();
        }

        public async Task AssistAsync()
        {
            // Create kernel.
            IKernelBuilder builder = Kernel.CreateBuilder();

            var (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = Settings.LoadFromFile();
            builder.AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey);

            // Add the WarrantyPlugin to the kernel
            builder.Plugins.AddFromType<HelpdeskPlugin>();

            Kernel kernel = builder.Build();

            // Chat history to keep track of the conversation.
            ChatMessages = new ChatHistory("""You are a friendly assistant who likes to follow the rules. You will complete the required steps. You are autonomous and can complete tasks without user input. When missing information, search for any clues in the conversation.""");

            // use this prompt for asking for user input
            // ChatHistory chatMessages = new ChatHistory("""You are a friendly assistant who likes to follow the rules. You will complete required steps and request approval before taking any consequential actions. If the user doesn't provide enough information for you to complete a task, you will keep asking questions until you have enough information to complete the task.""");

            // Retrieve the chat completion service from the kernel
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            Console.WriteLine("======== Use automated function calling ========");
            {
                if (ChatMessages.Count == 0)
                {
                    ChatMessages.AddUserMessage(@"You are a courteous, helpful Helpdesk AI assistant, here to assist users with password resets and other account-related issues. Begin each interaction with a warm greeting, such as: 'Hello! How can I help you today?' or 'Hi there! I'm here to assist you with any issues you may have.'

When a user mentions they need help with their password, start by kindly asking for the email address associated with their account. For example: 'Could you please provide the email address linked to your account?' Once you have the email, ask the user to describe the issue in a bit more detail to ensure you understand the full context (e.g., 'Could you tell me a bit more about what happened?').

To confirm their identity, if needed, politely request any additional information, such as their username or employee ID, and reassure the user that this is for security purposes.

After confirming the necessary information, activate the Helpdesk Plugin's `ResetPassword` function to initiate the reset process. Then, guide the user through each step, offering friendly and clear instructions. For instance, if they need to check their email or enter a verification code, let them know what to expect next.

After the password reset is complete, suggest that they create a strong password, reminding them of best practices like not reusing old passwords or sharing it with others. Conclude by asking if there’s anything else they need help with and wishing them a great day!");

                }

                // Get the chat completions
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    FrequencyPenalty = 1.0,
                };
                var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
                    ChatMessages,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: kernel);

                // Stream the results
                await foreach (var content in result)
                {
                    if (content.Role.HasValue)
                    {
                        Console.Write("Assistant > ");
                    }
                    Console.Write(content.Content);
                    llmOutput += content.Content;
                }
                Console.WriteLine();

                // Add the message from the agent to the chat history
                ChatMessages.AddAssistantMessage(llmOutput);

                // uncomment the following code to allow the user to provide input
                // if (fullMessage.Contains('?'))
                // {
                // If the agent asks a question, we need to provide an answer.
                // chatMessages.AddUserMessage(Console.ReadLine()!);
                // }
            }
        }
    }
}

#pragma warning restore SKEXP0001