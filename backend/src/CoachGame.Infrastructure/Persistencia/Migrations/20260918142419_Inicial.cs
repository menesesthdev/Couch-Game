using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachGame.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jogadores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    puuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    regiao = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rr_atual = table.Column<int>(type: "integer", nullable: false),
                    tier_atual = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tag = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jogadores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshots_rr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capturado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    partidas_analisadas = table.Column<int>(type: "integer", nullable: false),
                    win_rate = table.Column<double>(type: "double precision", nullable: false),
                    rr_medio_ganho = table.Column<double>(type: "double precision", nullable: false),
                    rr_medio_perdido = table.Column<double>(type: "double precision", nullable: false),
                    partidas_por_dia = table.Column<double>(type: "double precision", nullable: false),
                    rr = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_snapshots_rr", x => x.id);
                    table.ForeignKey(
                        name: "fk_snapshots_rr_jogadores_jogador_id",
                        column: x => x.jogador_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jogadores_puuid",
                table: "jogadores",
                column: "puuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_snapshots_rr_jogador_id_capturado_em",
                table: "snapshots_rr",
                columns: new[] { "jogador_id", "capturado_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snapshots_rr");

            migrationBuilder.DropTable(
                name: "jogadores");
        }
    }
}
