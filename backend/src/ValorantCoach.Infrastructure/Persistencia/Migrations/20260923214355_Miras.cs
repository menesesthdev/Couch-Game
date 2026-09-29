using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValorantCoach.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Miras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "miras",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    codigo = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    copias = table.Column<int>(type: "integer", nullable: false),
                    copias_na_semana = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_miras", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_miras_tipo_copias",
                table: "miras",
                columns: new[] { "tipo", "copias" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "miras");
        }
    }
}
