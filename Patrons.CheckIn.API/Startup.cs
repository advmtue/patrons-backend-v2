using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Amazon;
using Amazon.SimpleEmail;

using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Authentication;
using Patrons.CheckIn.API.Services;

namespace Patrons.CheckIn.API
{
    public class Startup
    {
        private readonly string _corsPatronsOrigins = "_corsPatronsOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(_corsPatronsOrigins, builder =>
                {
                    builder
                        .WithOrigins("https://patrons.at", "http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            // Authentication configuration
            services.AddAuthentication("sessionId")
                .AddScheme<SessionIdAuthenticationSchemeOptions, SessionIdAuthenticationHandler>("sessionId", o => { });

            // Authorization configuration
            services.AddAuthorization(options =>
            {
                options.AddPolicy("fullAccess", policy => policy.RequireRole("FULL"));
                options.AddPolicy("registrationAccess", policy => policy.RequireRole("RESET"));
                options.AddPolicy("authenticated", policy => policy.RequireRole("FULL", "RESET"));
            });

            // Configs
            services.Configure<MongoDatabaseSettings>(Configuration.GetSection(nameof(MongoDatabaseSettings)));
            services.Configure<SessionSettings>(Configuration.GetSection(nameof(SessionSettings)));
            services.Configure<RecaptchaV3Settings>(Configuration.GetSection(nameof(RecaptchaV3Settings)));

            services.AddSingleton<IMongoDatabaseSettings>(sp => sp.GetRequiredService<IOptions<MongoDatabaseSettings>>().Value);
            services.AddSingleton<ISessionSettings>(sp => sp.GetRequiredService<IOptions<SessionSettings>>().Value);
            services.AddSingleton<IRecaptchaV3Settings>(sp => sp.GetRequiredService<IOptions<RecaptchaV3Settings>>().Value);

            // AWS SES
            services.AddSingleton<IAmazonSimpleEmailService>(new AmazonSimpleEmailServiceClient(RegionEndpoint.APSoutheast2));

            // Register injectables for IoC injector
            services.AddSingleton<IPatronsDatabase, MongoDatabase>();
            services.AddSingleton<IManagerService, ManagerService>();
            services.AddSingleton<IVenueService, VenueService>();
            services.AddSingleton<IPatronService, PatronService>();
            services.AddSingleton<PasswordService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IRecaptchaService, RecaptchaService>();
            services.AddSingleton<INewsletterService, NewsletterService>();
            services.AddSingleton<IEmailService, EmailService>();

            // Register API controllers
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(_corsPatronsOrigins);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
