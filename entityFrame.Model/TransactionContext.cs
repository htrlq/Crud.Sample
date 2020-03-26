using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace entityFrame.Model
{
    public class TransactionContexts:DbContext
    {
        public DbSet<PayOrder> PayOrders { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }

        public TransactionContexts(DbContextOptions<TransactionContexts> options):base(options)
        {

        }
    }
}
