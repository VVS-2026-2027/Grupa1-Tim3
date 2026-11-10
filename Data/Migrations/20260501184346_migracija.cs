using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESportskiCentar.Data.Migrations
{
    /// <inheritdoc />
    public partial class migracija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Korisnik",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prezime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lozinka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    korisnickoIme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnik", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Popust",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    potrebanBrojRezervacija = table.Column<int>(type: "int", nullable: false),
                    procenat = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Popust", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Teren",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    naziv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sport = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teren", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Izvjestaj",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    datumGenerisanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    korisnikID = table.Column<int>(type: "int", nullable: false),
                    datumOd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    datumDo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Izvjestaj", x => x.id);
                    table.ForeignKey(
                        name: "FK_Izvjestaj_Korisnik_korisnikID",
                        column: x => x.korisnikID,
                        principalTable: "Korisnik",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Termin",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cijena = table.Column<double>(type: "float", nullable: false),
                    rezervisan = table.Column<bool>(type: "bit", nullable: false),
                    terenID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Termin", x => x.id);
                    table.ForeignKey(
                        name: "FK_Termin_Teren_terenID",
                        column: x => x.terenID,
                        principalTable: "Teren",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rezervacija",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    status = table.Column<int>(type: "int", nullable: false),
                    terminID = table.Column<int>(type: "int", nullable: false),
                    korisnikID = table.Column<int>(type: "int", nullable: false),
                    vrijemeRezervacije = table.Column<DateTime>(type: "datetime2", nullable: false),
                    primjenjenPopust = table.Column<bool>(type: "bit", nullable: false),
                    konacnaCijena = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervacija", x => x.id);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Korisnik_korisnikID",
                        column: x => x.korisnikID,
                        principalTable: "Korisnik",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Termin_terminID",
                        column: x => x.terminID,
                        principalTable: "Termin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifikacija",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poslana = table.Column<bool>(type: "bit", nullable: false),
                    vrijemeSlanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    rezervacijaID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifikacija", x => x.id);
                    table.ForeignKey(
                        name: "FK_Notifikacija_Rezervacija_rezervacijaID",
                        column: x => x.rezervacijaID,
                        principalTable: "Rezervacija",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Izvjestaj_korisnikID",
                table: "Izvjestaj",
                column: "korisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikacija_rezervacijaID",
                table: "Notifikacija",
                column: "rezervacijaID");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_korisnikID",
                table: "Rezervacija",
                column: "korisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_terminID",
                table: "Rezervacija",
                column: "terminID");

            migrationBuilder.CreateIndex(
                name: "IX_Termin_terenID",
                table: "Termin",
                column: "terenID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Izvjestaj");

            migrationBuilder.DropTable(
                name: "Notifikacija");

            migrationBuilder.DropTable(
                name: "Popust");

            migrationBuilder.DropTable(
                name: "Rezervacija");

            migrationBuilder.DropTable(
                name: "Korisnik");

            migrationBuilder.DropTable(
                name: "Termin");

            migrationBuilder.DropTable(
                name: "Teren");
        }
    }
}
