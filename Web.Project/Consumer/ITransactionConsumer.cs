#undef _001
#undef _002
#define _rpc

using AspectCore.DynamicProxy;
using AspectCore.Extensions.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Web.Project.Command;

namespace Web.Project.Consumer
{
    public interface ITransactionConsumer :
        IConsumer<PayOrderEvent>,
        IConsumer<PayOrderRequest>,
        IConsumer<FaultMessage<PayOrderEvent>>
    {
        [RpcConsumer]
        new Task Consume(ConsumeContext<PayOrderEvent> context);
    }

    public class RpcConsumerAttribute : AbstractInterceptorAttribute
    {
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            try
            {
                await next(context);
            }
            catch
            {
                var _arg = context.Parameters[0];

                if (_arg is ConsumeContext consume)
                {
                    var consumeType = consume.GetType().GetGenericArguments()[1];
                    var property = consume.GetType().GetProperty("Message").GetReflector();
                    var arg = property.GetValue(consume);
                    var argType = arg.GetType();

                    var faultType = typeof(FaultMessage<>).MakeGenericType(argType);
                    var constructor = faultType.GetConstructor(new[] { argType });
                    var constructorInfo = constructor.GetReflector();
                    var instance = constructorInfo.Invoke(arg);

                    var busControl = context.ServiceProvider.GetRequiredService<IBusControl>();
                    await busControl.Publish(instance);
                }
            }
        }
    }
}
