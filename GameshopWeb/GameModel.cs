namespace GameshopWeb.Controllers
{
    public class GameModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? IdGenre { get; set; }
        public int? IdPublisher { get; set; }
        public decimal? Price { get; set; }
        public int? IdDeveloper { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Image { get; set; }
        public IFormFile? UploadedImage { get; set; }
    }
}
