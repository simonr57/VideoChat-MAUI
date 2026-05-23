using System.Text;
using System.Threading.RateLimiting;
using ChatBE.Hubs;
using ChatBE.Services;
using ChatBE.Util;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ChatBE
{
    public class Startup
    {
        private const string firebaseUri =
            "https://your-proj-default-rtdb.europe-west1.firebasedatabase.app";

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;

            ConfigurationHelper.SetConfiguration(configuration);
        }

        public IWebHostEnvironment Env { get; }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<FirebaseService>(client =>
            {
                client.BaseAddress = new Uri(firebaseUri); // Replace with your Firebase URL
            });

            services.AddHttpClient<SyncService>(client =>
            {
                client.BaseAddress = new Uri(firebaseUri); // Replace with your Firebase URL
            });

            services.AddCors();

            services
                .AddAuthentication("UserClaims")
                .AddScheme<UserClaimsCustomAuthOptions, UserClaimsCustomAuthHandler>(
                    "UserClaims",
                    null
                );
            services.AddScoped<IGenerateJWT, GenerateJWT>();

            services.AddSignalR();

            services.AddSingleton<List<User>>();
            services.AddSingleton<List<UserCall>>();
            services.AddSingleton<List<CallOffer>>();

            services.AddControllers();

            // Add Rate Limiting
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            key => new FixedWindowRateLimiterOptions // Ensure 'key' is passed here
                            {
                                PermitLimit = 100, // Allow 100 requests
                                Window = TimeSpan.FromMinutes(1), // Per minute
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0, // Max queue of 10 requests
                            }
                        )
                );

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Return 429 when limit is exceeded
            });

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["JwtToken:Issuer"],
                        ValidAudience = Configuration["JwtToken:Issuer"],

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(Configuration["JwtToken:MaSecurtKey"] ?? "")
                        ),
                    };
                });

            //never use addsingleton -> it will break server.urls
            services.AddHttpContextAccessor();

            // Configure cookie policy
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
            });

            services.AddTransient<IPasswordHasher<string>, PasswordHasher<string>>();
            services.AddMvc().AddSessionStateTempDataProvider();
            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseCors(
            // options => options.WithOrigins("http://localhost",
            //                                 "http://localhost:4200")
            //                 .AllowAnyMethod()
            //                 .AllowAnyHeader()
            //                 .AllowCredentials()
            //);

            app.UseCors(x =>
                x.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true) // allow any origin
                    .AllowCredentials()
            );

            app.UseSession();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Apply Rate Limiting Middleware
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("default", "{controller=Account}/{action=Login}");
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/chatHub");
                endpoints.MapHub<ConnectionHub>("/ConnectionHub");
            });
        }
    }
}
