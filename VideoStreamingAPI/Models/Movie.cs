namespace VideoStreamingAPI.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SegmentsDirectory { get; set; }
        public string PlaylistFileName { get; set; }
    }
}
