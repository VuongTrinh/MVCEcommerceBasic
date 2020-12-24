using ApplicationCore;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Infrastructure.Data;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Interfaces;
using Web.Services;

namespace Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            // use in-memory database
            //ConfigureInMemoryDatabases(services);

            // use real database
            ConfigureProductionServices(services);
        }

        //private void ConfigureInMemoryDatabases(IServiceCollection services)
        //{
        //    // use in-memory database
        //    services.AddDbContext<CatalogContext>(c =>
        //        c.UseInMemoryDatabase("Catalog"));

        //    // Add Identity DbContext
        //    services.AddDbContext<AppIdentityDbContext>(options =>
        //        options.UseInMemoryDatabase("Identity"));

        //    ConfigureServices(services);
        //}

        public void ConfigureProductionServices(IServiceCollection services)
        {
            // use real database
            // Requires LocalDB which can be installed with SQL Server Express 2016
            // https://www.microsoft.com/en-us/download/details.aspx?id=54284
            services.AddDbContext<CatalogContext>(c =>
                c.UseSqlServer(Configuration.GetConnectionString("CatalogConnection")));

            // Add Identity DbContext
            //services.AddDbContext<AppIdentityDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("IdentityConnection")));

            ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureCookieSettings(services);

            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

            services.AddScoped<ICatalogViewModelService, CachedCatalogViewModelService>();
            services.AddScoped<CatalogViewModelService>();
            services.Configure<CatalogSettings>(Configuration);
            services.AddSingleton<IUriComposer>(new UriComposer(Configuration.Get<CatalogSettings>()));
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            // Add memory cache services
            services.AddMemoryCache();

            services.AddRouting(options =>
            {
                // Replace the type and the name used to refer to it with your own
                // IOutboundParameterTransformer implementation
                options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });

            services.AddMvc(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(
                         new SlugifyParameterTransformer()));

            });

            services.AddControllersWithViews();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/Basket/Checkout");
            });

            services.AddHttpContextAccessor();

            
        }

        private static void ConfigureCookieSettings(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.Cookie = new CookieBuilder
                {
                    IsEssential = true // required for auth to work without explicit user consent; adjust to suit your privacy policy
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseShowAllServicesMiddleware();
                //app.UseMigrationsEndPoint();
                //app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller:slugify=Home}/{action:slugify=Index}/{id?}");
                endpoints.MapRazorPages();
                //endpoints.MapBlazorHub("/admin");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
