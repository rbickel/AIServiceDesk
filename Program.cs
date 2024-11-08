using AILogic;
using Microsoft.Extensions.Options;
using AIServiceDesk.Components;
using Azure.Communication.CallAutomation;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Configuration.AddJsonFile("appsettings.{Environment}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
AISettings aiSettings = new();
ACSSettings acsSettings = new();
builder.Configuration.GetSection(nameof(AISettings)).Bind(aiSettings);
builder.Configuration.GetSection(nameof(AISettings)).Bind(acsSettings);
builder.Services
    .Configure<AISettings>(builder.Configuration.GetSection(key: nameof(AISettings)))
    .Configure<ACSSettings>(builder.Configuration.GetSection(key: nameof(ACSSettings)))
    .AddScoped<AIAssistant>()  // Add the AIAssistant to the DI container
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
            {
                options.MaximumReceiveMessageSize = 2048 * 1024;
            });

builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles().UseRouting().UseAntiforgery().UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
});
app.UseAntiforgery();
app.UseRouting();

app.Run();