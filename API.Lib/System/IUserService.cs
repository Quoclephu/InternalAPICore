using API.Lib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Lib.System
{
    public interface IUserService
    {
        Task<bool> Authencate(LoginRequest request);
        Task<bool> Register(RegisterRequest request);
    }
}
