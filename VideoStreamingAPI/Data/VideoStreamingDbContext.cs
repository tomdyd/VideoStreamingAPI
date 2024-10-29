using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoStreamingAPI.Models;
using VideoStreamingAPI.Models.VideoStreamingAPI.Models;

namespace VideoStreamingAPI.Data
{
    public class VideoStreamingDbContext : IdentityDbContext<AppUserModel>
    {
        public VideoStreamingDbContext(DbContextOptions<VideoStreamingDbContext> options) : base(options) { }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
