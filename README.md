# AI Service Desk

AI Service Desk is an AI-powered helpdesk assistant built with C# and .NET. It provides users with an interactive assistant for handling password resets and other account-related issues.

## Features

- **AI Assistant**: An AI assistant implemented in 

AIAssistant

 that interacts with users.
- **Chat History**: Maintains conversation history using the 

ChatMessages

 property.
- **Plugin Support**: Integrates plugins like 

HelpdeskPlugin

 to extend functionality.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- OpenAI API key

### Installation

1. Clone the repository:

   ```sh
   git clone <repository-url>
   ```

2. Navigate to the project directory:

   ```sh
   cd AIServiceDesk
   ```

3. Restore dependencies:

   ```sh
   dotnet restore
   ```

### Configuration

Update the settings in 

Settings.cs

 or 

appsettings.json

 with your OpenAI API key and model information.

## Usage

Run the application:

```sh
dotnet run
```

The AI assistant will start and can assist with tasks like password resets. It maintains chat history using the 

ChatMessages

 property.

## Project Structure

- 

AILogic

 - Contains the AI assistant logic.
- 

Components

 - UI components for the application.
- 

Plugins

 - Plugins like 

HelpdeskPlugin

 for additional functionality.
- 

Program.cs

 - Main entry point of the application.
- 

Settings.cs

 - Configuration settings.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
Built by the best technical specialists in Switzerland.