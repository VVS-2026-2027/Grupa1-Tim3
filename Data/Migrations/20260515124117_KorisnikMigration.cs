using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESportskiCentar.Data.Migrations
{
    /// <inheritdoc />
    public partial class KorisnikMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Izvjestaj_Korisnik_korisnikID",
                table: "Izvjestaj");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervacija_Korisnik_korisnikID",
                table: "Rezervacija");

            migrationBuilder.DropTable(
                name: "Korisnik");

            migrationBuilder.AlterColumn<string>(
                name: "korisnikID",
                table: "Rezervacija",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "korisnikID",
                table: "Izvjestaj",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ime",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "prezime",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Izvjestaj_AspNetUsers_korisnikID",
                table: "Izvjestaj",
                column: "korisnikID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervacija_AspNetUsers_korisnikID",
                table: "Rezervacija",
                column: "korisnikID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Izvjestaj_AspNetUsers_korisnikID",
                table: "Izvjestaj");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervacija_AspNetUsers_korisnikID",
                table: "Rezervacija");

            migrationBuilder.DropColumn(
                name: "ime",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "prezime",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "korisnikID",
                table: "Rezervacija",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "korisnikID",
                table: "Izvjestaj",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Korisnik",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    korisnickoIme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lozinka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prezime = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnik", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Izvjestaj_Korisnik_korisnikID",
                table: "Izvjestaj",
                column: "korisnikID",
                principalTable: "Korisnik",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervacija_Korisnik_korisnikID",
                table: "Rezervacija",
                column: "korisnikID",
                principalTable: "Korisnik",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
