using Repository.App.API;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;
using Repository.App.Database;

namespace Repository
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //DBManager.FillMetadata(); // Will fill the metadata file with all the files in Resources. REMEMBER to delete the contents of the file before


            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine("Environment.IsDevelopment");
                builder.WebHost.UseUrls("http://localhost:4001");
                //builder.WebHost.UseUrls("http://localhost:4001", "https://localhost:4000");
            }

            // Add services to the container.
            builder.Services.AddAuthorization();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors();

            // Rate Limiting with psQl: https://blog.devart.com/implementing-rate-limiting-in-asp-net-6-core-and-c.html
            // Alternative in 7.0:
            builder.Services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 1;    // Number of requests in the timespan
                    options.Window = TimeSpan.FromSeconds(1); 
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 10;
                }
            ));

            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Streaming? Register a third party API with the HttpClient instance in the DI services. From here: https://www.learmoreseekmore.com/2021/12/minimal-api-Result-stream-return-type.html
            //builder.Services.AddScoped(httpClient => new HttpClient
            //{
            //    BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
            //});

            //builder.Services.AddSingleton<MultiThreadFileWriter>(); // TODO: Delete if we don't end up using MultiThreadFileWriter

            var app = builder.Build();
            app.UseRateLimiter();

            app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            );

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthorization();
            //ResourceConnector.GetGraphForResource("08ac06ea-4005-4e31-ae69-7ffc0447b332");
            //HistogramGenerator.GetHistogram("21514961-4208-41ab-8c7a-3b549e22e3e3");

            // TODO: Consider if we could/should use EnableBuffering for all endpoints. Code will look something like this:
            //app.Use(async (context, next) =>
            //{
            //    context.Request.EnableBuffering();
            //    await next();
            //});

            //app.UseRouting();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGet("/", async context =>
            //    {
            //        await context.Response.WriteAsync("Hello World!");
            //    });
            //});


            app.MapGet("/", () => "Hello World!");

            new Endpoints(app);
            app.Run();
        }
    }
}