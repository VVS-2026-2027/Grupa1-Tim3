using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESportskiCentar.Data.Migrations
{
    /// <inheritdoc />
    public partial class DodanaPoljaIzvjestaj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ukupnaZarada",
                table: "Izvjestaj",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ukupnoRezervacija",
                table: "Izvjestaj",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ukupnaZarada",
                table: "Izvjestaj");

            migrationBuilder.DropColumn(
                name: "ukupnoRezervacija",
                table: "Izvjestaj");
        }
    }
}
