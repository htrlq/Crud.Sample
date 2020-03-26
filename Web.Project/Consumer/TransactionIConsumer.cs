using entityFrame.Model;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Project.Command;

namespace Web.Project.Consumer
{
    internal class TransactionConsumer : 
        IConsumer<PayOrderCommand>,
        IConsumer<PayOrderEvent>
    {
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
    }
}
