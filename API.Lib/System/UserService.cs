using API.Lib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Lib.System
{
    public class UserService : IUserService
    {
        public UserService( UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) 
        {

        }
        public Task<bool> Authencate(LoginRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Register(RegisterRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
