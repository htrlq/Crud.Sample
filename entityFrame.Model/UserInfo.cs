using System;

namespace entityFrame.Model
{
    public class UserInfo: BaseModel
    {

        public string NickName { get; set; }
        public decimal Money { get; set; }
        public DateTime LastOptions { get; set; }
    }
}
