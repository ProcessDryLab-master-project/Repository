using Repository.App.API;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;
using Repository.App.Database;
using Repository.App.Visualizers;
using System.Threading.Channels;

namespace Repository
{
    public class Program
    {
        public static void Main(string[] args)
        {
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

            // Rate Limiting example with psQl: https://blog.devart.com/implementing-rate-limiting-in-asp-net-6-core-and-c.html
            builder.Services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 1;    // Number of requests in the timespan
                    options.Window = TimeSpan.FromSeconds(0.3);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 10;
                }
            ));

            // Dependency injection of the ResourceManager to use the databases:
            builder.Services.AddSingleton<ResourceManager>(new ResourceManager(new FileDb(), new MetadataDb()));
            var app = builder.Build();
            // TODO: Consider if we could/should use EnableBuffering for all endpoints. Code will look something like this:
            //app.Use(async (context, next) => {
            //    context.Request.EnableBuffering();
            //    await next();
            //});
            //app.UseMiddleware<RequestValidatorMiddleware>(); // TODO: If this isn't necessary, delete the class as well.

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

            // TODO: Delete if we don't reimplement https.
            //app.UseHttpsRedirection();
            //app.UseAuthorization();
            //app.UseRouting();

            app.MapGet("/", () => "Hello World!");

            new Endpoints(app);
            app.Run();
        }
    }
}