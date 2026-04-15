using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class UniqueIndexMessageEmbeddingsMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageEmbeddings_MessageId",
                table: "MessageEmbeddings");

            migrationBuilder.CreateIndex(
                name: "IX_MessageEmbeddings_MessageId",
                table: "MessageEmbeddings",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageEmbeddings_MessageId",
                table: "MessageEmbeddings");

            migrationBuilder.CreateIndex(
                name: "IX_MessageEmbeddings_MessageId",
                table: "MessageEmbeddings",
                column: "MessageId");
        }
    }
}

