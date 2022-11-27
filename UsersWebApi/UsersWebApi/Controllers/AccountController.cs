using AuthCommon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UsersWebApi.Helpers;
using UsersWebApi.Models;
using MailKit.Net.Smtp;
using MimeKit;



namespace UsersWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationContext _context;
        private readonly IOptions<AuthOptions> _authOptions;
        public AccountController(IConfiguration configuration, IWebHostEnvironment env, ApplicationContext context,
            IOptions<AuthOptions> authOptions)
        {


            _configuration = configuration;
            _env = env;
            _context = context;
            _authOptions = authOptions;
        }


        [Route("login")]
        [HttpPost]

        public IActionResult Login([FromBody] Login request)
        {
            var user = AuthenticateUser(request.Email, request.Password);

            if (user != null)
            {
                var token = GenerateJWT(user);

                return Ok(new { access_token = token,role=user.Role });
            }

            return Unauthorized();
        }

        [Route("verify")]
        [HttpPost]

        public IActionResult Verify(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.VerificationToken == token);

            if (user == null)
            {
                return BadRequest("Invalid token.");
            }

            return Ok("User Verifyed!");
        }


        private User AuthenticateUser(string email, string password)
        {
            var password_hash = HashPasswordHelper.HashPassword(password);
            return _context.Users.SingleOrDefault(u => u.Email == email && u.Password == password_hash);
        }

        private string GenerateJWT (User user){
            var authParams = _authOptions.Value;

            var securityKey = authParams.GetSymmetricSecurityKey();
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString())
            };


            var token = new JwtSecurityToken(authParams.Issuer,
                authParams.Audience,
                claims,
                expires: DateTime.Now.AddSeconds(authParams.TokenLifetime),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Route("register")]
        [HttpPost]
        public JsonResult Post(User user)
        {
            
            
            var userNew = new User()
            {
                Password = HashPasswordHelper.HashPassword(user.Password),
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role
        };

            _context.Users.Add(userNew);
            _context.SaveChanges();
            return new JsonResult("Added Success");
        }

        [HttpGet]
        [Authorize]
        [Route("users")]

        public JsonResult GetUsers()
        {
           
            var table = (from user in _context.Users join role in _context.Roles
                        on user.Role equals role.Id 
                        select new { 
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Phone,
                        role.Role
                        }).ToList();
            return new JsonResult(table);
        }

        [HttpGet]
        [Authorize]
        [Route("user/{userId}")]

        public JsonResult GetUser(int userId)
        {

            var userResponse = (from user in _context.Users
                         join role in _context.Roles
                         on user.Role equals role.Id
                         where user.Id == userId
                         select new
                         {
                             user.Id,
                             user.Name,
                             user.Email,
                             user.Phone,
                             role.Role
                         }).FirstOrDefault();
            return new JsonResult(userResponse);
        }

        [HttpDelete]
        [Authorize]
        [Route("user/{id}")]
        public JsonResult Delete(int id)
        {
            

            var table = _context.Users.ToList();
            var itemToRemove = _context.Users.SingleOrDefault(x => x.Id == id);


            _context.Users.Remove(itemToRemove);
            _context.SaveChanges();

            return new JsonResult("Delete Success");
        }
    }
}
