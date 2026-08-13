using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Context;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Repository;
using TERMS_LOYALTY_API.Services;
using TERMS_LOYALTY_API.Services.Providers;
using TERMS_LOYALTY_API.SignalRHubs;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;
using TERMS_MOBILE_WEB_API.Repository;

namespace TERMS_MOBILE_WEB_API
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

            services.AddControllers();

            //services.AddDbContext<UserManagementDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("UserManagementDbCon")));


            services.AddDbContext<SmartShelfDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("SmartShelfDbCon")));


            services.Configure<ApplicationSettings>(Configuration.GetSection("ApplicationSettings"));

           // services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));

            //Minew Cloud Service
            services.Configure<MinewLoginSettings>(Configuration.GetSection("MinewLogin"));

            //services.AddDefaultIdentity<ApplicationUser>()
            //     .AddEntityFrameworkStores<DatabaseContext>();
            //services.AddScoped<IDialogSMSSenderAPIService, DialogSMSSenderAPIService>();

            services.AddScoped<IProduct, ProductRepository>();
            services.AddScoped<IShelf, ShelfRepository>();
            services.AddScoped<IAisle, AisleRepository>();
            services.AddScoped<IQueue, QueueRepository>();
            services.AddScoped<IMessage, MessageRepository>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddHttpClient<MinewCloudService>();
            services.AddSingleton<MinewCloudService>();
            services.AddScoped<IEslProvider, MinewEslProvider>();
            services.AddScoped<IEslProviderFactory, EslProviderFactory>();
            services.AddScoped<IStore, StoreRepository>();
            services.AddScoped<IDevice, DeviceRepository>();
            services.AddScoped<IDashboard, DashboardRepository>();
            services.AddScoped<IUserMaster, UserMasterRepository>();

            services.AddHostedService<QueueProcessorService>();


            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;
            }
            );

            //services.AddCors(options =>
            //{
            //    options.AddPolicy("AngularPolicy",
            //        builder => builder
            //            .WithOrigins(
            //                "http://localhost:4200", // Angular dev server
            //                "http://localhost:82",
            //                "http://localhost:44321",
            //                Configuration["ApplicationSettings:Client_URL"].ToString())
            //            .AllowAnyHeader()
            //            .AllowAnyMethod()
            //            .AllowCredentials());
            //});

            //// In ConfigureServices
            //services.AddCors(options =>
            //{
            //    options.AddPolicy("AllowAll", builder =>
            //    {
            //        builder
            //            .AllowAnyOrigin()
            //            .AllowAnyMethod()
            //            .AllowAnyHeader();
            //    });
            //});

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .WithOrigins(
                            "http://20.212.176.38:8085",     // Live Server
                            "https://esl.retailit.lk",
                            "http://localhost:8085",      // Angular dev server (this app)
                            "http://localhost:5500",      // Live Server alternative
                            "http://localhost:5173",     //react web
                            "http://localhost:4200",      // Angular dev server
                            "http://localhost:82",        // Your other ports
                            "https://localhost:44321",    // Your API itself
                            "http://localhost:8056"     // Live Server - Local
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()               // Keep this for SignalR if needed
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            });



            services.AddHttpContextAccessor();

            services.AddSignalR();


            //services.AddCors();

            var key = Encoding.UTF8.GetBytes(Configuration["ApplicationSettings:JWT_Secret"].ToString());

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Jwt.JwtAudience,
                    ValidIssuer = Jwt.JwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt.JwtKey))
                };
            });

            ////Minew Cloud Service
            //services.Configure<MinewLogin.MinewLoginRequest>(Configuration.GetSection("MinewLogin"));

            //After Publish Remov
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TERMS_MOBILE_WEB_API",
                    Version = "v1"
                });

                // 🔐 Enable JWT Bearer Authorization in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n" +
                                  "Enter 'Bearer' [space] and then your token.\r\n\r\n" +
                                  "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
            });

            //        services.AddSwaggerGen(c =>
            //        {
            //            c.SwaggerDoc("v1", new OpenApiInfo
            //            {
            //                Title = "TERMS_MOBILE_WEB_API",
            //                Version = "v1"
            //            });

            //            // Enable Bearer token input in Swagger
            //            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //            {
            //                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            //                Name = "Authorization",
            //                In = ParameterLocation.Header,
            //                Type = SecuritySchemeType.Http,
            //                Scheme = "bearer",
            //                BearerFormat = "JWT"
            //            });

            //            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //{
            //    {
            //        new OpenApiSecurityScheme
            //        {
            //            Reference = new OpenApiReference
            //            {
            //                Type = ReferenceType.SecurityScheme,
            //                Id = "Bearer"
            //            }
            //        },
            //        Array.Empty<string>()
            //    }
            //});
            //        });


            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TERMS_MOBILE_WEB_API", Version = "v1" });
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());

            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            // NOTE: a middleware here used to force ContentLength = 0 on every
            // 201 Created. By that point the response had already begun
            // streaming with Transfer-Encoding: chunked, so declaring a
            // conflicting length produced a malformed response and the
            // connection was reset (ECONNRESET) as soon as a client tried to
            // read the body. Every CreatedAtAction result - queue creation
            // among them - was affected: the row was written, but the caller
            // saw a network failure. Removed; 201 responses now return their
            // body normally.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TERMS_MOBILE_WEB_API v1");
                c.RoutePrefix = "swagger"; // optional, default
            });

            //app.UseCors("AngularPolicy");

            // In Configure
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseAuthentication();

            app.UseAuthorization();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                // A published app never reads launchSettings.json, so its
                // launchUrl ("swagger") only applies when debugging locally and
                // the deployed site root just 404s - wwwroot has no index page.
                // Send the root to the Swagger UI instead. PathBase is included
                // so this still works when hosted under an IIS virtual
                // directory rather than at the site root.
                endpoints.MapGet("/", context =>
                {
                    context.Response.Redirect($"{context.Request.PathBase}/swagger");
                    return Task.CompletedTask;
                });

                endpoints.MapHub<DeviceAssignmentHub>("/deviceHub");
                endpoints.MapControllers();
            });
            //app.UseSwagger();
            //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "coreweb5 v1"));
        }
    }
}
