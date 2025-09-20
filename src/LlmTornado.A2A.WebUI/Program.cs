using LlmTornado.A2A.WebUI.Components;
using LlmTornado.A2A.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register services
builder.Services.AddScoped<IA2AApiService, A2AApiService>();
builder.Services.AddScoped<SSEStreamingService>();

// Add HttpClient for API calls
builder.Services.AddHttpClient<IA2AApiService, A2AApiService>(client =>
{
    // Configure the base address for the A2A Hosting API
    // This should be configurable via appsettings
    var a2aApiUrl = builder.Configuration.GetValue<string>("A2AApiBaseUrl") ?? "http://localhost:5000";
    client.BaseAddress = new Uri(a2aApiUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Allow for longer streaming operations
});



// Add CORS for cross-origin requests if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

app.UseCors();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
