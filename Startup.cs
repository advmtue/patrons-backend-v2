using System.Net;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

using patrons_web_api.Database;
using patrons_web_api.Authentication;
using patrons_web_api.Services;

namespace patrons_web_api
{
    public class Startup
    {
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
                options.AddPolicy("allowAll", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
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

            services.AddSingleton<IMongoDatabaseSettings>(sp => sp.GetRequiredService<IOptions<MongoDatabaseSettings>>().Value);
            services.AddSingleton<ISessionSettings>(sp => sp.GetRequiredService<IOptions<SessionSettings>>().Value);

            // Register injectables for IoC injector
            services.AddSingleton<IPatronsDatabase, MongoDatabase>();
            services.AddSingleton<IManagerService, ManagerService>();
            services.AddSingleton<IVenueService, VenueService>();
            services.AddSingleton<IPatronService, PatronService>();
            services.AddSingleton<PasswordService>();
            services.AddSingleton<ISessionService, SessionService>();

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

            app.UseCors("allowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
