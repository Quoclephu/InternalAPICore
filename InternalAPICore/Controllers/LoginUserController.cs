using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly AppSetting _appSettings;
    public UserController(MyDbContext context, IOptionsMonitor<AppSetting> optionsMonitor)
    {
        _context = context;
        _appSettings = optionsMonitor.CurrentValue;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Validate(LoginModel model)
    {
        var user = "admin";
        if (user == null) //không đúng
        {
            return Ok(new ApiResponse
            {
                Success = false,
                Message = "Invalid username/password"
            });
        }

        //cấp token
        var token = await GenerateToken();

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Authenticate success",
            Data = token
        });
    }

    private async Task<TokenModel> GenerateToken()
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var secretKeyBytes = Encoding.UTF8.GetBytes(_appSettings.SecretKey);

        var token = jwtTokenHandler.CreateToken(tokenDescription);
        var accessToken = jwtTokenHandler.WriteToken(token);
        var refreshToken = GenerateRefreshToken();
        await _context.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new TokenModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string GenerateRefreshToken()
    {
        var random = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);

            return Convert.ToBase64String(random);
        }
    }

    [HttpPost("RenewToken")]
    public async Task<IActionResult> RenewToken(TokenModel model)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var secretKeyBytes = Encoding.UTF8.GetBytes(_appSettings.SecretKey);
        var tokenValidateParam = new TokenValidationParameters
        {
            //tự cấp token
            ValidateIssuer = false,
            ValidateAudience = false,

            //ký vào token
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKeyBytes),

            ClockSkew = TimeSpan.Zero,

            ValidateLifetime = false //ko kiểm tra token hết hạn
        };
        try
        {
            //check 1: AccessToken valid format
            var tokenInVerification = jwtTokenHandler.ValidateToken(model.AccessToken, tokenValidateParam, out var validatedToken);

            //check 2: Check alg
            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);
                if (!result)//false
                {
                    return Ok(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid token"
                    });
                }
            }

            //check 3: Check accessToken expire?
            var utcExpireDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expireDate = ConvertUnixTimeToDateTime(utcExpireDate);
            if (expireDate > DateTime.UtcNow)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Access token has not yet expired"
                });
            }

            //check 4: Check refreshtoken exist in DB
            var storedToken = _context.RefreshTokens.FirstOrDefault(x => x.Token == model.RefreshToken);
            if (storedToken == null)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Refresh token does not exist"
                });
            }

            //check 5: check refreshToken is used/revoked?
            if (storedToken.IsUsed)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Refresh token has been used"
                });
            }
            if (storedToken.IsRevoked)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Refresh token has been revoked"
                });
            }

            //check 6: AccessToken id == JwtId in RefreshToken
            var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (storedToken.JwtId != jti)
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Token doesn't match"
                });
            }

            //Update token is used
            storedToken.IsRevoked = true;
            storedToken.IsUsed = true;
            _context.Update(storedToken);
            await _context.SaveChangesAsync();

            //create new token
            var user = await _context.NguoiDungs.SingleOrDefaultAsync(nd => nd.Id == storedToken.UserId);
            var token = await GenerateToken(user);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Renew token success",
                Data = token
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Something went wrong"
            });
        }
    }
}