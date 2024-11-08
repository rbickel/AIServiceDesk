#pragma warning disable SKEXP0001

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins;

namespace AILogic
{
    public class AIAssistant : INotifyPropertyChanged
    {

        private readonly AISettings settings;

        public AIAssistant(IOptions<AISettings> settings)
        {
            this.settings = settings.Value;
        }  

        public AIAssistant(AISettings settings)
        {
            this.settings = settings;
        }  

        private string llmOutput = string.Empty;

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

        public async Task<string> AddUserMessage(string message)
        {
            System.Console.Write("User > ");
            ChatMessages.AddUserMessage(message);
            return await AssistAsync();
        }

        public async Task<string> AssistAsync()
        {
            // reset the output
            llmOutput = string.Empty;

            // Create kernel.
            IKernelBuilder builder = Kernel.CreateBuilder();

    
            builder.AddAzureOpenAIChatCompletion(settings.Model, settings.ApiEndpoint, settings.ApiKey);

            // Add the WarrantyPlugin to the kernel
            builder.Plugins.AddFromType<HelpdeskPlugin>();

            Kernel kernel = builder.Build();

            // Chat history to keep track of the conversation.
            if (ChatMessages == null || ChatMessages.Count == 0)
            {
                ChatMessages = new ChatHistory(settings.Prompt);
            }
            
            // Retrieve the chat completion service from the kernel
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            Console.WriteLine("======== Use automated function calling ========");
            {
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

                return llmOutput;
            }
        }
    }
}

#pragma warning restore SKEXP0001