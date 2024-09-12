using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using TodoList_back.Data;
using TodoList_back.Models;

namespace TodoList_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public ActionResult Register([FromBody] RegisterModel model)
        {
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                return BadRequest("User with this email already exists.");
            }

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                AuthProvider = "RegisterForm"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginModel model)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }
            if (user.AuthProvider == "Google")
            {
                return BadRequest("This account is linked to Google. Please use Google to log in.");
            }
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString });
        }

        [HttpGet("login-google")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse"),
                IsPersistent = true
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpPost("signin-google")]
        public async Task<IActionResult> GoogleResponse([FromBody] GoogleTokenModel model)
        {
            if (string.IsNullOrEmpty(model.Token))
            {
                return BadRequest("Missing token");
            }

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>()
                        { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(model.Token, settings);

                if (payload == null)
                {
                    return BadRequest("Invalid token");
                }
                
                var existingUser = _context.Users.SingleOrDefault(u => u.Email == payload.Email);
                if (existingUser != null)
                {
                    if (existingUser.AuthProvider != "Google")
                    {
                        existingUser.FirstName = payload.GivenName;
                        existingUser.LastName = payload.FamilyName;
                        existingUser.Email = payload.Email;
                        existingUser.Password = BCrypt.Net.BCrypt.HashPassword(_configuration["Authentication:Google:DefaultGooglePassword"]);
                        existingUser.AuthProvider = "Google";

                        _context.Users.Update(existingUser);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    var newUser = new User
                    {
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        Email = payload.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(_configuration["Authentication:Google:DefaultGooglePassword"]),
                        AuthProvider = "Google"
                    };

                    _context.Users.Add(newUser);
                    _context.SaveChanges();
                    existingUser = newUser;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, existingUser.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var jwtToken = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds);

                var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return BadRequest($"Google login failed: {ex.Message}");
            }
        }
    }
}

public class GoogleTokenModel
{
    public string Token { get; set; }
}
