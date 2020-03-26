using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using MassTransit;
using entityFrame.Model;
using Microsoft.EntityFrameworkCore;
using Web.Project.Consumer;
using Web.Project.Command;

namespace Web.Project
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TransactionConsumer>();

            services.AddMassTransit(c =>
            {
                c.AddConsumer<TransactionConsumer>();

                c.AddBus(serviceProvider =>
                {
                    return Bus.Factory.CreateUsingRabbitMq(cfg =>
                    {
                        var host = cfg.Host(new Uri("rabbitmq://localhost/"), hst =>
                        {
                            hst.Username("guest");
                            hst.Password("guest");
                        });

                        cfg.ReceiveEndpoint("Transaction", config =>
                        {
                            config.ConfigureConsumer<TransactionConsumer>(serviceProvider);
                        });
                    });
                });
            });

            services.AddDbContext<TransactionContexts>(build=> {
                build.UseInMemoryDatabase("TransactionContexts");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region
            using (var serviceScoped = app.ApplicationServices.CreateScope())
            {
                var serviceProvider = serviceScoped.ServiceProvider;
                var context = serviceProvider.GetRequiredService<TransactionContexts>();

                context.UserInfos.Add(new UserInfo
                {
                    Id = 1,
                    NickName = "Form",
                    CreateTime = DateTime.Now,
                    LastOptions = DateTime.Now,
                    Money = 5000
                });
                context.UserInfos.Add(new UserInfo
                {
                    Id = 2,
                    NickName = "To",
                    CreateTime = DateTime.Now,
                    LastOptions = DateTime.Now,
                    Money = 5000
                });

                context.SaveChanges();
            }
            #endregion

            var busControl = app.ApplicationServices.GetRequiredService<IBusControl>();

            lifetime.ApplicationStarted.Register(busControl.Start);
            lifetime.ApplicationStopped.Register(busControl.Stop);

            app.Run(async (context) =>
            {
                await busControl.Publish(new PayOrderCommand
                {
                    SourceId = 1,
                    TargetId = 2,
                    Money = 2000
                });

                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
