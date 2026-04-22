using Microsoft.EntityFrameworkCore;

namespace TreningsAppHaffi.Data
{
    public class MyDatabaseContext : DbContext
    {
        public MyDatabaseContext(DbContextOptions<MyDatabaseContext> options)
            : base(options)
        {
        }

        // dette er standard kode jeg kopierte fra nett (dvs bare denne konstructoren. Hvis jeg har lagt til noen DbSet-egenskaper,
        // så har jeg nok lagt de til selv), // -autogenerert(og det er en standard måte å sette opp en DbContext på i Entity Framework Core.)
        // (ganske spesielt at visual studio greier fylle inn så mye tekst for meg)

        // denne klassen representerer databasen,
        // og du kan legge til DbSet-egenskaper for hver tabell du vil ha i databasen.
        // For eksempel, hvis du har en "Products" tabell, kan du legge til en DbSet<Product> egenskap her.

        // Add DbSets for your tables here
        // public DbSet<Product> Products { get; set; }
    }
}
