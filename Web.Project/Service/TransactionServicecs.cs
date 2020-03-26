using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Project.Command;

namespace Web.Project.Service
{
    internal class TransactionService: ITransactionService
    {
        private IBusControl busControl;

        public TransactionService(IBusControl busControl)
        {
            this.busControl = busControl;
        }

        public async void TransferAccounts(int sourceId, int targetId, decimal money)
        {
            await busControl.Publish(
                new PayOrderCommand { SourceId = sourceId, TargetId = targetId, Money = money }
            );
        }
    }

    public interface ITransactionService
    {
        void TransferAccounts(int sourceId, int targetId, decimal money);
    }
}
