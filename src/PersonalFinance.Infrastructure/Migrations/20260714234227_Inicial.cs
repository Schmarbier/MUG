using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PersonalFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    FechaMensaje = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Procesado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Monedas",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TipoCambio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    EsBase = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monedas", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MensajeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MonedaCodigo = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TipoCambioHistorico = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Movimientos_Mensajes_MensajeId",
                        column: x => x.MensajeId,
                        principalTable: "Mensajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Movimientos_Monedas_MonedaCodigo",
                        column: x => x.MonedaCodigo,
                        principalTable: "Monedas",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Activa", "Descripcion", "Titulo" },
                values: new object[,]
                {
                    { 1, true, "Gastos del hogar", "Hogar" },
                    { 2, true, "Ingresos por sueldo", "Sueldo" },
                    { 3, true, "Ahorros e inversiones", "Ahorro" },
                    { 4, true, "Entretenimiento y tiempo libre", "Ocio" }
                });

            migrationBuilder.InsertData(
                table: "Monedas",
                columns: new[] { "Codigo", "EsBase", "TipoCambio" },
                values: new object[] { "ARS", true, 1m });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Titulo",
                table: "Categorias",
                column: "Titulo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_MessageId",
                table: "Mensajes",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CategoriaId",
                table: "Movimientos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_MensajeId",
                table: "Movimientos",
                column: "MensajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_MonedaCodigo",
                table: "Movimientos",
                column: "MonedaCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "Monedas");
        }
    }
}
