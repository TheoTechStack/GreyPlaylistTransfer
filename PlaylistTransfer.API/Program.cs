using PlaylistTransfer.API.Agents;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton(_ => 
    new AzureKeyVaultService(builder.Configuration["AzureKeyVaultUri"]));
builder.Services.AddScoped<SpotifyCredentialsProvider>();
builder.Services.AddScoped<SpotifyService>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();


var app = builder.Build();

app.UseRouting();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();
