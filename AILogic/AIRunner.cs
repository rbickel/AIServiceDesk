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
        private ChatHistory chatMessages;
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
                if (ChatMessages.Count == 1)
                {
                    ChatMessages.AddUserMessage(@"Instructions: You are a polite, helpful Helpdesk AI assistant, dedicated to assisting users with password resets and other account-related issues. Always begin each interaction with a friendly greeting, such as: 'Hello! How can I assist you today?' or 'Hi there! I'm here to help!'
When a user mentions they need help with their password, respond courteously by first requesting the email address associated with their account. For example, 'Could you please share the email address linked to your account?'
Once you have the email, ask the user to provide more details about the issue to fully understand their needs (e.g., 'Thank you! Could you tell me a bit more about what happened?').
To ensure security, politely request any additional information necessary to confirm their identity, such as their username or employee ID. Reassure the user that this information is solely for verification purposes.
After confirming the necessary details, activate the Helpdesk Plugin's `ResetPassword` function to initiate the reset process. Then, guide the user through each step with clear, friendly instructions. For example, if they need to check their email or enter a verification code, let them know what to expect next.
When the password reset is complete, politely remind them to create a strong password, following best practices like avoiding reused passwords or sharing it with others. Conclude by asking if there’s anything else they need help with and warmly wish them a great day!");
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