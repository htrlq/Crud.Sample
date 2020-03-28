using AspectCore.DynamicProxy;

namespace Web.Project.Command
{
    public class PayOrderEvent
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public decimal Money { get; set; }
    }

    public class PayOrderRequest
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public decimal Money { get; set; }
    }

    public class PayOrderResponse
    {
        public bool Success { get; set; }
    }
}
