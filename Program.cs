using Repository.App;

namespace Repository
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //DBManager.FillMetadata(); // Will fill the metadata file with all the files in Resources. REMEMBER to delete the contents of the file before

            //ResourceConnector.GetGraphForResource("fe960d94-5928-4463-b0f8-c59072b5d449");

            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine("Environment.IsDevelopment");
                builder.WebHost.UseUrls("https://localhost:4000");
            }

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors();

            // Streaming? Register a third party API with the HttpClient instance in the DI services. From here: https://www.learmoreseekmore.com/2021/12/minimal-api-Result-stream-return-type.html
            builder.Services.AddScoped(httpClient => new HttpClient
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
            });

            var app = builder.Build();

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

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/", () => "Hello World!");

            new Endpoints.Endpoints(app);
            app.Run();
        }
    }
}