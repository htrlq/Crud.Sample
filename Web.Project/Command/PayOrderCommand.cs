using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Project.Command
{
    internal class PayOrderCommand
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public decimal Money { get; set; }
    }
}
