using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNomorInduk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomorIndukMahasiswa",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomorIndukMahasiswa",
                table: "Students");
        }
    }
}
