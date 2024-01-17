using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment webHostEnvironment;

        public GameController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment) 
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
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

        [HttpGet("")]
        [Authorize]
        public List<Game> GetAll()
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                var games = db.Query<Game>("SELECT * FROM Game").ToList();
                var genres = db.Query<Genre>("SELECT * FROM Genre").ToList();
                var companies = db.Query<Company>("SELECT * FROM Company").ToList();
                foreach (var game in games)
                {
                    game.Genre = genres.FirstOrDefault(g => g.Id == game.IdGenre);
                    game.Developer = companies.FirstOrDefault(c => c.Id == game.IdDeveloper);
                    game.Publisher = companies.FirstOrDefault(c => c.Id >= game.IdPublisher);
                }
                return games;
            }
        }

        [HttpGet("editModel/{id}")]
        [Authorize]
        public EditModel GetEditModel(int id)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                var editModel = new EditModel();
                editModel.Genres = db.Query<Genre>("SELECT * FROM Genre ORDER BY name").ToList(); 
                editModel.Companies = db.Query<Company>("SELECT * FROM Company ORDER BY name").ToList();
                if (id == 0)
                    editModel.Game = new Game();
                else
                    editModel.Game = db.QueryFirstOrDefault<Game>("SELECT * FROM Game WHERE id = @id",
                        new { id });
                return editModel;
            }
        }

        [HttpPost("")]
        [Authorize]
        public void Create([FromForm] GameModel model)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                if(model.UploadedImage != null)
                {
                    StoreFile(model.UploadedImage);
                    model.Image = model.UploadedImage.FileName;
                }
                var sql = @"INSERT INTO [dbo].[Game]
                       ([title],[idGenre],[idPublisher]
                       ,[price],[idDeveloper],[releaseDate]
                       ,[image]) OUTPUT inserted.id
                 VALUES
                       (@title,@idGenre,@idPublisher
                       ,@price,@idDeveloper,@releaseDate
                       ,@image)";
                db.Execute(sql, model);
            }
        }

        private void StoreFile(IFormFile uploadedImage)
        {
            var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "assets/gameimages");
            var filePath = Path.Combine(uploadsFolder, uploadedImage.FileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                uploadedImage.CopyTo(fileStream);
            }
        }

        [HttpPut("")]
        [Authorize]
        public void Update([FromForm] GameModel model)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                if (model.UploadedImage != null)
                {
                    if (model.Image != null)
                    {
                        DeleteFile(model.Image);
                    }
                    StoreFile(model.UploadedImage);
                    model.Image = model.UploadedImage.FileName;
                }
                var sql = @"UPDATE [dbo].[Game]
                    SET [title] = @title
                        ,[idGenre] = @idGenre
                        ,[idPublisher] = @idPublisher
                        ,[price] = @price
                        ,[idDeveloper] = @idDeveloper
                        ,[releaseDate] = @releaseDate
                        ,[image] = @image
                    WHERE id = @id";
                db.Execute(sql, model);
                
            }
        }

        private void DeleteFile(string image)
        {
            var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "assets/gameimages");
            var filePath = Path.Combine(uploadsFolder, image);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public void Delete(int id)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                db.Execute("DELETE FROM Game WHERE id = @id", new { id });
            }
        }
    }
}
