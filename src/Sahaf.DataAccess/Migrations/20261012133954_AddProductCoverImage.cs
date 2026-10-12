using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahaf.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverImageId",
                table: "Products",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Products");
        }
    }
}
