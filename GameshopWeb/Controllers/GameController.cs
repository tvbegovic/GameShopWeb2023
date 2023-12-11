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
    }
}
