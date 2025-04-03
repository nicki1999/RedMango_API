using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RedMango_API.Data;
using RedMango_API.Model;
using RedMango_API.Model.Dto;
using RedMango_API.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
namespace RedMango_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        public AuthController(ApplicationDbContext db, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _userManager = userManager;
            _roleManager = roleManager;
            _response = new ApiResponse();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            ApplicationUser userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
            bool isValid = await _userManager.CheckPasswordAsync(userFromDb, model.Password);

            if(isValid == false)
            {
                _response.Result = new LoginResponseDto();
                _response.statusCode = HttpStatusCode.BadRequest;
                _response.isSuccess = false;
                return BadRequest(_response);
            }

            var roles = await _userManager.GetRolesAsync(userFromDb);

            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
    new Claim("fullName", userFromDb.Name),
    new Claim("id", userFromDb.Id.ToString()),
    new Claim(ClaimTypes.Email, userFromDb.UserName.ToString()),
    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);



            LoginResponseDto loginResponse = new()
            {
                Email = userFromDb.Email,
                Token = tokenHandler.WriteToken(token)
            };
            if(loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _response.statusCode = HttpStatusCode.BadRequest;
                _response.isSuccess = false;
                return BadRequest(_response);
            }
            _response.statusCode = HttpStatusCode.OK;
            _response.isSuccess = true;
            _response.Result = loginResponse;
            return Ok(_response);
        }



        [HttpPost("register")]
        public async Task<IActionResult> Resgister([FromBody] RegisterRequestDto model)
        {
            ApplicationUser userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());

            if (userFromDb != null)
            {
                _response.statusCode = HttpStatusCode.BadRequest;
                _response.isSuccess = false;
                _response.ErrorMessages.Add("User already exists");
                return BadRequest(_response);
            }
            ApplicationUser newUser = new()
            {
                UserName = model.UserName,
                Name = model.Name,
                Email = model.UserName
            };
            try
            {
                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
                    {
                        //create roles in db
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
                    }
                    if (model.Role.ToLower() == SD.Role_Admin)
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Admin);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Customer);
                    }

                    _response.statusCode = HttpStatusCode.OK;
                    _response.isSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {

            }
            _response.statusCode = HttpStatusCode.BadRequest;
            _response.isSuccess = false;
            _response.ErrorMessages.Add("Error while registering");
            return BadRequest(_response);

        }
    }
}
