using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollabEditor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateParticipants : Migration
    {
        private static readonly string[] columns = new[] { "SessionId", "ParticipantId" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Participants_SessionId_ParticipantId",
                table: "Participants",
                columns: columns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
            name: "IX_Participants_SessionId_ParticipantId",
            table: "Participants");
        }
    }
}
