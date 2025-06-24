using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ShardedKeyValueStore>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();