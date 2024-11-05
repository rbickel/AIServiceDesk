using AIServiceDesk.Components;


// initialize AI model
bool useAzureOpenAI = true;
Settings.LoadAzureEndpoint(useAzureOpenAI);
Settings.LoadModel(useAzureOpenAI);
Settings.LoadApiKey(useAzureOpenAI);
Console.WriteLine();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
            {
                options.MaximumReceiveMessageSize = 2048 * 1024;
            });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();