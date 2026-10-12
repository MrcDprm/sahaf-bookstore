using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sahaf.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UseGuidCartIds : Migration
    {
        // Postgres cannot cast integer to uuid, so the key column is recreated.
        // Cart rows are short-lived; existing ones are dropped.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ShoppingCarts\";");
            migrationBuilder.DropPrimaryKey(name: "PK_ShoppingCarts", table: "ShoppingCarts");
            migrationBuilder.DropColumn(name: "Id", table: "ShoppingCarts");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "ShoppingCarts", type: "uuid", nullable: false);
            migrationBuilder.AddPrimaryKey(name: "PK_ShoppingCarts", table: "ShoppingCarts", column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ShoppingCarts\";");
            migrationBuilder.DropPrimaryKey(name: "PK_ShoppingCarts", table: "ShoppingCarts");
            migrationBuilder.DropColumn(name: "Id", table: "ShoppingCarts");
            migrationBuilder.AddColumn<int>(name: "Id", table: "ShoppingCarts", type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            migrationBuilder.AddPrimaryKey(name: "PK_ShoppingCarts", table: "ShoppingCarts", column: "Id");
        }
    }
}
