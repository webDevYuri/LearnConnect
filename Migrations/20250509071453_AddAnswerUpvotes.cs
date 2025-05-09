using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerUpvotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Upvotes",
                table: "Answers");

            migrationBuilder.CreateTable(
                name: "AnswerUpvotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerUpvotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerUpvotes_Answers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "Answers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerUpvotes_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerUpvotes_AnswerId",
                table: "AnswerUpvotes",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerUpvotes_UserProfileId",
                table: "AnswerUpvotes",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerUpvotes");

            migrationBuilder.AddColumn<int>(
                name: "Upvotes",
                table: "Answers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
