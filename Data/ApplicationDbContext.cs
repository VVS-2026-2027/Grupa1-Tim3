using ESportskiCentar.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ESportskiCentar.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<Korisnik>(options)
    {
        public DbSet<Korisnik> Korisnici { get; set; }
        public DbSet<Teren> Tereni { get; set; }
        public DbSet<Termin> Termini { get; set; }
        public DbSet<Rezervacija> Rezervacije { get; set; }
        public DbSet<Notifikacija> Notifikacije { get; set; }
        public DbSet<Izvjestaj> Izvjestaji { get; set; }
        public DbSet<Popust> Popusti { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Korisnik>(b =>
            {
                b.Property(u => u.ime);
                b.Property(u => u.prezime);
            });

            modelBuilder.Entity<Teren>().ToTable("Teren");
            modelBuilder.Entity<Termin>().ToTable("Termin");
            modelBuilder.Entity<Rezervacija>().ToTable("Rezervacija");
            modelBuilder.Entity<Notifikacija>().ToTable("Notifikacija");
            modelBuilder.Entity<Izvjestaj>().ToTable("Izvjestaj");
            modelBuilder.Entity<Popust>().ToTable("Popust");

            base.OnModelCreating(modelBuilder);
        }
    }
}
