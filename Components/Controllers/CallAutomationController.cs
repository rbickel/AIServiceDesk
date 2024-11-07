using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using AILogic;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Contracts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net;


namespace AIServiceDesk.Controllers
{

    [ApiController]
    [AllowAnonymous]
    [Route("api")]
    public class CallAutomationController : ControllerBase
    {
        private readonly ACSSettings _settings;
        private readonly AISettings _aiSettings;
        private readonly CallAutomationClient _callClient;

        //InMemory "memory" baaaaaaaaad
        private Dictionary<string, AIAssistant> _sessions = new Dictionary<string, AIAssistant>();
        public CallAutomationController(IOptions<ACSSettings> settings, IOptions<AISettings> aiSettings)
        {
            this._settings = settings.Value;
            this._aiSettings = aiSettings.Value;
            this._callClient = new CallAutomationClient(this._settings.ACSConnectionString);
        }

        [HttpPost]
        [Route("incoming-call")]
        public async Task<IResult> IncomingCall([FromBody] EventGridEvent[] events)
        {
            foreach (EventGridEvent eventGridEvent in events)
            {
                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        // Do any additional validation (as required) and then return back the below response
                        var responseData = new
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };

                        return Results.Ok(responseData);
                    }
                }
                var contextId = Guid.NewGuid().ToString();

                var incomingCall = JsonSerializer.Deserialize<IncomingCall>(eventGridEvent.Data);
                var callOptions = new AnswerCallOptions(incomingCall.IncomingCallContext, 
                    new Uri($"{_settings.Host}/api/callbacks/{contextId}?callerId={WebUtility.UrlEncode(incomingCall.From.RawId)}"))
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions()
                    {
                        CognitiveServicesEndpoint = new Uri(_settings.CognitiveServicesEndpoint) 
                    }
                };
                await _callClient.AnswerCallAsync(callOptions);
            }
            return Results.Ok();
        }


        [HttpPost]
        [Route("callbacks/{contextId}")]
        public async Task Callback([FromBody] CloudEvent[] cloudEvents, [FromQuery]string callerId)
        {
            var contextId = Request.RouteValues["contextId"]?.ToString() ?? "";
            var phoneId = callerId;
            var aiAssistant = _sessions.ContainsKey(contextId) ? _sessions[contextId] : new AIAssistant(_aiSettings);

            foreach (var cloudEvent in cloudEvents)
            {
                // Parse the cloud event to get the call event details
                CallAutomationEventBase callEvent = CallAutomationEventParser.Parse(cloudEvent);
                var callConnection = _callClient.GetCallConnection(callEvent.CallConnectionId);
                var callConnectionMedia = callConnection.GetCallMedia();

                if (callEvent is CallConnected)
                {
                    // If the call is connected, get a response from the chatbot and send it to the user
                    var response = await aiAssistant.AssistAsync();
                    await SayAndRecognize(callConnectionMedia, phoneId, response);
                }
                if (callEvent is RecognizeCompleted recogEvent
                    && recogEvent.RecognizeResult is SpeechResult speech_result)
                {
                    // If speech is recognized, get a response from the chatbot based on the recognized speech and send it to the user
                    var response = await aiAssistant.AddUserMessage(speech_result.Speech);
                    await SayAndRecognize(callConnectionMedia, phoneId, response);
                }
            }
            _sessions[contextId] = aiAssistant;
        }

        // Function to send a message to the user and recognize their response
        private async Task SayAndRecognize(CallMedia callConnectionMedia, string phoneId, string response)
        {
            // Set up the text source for the chatbot's response
            var chatGPTResponseSource = new TextSource(response, "en-US-Steffan:DragonHDLatestNeural");

            String ssmlToPlay = $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-Steffan:DragonHDLatestNeural\">{response}</voice></speak>";
            var playSource = new SsmlSource(ssmlToPlay);

            //log
            Console.WriteLine(response);
            // Recognize the user's speech after sending the chatbot's response
            var recognizeOptions =
                new CallMediaRecognizeSpeechOptions(
                    targetParticipant: CommunicationIdentifier.FromRawId(phoneId))
                {
                    Prompt = playSource,
                    EndSilenceTimeout = TimeSpan.FromMilliseconds(200),
                };

            var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }
    }
}
