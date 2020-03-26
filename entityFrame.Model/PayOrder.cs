using System;

namespace entityFrame.Model
{
    public class PayOrder: BaseModel
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public decimal Money { get; set; }
    }
}
