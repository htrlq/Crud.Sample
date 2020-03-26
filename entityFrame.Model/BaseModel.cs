using System;
using System.ComponentModel.DataAnnotations;

namespace entityFrame.Model
{
    public class BaseModel
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
