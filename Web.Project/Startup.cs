#undef _001

using entityFrame.Model;
using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Project.Command;
using Web.Project.Consumer;
using AspectCore.Extensions.DependencyInjection;

namespace Web.Project
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            services
                .ConfigureDynamicProxy()
                .AddScoped<TransactionConsumer>()
                .AddDbContext<TransactionContexts>(build => {
                    build.UseInMemoryDatabase("TransactionContexts");
                })
                .AddMassTransit(c =>
                {
                    c.AddConsumer<TransactionConsumer>();
                    c.AddConsumer<FaultConsumer>();

                    c.AddBus(serviceProvider =>
                    {
                        return Bus.Factory.CreateUsingRabbitMq(cfg =>
                        {

                            cfg.SetExchangeArgument("durable", true);

                            var host = cfg.Host(new Uri("rabbitmq://localhost/"), hst =>
                            {
                                hst.Username("guest");
                                hst.Password("guest");
                            });

                            cfg.ReceiveEndpoint("Transaction", config =>
                            {
                                config.Handler<PayOrderRequest>(context =>
                                {
                                    var value = context.Message;

                                    var bus = serviceProvider.GetRequiredService<IBusControl>();
                                    var rpcClient = context.Request<PayOrderEvent, PayOrderResponse>(bus,
                                        new PayOrderEvent
                                        {
                                            SourceId = value.SourceId,
                                            TargetId = value.TargetId,
                                            Money = value.Money
                                        }
                                    );

                                    var response = rpcClient.Result;

                                    return context.RespondAsync(response.Message);
                                });

                                EndpointConvention.Map<PayOrderEvent>(config.InputAddress);
                                EndpointConvention.Map<PayOrderRequest>(config.InputAddress);


                                var transactionConsumer = serviceProvider.GetRequiredService<TransactionConsumer>();
                                Console.WriteLine(transactionConsumer.GetType());
                                config.Consumer(() => transactionConsumer);
                                //config.ConfigureConsumer<TransactionConsumer>(serviceProvider);
                            });

                            cfg.ReceiveEndpoint("Retry", config =>
                            {
                                config.ConfigureConsumer<FaultConsumer>(serviceProvider);
                            });
                        });
                    });
                });

            var container = services.BuildDynamicProxyProvider();

            return container;
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
#if _001
                await busControl.Publish(new PayOrderCommand
                {
                    SourceId = 1,
                    TargetId = 2,
                    Money = 2000
                });

                await context.Response.WriteAsync($"Hello World!");
#else
                var entity = new PayOrderEvent
                {
                    SourceId = 1,
                    TargetId = 2,
                    Money = 2000
                };
                //await busControl.Publish(entity);
                var response = await busControl.Request<PayOrderEvent, PayOrderResponse>(entity);

                //await context.Response.WriteAsync($"Hello World! Success:{response.Message.Success}");
#endif
            });
        }
    }

    public static class ExampleMiddlewareConfiguratorExtensions
    {
        public static void UseExceptionLogger<T>(this IPipeConfigurator<T> configurator, IServiceProvider serviceProvider)
            where T : class, PipeContext
        {
            configurator.AddPipeSpecification(serviceProvider.GetRequiredService<ExceptionSpecification<T>>());
        }
    }

    internal class ExceptionSpecification<T> : IPipeSpecification<T>
        where T : class, PipeContext
    {
        private ExceptionFilter<T> filter;

        public ExceptionSpecification(ExceptionFilter<T> filter)
        {
            this.filter = filter;
        }

        public void Apply(IPipeBuilder<T> builder)
        {
            builder.AddFilter(filter);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    internal class ExceptionFilter<T> : IFilter<T>
        where T : class, PipeContext
    {
        private IExceptionFilter filter;

        public ExceptionFilter(IExceptionFilter filter)
        {
            this.filter = filter;
        }

        public void Probe(ProbeContext context)
        {
        }

        public async Task Send(T context, IPipe<T> next)
        {
            try
            {
                await next.Send(context);
            }
            catch(Exception ex)
            {
                await filter.InvokeAsync(ex);
            }
        }
    }

    public interface IExceptionFilter
    {
        Task InvokeAsync(Exception exception);
    }
}
