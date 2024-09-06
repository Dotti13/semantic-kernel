﻿// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agents;

/// <summary>
/// Demonstrate creation of <see cref="ChatCompletionAgent"/> and
/// eliciting its response to three explicit user messages.
/// </summary>
public class ChatCompletion_Streaming(ITestOutputHelper output) : BaseAgentsTest(output)
{
    private const string ParrotName = "Parrot";
    private const string ParrotInstructions = "Repeat the user message in the voice of a pirate and then end with a parrot sound.";

    [Fact]
    public async Task UseStreamingChatCompletionAgentAsync()
    {
        // Define the agent
        ChatCompletionAgent agent =
            new()
            {
                Name = ParrotName,
                Instructions = ParrotInstructions,
                Kernel = this.CreateKernelWithChatCompletion(),
            };

        ChatHistory chat = [];

        // Respond to user input
        await InvokeAgentAsync(agent, chat, "Fortune favors the bold.");
        await InvokeAgentAsync(agent, chat, "I came, I saw, I conquered.");
        await InvokeAgentAsync(agent, chat, "Practice makes perfect.");
    }

    [Fact]
    public async Task UseStreamingChatCompletionAgentWithPluginAsync()
    {
        const string MenuInstructions = "Answer questions about the menu.";

        // Define the agent
        ChatCompletionAgent agent =
            new()
            {
                Name = "Host",
                Instructions = MenuInstructions,
                Kernel = this.CreateKernelWithChatCompletion(),
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };

        // Initialize plugin and add to the agent's Kernel (same as direct Kernel usage).
        KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
        agent.Kernel.Plugins.Add(plugin);

        ChatHistory chat = [];

        // Respond to user input
        await InvokeAgentAsync(agent, chat, "What is the special soup?");
        await InvokeAgentAsync(agent, chat, "What is the special drink?");
    }

    // Local function to invoke agent and display the conversation messages.
    private async Task InvokeAgentAsync(ChatCompletionAgent agent, ChatHistory chat, string input)
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.Add(message);
        this.WriteAgentChatMessage(message);

        StringBuilder builder = new();
        await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(chat))
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                continue;
            }

            if (builder.Length == 0)
            {
                Console.WriteLine($"# {response.Role} - {response.AuthorName ?? "*"}:");
            }

            Console.WriteLine($"\t > streamed: '{response.Content}'");
            builder.Append(response.Content);
        }

        if (builder.Length > 0)
        {
            // Display full response and capture in chat history
            ChatMessageContent response = new(AuthorRole.Assistant, builder.ToString()) { AuthorName = agent.Name };
            chat.Add(response);
            this.WriteAgentChatMessage(response);
        }
    }

    public sealed class MenuPlugin
    {
        [KernelFunction, Description("Provides a list of specials from the menu.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
        public string GetSpecials()
        {
            return @"
Special Soup: Clam Chowder
Special Salad: Cobb Salad
Special Drink: Chai Tea
";
        }

        [KernelFunction, Description("Provides the price of the requested menu item.")]
        public string GetItemPrice(
            [Description("The name of the menu item.")]
        string menuItem)
        {
            return "$9.99";
        }
    }
}
