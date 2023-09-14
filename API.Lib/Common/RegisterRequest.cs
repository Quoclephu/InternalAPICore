using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Lib.Common
{
    public class RegisterRequest
    {
        public int intID { get; set; }
        public string strUserName { get; set; }
        public string strFullName { get; set; }
        public Boolean bolIsAdmin { get; set; }
        public string strPassword { get; set; }
        public string strEmail { get; set; }
        public string strConfirmPass { get; set;}

    }
}
