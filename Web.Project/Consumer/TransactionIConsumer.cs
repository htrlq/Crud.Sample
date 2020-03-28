#undef _001
#undef _002
#define _rpc

using entityFrame.Model;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Web.Project.Command;

namespace Web.Project.Consumer
{

    public class TransactionConsumer:
#if _001
        IConsumer<PayOrderCommand>
#else
        IConsumer<PayOrderEvent>,
        IConsumer<PayOrderRequest>,
        IConsumer<FaultMessage<PayOrderEvent>>
#endif

    {
#if _001
        private TransactionContexts transactionContexts;

        public TransactionConsumer(TransactionContexts transactionContexts)
        {
            this.transactionContexts = transactionContexts;
        }

        public async Task Consume(ConsumeContext<PayOrderCommand> context)
        {
            var value = context.Message;

            await Console.Out.WriteLineAsync($"PayOrderCommand Before:{DateTime.Now} SourceId:{value.SourceId} TargetId:{value.TargetId} Money:{value.Money}");

            await transactionContexts.PayOrders.AddAsync(new PayOrder
            {
                Id = 1,
                SourceId = value.SourceId,
                TargetId = value.TargetId,
                Money = value.Money
            });
            await transactionContexts.SaveChangesAsync();

            await context.Publish(new PayOrderEvent
            {
                SourceId = value.SourceId,
                TargetId = value.TargetId,
                Money = value.Money
            });

            await Console.Out.WriteLineAsync($"PayOrderCommand After:{DateTime.Now}");
        }

        public async Task Consume(ConsumeContext<PayOrderEvent> context)
        {
            var value = context.Message;

            await Console.Out.WriteLineAsync($"PayOrderEvent Before:{DateTime.Now} SourceId:{value.SourceId} TargetId:{value.TargetId} Money:{value.Money}");

            var source = transactionContexts.UserInfos.First(user => user.Id == value.SourceId);

            if (source.Money < value.Money)
                throw new Exception();

            var target = transactionContexts.UserInfos.First(user => user.Id == value.TargetId);

            source.Money -= value.Money;
            target.Money += value.Money;

            transactionContexts.UserInfos.Update(source);
            transactionContexts.UserInfos.Update(target);

            await transactionContexts.SaveChangesAsync();

            await Console.Out.WriteLineAsync($"PayOrderEvent After:{DateTime.Now}");
        }
#else
        private TransactionContexts transactionContexts;
        private IServiceProvider serviceProvider;
        private Lazy<IBusControl> lazyBusControl;

        public TransactionConsumer(TransactionContexts transactionContexts, IServiceProvider serviceProvider)
        {
            this.transactionContexts = transactionContexts;
            this.serviceProvider = serviceProvider;

            lazyBusControl = new Lazy<IBusControl>(serviceProvider.GetRequiredService<IBusControl>);
        }

        public async Task Consume(ConsumeContext<PayOrderRequest> context)
        {
            var value = context.Message;

            var source = transactionContexts.UserInfos.First(user => user.Id == value.SourceId);

            if (source.Money < value.Money)
                throw new Exception();

            var target = transactionContexts.UserInfos.First(user => user.Id == value.TargetId);

            source.Money -= value.Money;
            target.Money += value.Money;

            transactionContexts.UserInfos.Update(source);
            transactionContexts.UserInfos.Update(target);

            await transactionContexts.SaveChangesAsync();

            var bus = lazyBusControl.Value;
            var rpcClient = context.Request<PayOrderEvent, PayOrderResponse>(bus, 
                new PayOrderEvent
                {
                    SourceId = value.SourceId,
                    TargetId = value.TargetId,
                    Money = value.Money
                }
            );

            var response = rpcClient.Result;

            await context.RespondAsync(response.Message);
        }
        /*
        public async Task Consume(ConsumeContext<PayOrderEvent> context)
        {
            try
            {
                var value = context.Message;

                await transactionContexts.PayOrders.AddAsync(new PayOrder
                {
                    Id = 1,
                    SourceId = value.SourceId,
                    TargetId = value.TargetId,
                    Money = value.Money
                });
                await transactionContexts.SaveChangesAsync();

                await context.RespondAsync(new PayOrderResponse { Success = true });
            }
            catch(Exception ex)
            {

            }
        }
        */

        [RpcConsumer]
        public virtual async Task Consume(ConsumeContext<PayOrderEvent> context)
        {
            var value = context.Message;

            await transactionContexts.PayOrders.AddAsync(new PayOrder
            {
                Id = 1,
                SourceId = value.SourceId,
                TargetId = value.TargetId,
                Money = value.Money
            });
            await transactionContexts.SaveChangesAsync();

            await context.RespondAsync(new PayOrderResponse { Success = true });
        }

        public Task Consume(ConsumeContext<FaultMessage<PayOrderEvent>> context)
        {
            var message = context.Message;
            var sourceMessage = message.Entity;

            return Task.CompletedTask;
        }
#endif
    }

    public class FaultMessage<TEntity>
        where TEntity:class
    {
        public TEntity Entity { get; }

        public FaultMessage(TEntity entity)
        {
            Entity = entity;
        }
    }

    internal class FaultConsumer : 
        IConsumer<Fault<PayOrderEvent>>
    {
        private async Task Execute(ConsumeContext context, ExceptionInfo exceptionInfo)
        {
            await Console.Out.WriteLineAsync($"Type:{exceptionInfo.ExceptionType} Message:{exceptionInfo.Message}");

            if (exceptionInfo.InnerException != null)
            {
                await Execute(context, exceptionInfo.InnerException);
            }
        }

        public async Task Consume(ConsumeContext<Fault<PayOrderEvent>> context)
        {
            var retryCount = context.GetRetryCount();

            if (retryCount == 5)
            {
                if (EndpointConvention.TryGetDestinationAddress<PayOrderEvent>(out Uri endPointUrl))
                {
                    await context.Forward(endPointUrl);
                }
            }
            else
            {
                var exceptions = context.Message.Exceptions;

                foreach (var exceptionInfo in exceptions)
                {
                    await Execute(context, exceptionInfo);
                }
            }
        }
    }
}
