using LlmTornado.Agents.API.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LlmTornado Agents API", Version = "v1" });
    // Note: XML comments file generation can be enabled in project properties if needed
});

// Add SignalR for streaming events
builder.Services.AddSignalR();

// Add response compression for SSE
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "text/event-stream" });
});

// Add CORS to allow web clients to connect
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<IChatRuntimeService, ChatRuntimeService>();


// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();

// Map controllers
app.MapControllers();


await app.RunAsync();

