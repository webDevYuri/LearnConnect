using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddUpvotesToAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Upvotes",
                table: "Answers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Upvotes",
                table: "Answers");
        }
    }
}
