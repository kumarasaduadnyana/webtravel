using Microsoft.AspNetCore.HttpOverrides;
using Travlr.Search.Client;
using TravelAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("TravlrSearch", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SearchService:BaseUrl"]
                                 ?? "https://webapi-search.odin.travlr.com");
});
builder.Services.AddScoped<ITravlrSearchApiClient>(sp =>
{
    var factory  = sp.GetRequiredService<IHttpClientFactory>();
    var http     = factory.CreateClient("TravlrSearch");
    var baseUrl  = builder.Configuration["SearchService:BaseUrl"]
                   ?? "https://webapi-search.odin.travlr.com";
    var client   = new TravlrSearchApiClient(baseUrl, http);
    var apiKey   = builder.Configuration["SearchService:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.SetApiKey(apiKey);
    return client;
});
builder.Services.AddScoped<IHotelService, HotelService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin(); // tighten this later
    });
});

//builder.Services.AddHttpClient<ISearchClient, SearchClient>(client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["SearchService:BaseUrl"]);
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
