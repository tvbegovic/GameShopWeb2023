using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace GameshopWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public UserController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            if(string.IsNullOrEmpty(user.Firstname) || string.IsNullOrEmpty(user.Lastname) ||
                string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Ime, prezime, email i lozinka moraju biti zadani");
            }
            if(user.Password.Length < 8)
            {
                return BadRequest("Lozinka mora imati najmanje 8 znakova");
            }
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                var count = db.ExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM [User] WHERE email = @email", new { email = user.Email });
                if(count > 0)
                {
                    return BadRequest("Već postoji korisnik s tom email adresom");
                }
                var sql = @"INSERT INTO [User] (firstname, lastname, address, email, City, password) OUTPUT inserted.id
                        VALUES(@firstname, @lastname, @address, @email, @City,@password)";
                db.Execute(sql, user);
            }
            SendVerificationMail(user);
            return Ok();
        }

        private void SendVerificationMail(User user)
        {
            var emailSettings = configuration.GetSection("emailSettings").Get<EmailSettings>();
            var smtpClient = new SmtpClient(emailSettings.Server)
            {
                Port = 587,
                Credentials = new NetworkCredential(emailSettings.Email, emailSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings.Email),
                Subject = "Registration confirmation",
                Body = "Please verify your email at the following link: ...",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(user.Email);

            smtpClient.Send(mailMessage);
        }

        [HttpGet("login")]
        public IActionResult Login(string email, string password)
        {
            using(var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                var user = db.QueryFirstOrDefault<User>(
                    "SELECT * FROM [user] WHERE email = @email AND password = @password",
                    new { email, password });
                if (user == null)
                {
                    return BadRequest("Invalid login or password");
                }
                var loginResult = new LoginResult();
                loginResult.User = user;
                return Ok(loginResult);
            }
        }

        [HttpPut("")]
        public IActionResult Update(User user)
        {
            if (string.IsNullOrEmpty(user.Firstname) || string.IsNullOrEmpty(user.Lastname) ||
                string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Ime, prezime, email i lozinka moraju biti zadani");
            }
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                db.Execute(
                            @"UPDATE [User] SET firstname = @firstname, lastname = @lastname,
                                address = @address, city = @city WHERE id = @id", user);
                return Ok();
            }
        }
    }

    public class LoginResult
    {
        public User User { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
