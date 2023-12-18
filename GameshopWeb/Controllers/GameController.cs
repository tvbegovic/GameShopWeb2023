using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace GameshopWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public GameController(IConfiguration configuration) 
        {
            this.configuration = configuration;
        }

        [HttpGet("genres")]
        public List<Genre> GetGenres()
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                return db.Query<Genre>("SELECT * FROM Genre").ToList();
            }
        }

        [HttpGet("companies")]
        public List<Company> GetCompanies()
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                return db.Query<Company>("SELECT * FROM Company ORDER BY Name").ToList();
            }
        }

        [HttpGet("bygenre/{id}")]
        public List<Game> GetByGenre(int id)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                return db.Query<Game>("SELECT * FROM Game WHERE idGenre = @id", new { id }).ToList();
            }
        }

        [HttpGet("bycompany/{id}")]
        public List<Game> GetByCompany(int id)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                return db.Query<Game>("SELECT * FROM Game WHERE idPublisher = @id OR idDeveloper = @id",
                    new { id }).ToList();
            }
        }

        [HttpGet("search/{text}")]
        public List<Game> Search(string text)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                return db.Query<Game>(
                    @"SELECT Game.* FROM Game
                    INNER JOIN Genre ON Game.idGenre = Genre.id
                    INNER JOIN Company Publisher ON Game.idPublisher = Publisher.id
                    INNER JOIN Company Developer ON Game.idDeveloper = Developer.id
                    WHERE Game.title LIKE @text OR Genre.name LIKE @text
                    OR Publisher.name LIKE @text OR Developer.name LIKE @text",
                    new { text = $"%{text}%" }).ToList();
            }
        }
    }
}
