using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using TravelAgent.Options;
using Travlr.Search.Client;
using Travlr.Accommodations.Application.Clients.AccommodationApi;
using TravelAgent.Services;
using TravelAgent.Services.CarRental.CarRentalClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMcpServer(opt =>
{
    opt.ServerInfo = new Implementation
    {
        Name = "Travlr Booking Assistant",
        Version = "1.0.0"
    };
})
.WithHttpTransport()
.WithToolsFromAssembly()
.WithResourcesFromAssembly()
.WithPromptsFromAssembly();

builder.Services.AddHttpClient("ImageProxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
});

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
builder.Services.AddHttpClient("Accommodation", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AccommodationService:BaseUrl"]
                                 ?? "https://webapi-accommodation.odin.travlr.com");
});
builder.Services.AddScoped<IAccommodationApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("Accommodation");
    var baseUrl = builder.Configuration["AccommodationService:BaseUrl"]
                  ?? "https://webapi-accommodation.odin.travlr.com";
    return new AccommodationApiClient(baseUrl, http);
});
builder.Services.AddHttpClient("Widget");
builder.Services.AddScoped<IHotelService, HotelService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

#region CarRental
builder.Services.Configure<CarRentalOption>(configuration.GetSection("CarRental"));

// Register HttpClient for Car Rental
builder.Services.AddHttpClient("CarRentalClient", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CarRentalOption>>().Value;
    client.BaseAddress = new Uri(options.Endpoint);
});

// Register the CarRental Client
builder.Services.AddScoped<TravelAgent.Services.CarRental.CarRentalClient.Client>(sp => 
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("CarRentalClient");
    var options = sp.GetRequiredService<IOptions<CarRentalOption>>().Value;
    
    return new Client(options.Endpoint, httpClient);
});

#endregion

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

app.MapMcp("/mcp");

app.Run();
