using PosPlatform.Application.Interfaces;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Infrastructure.Seed;

namespace PosPlatform.Infrastructure.Services
{
    public class AppDbSeederService : IAppDbSeeder
    {
        private readonly AppDbContext _context;

        public AppDbSeederService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await AppDbSeeder.SeedAsync(_context);
        }
    }
}