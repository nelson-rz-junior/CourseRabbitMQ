using CourseDataAccess.Data.Context;
using CourseDataAccess.Data.Interfaces;
using CourseDataAccess.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourseConsumerProducer
{
    public static class RepositoryService
    {
        public static ServiceProvider GetServiceProvider()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var services = new ServiceCollection();
            services.AddDbContext<CourseContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<ICourseRepository, CourseRepository>();

            return services.BuildServiceProvider();
        }
    }
}
