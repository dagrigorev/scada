using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RapidScada.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    channel_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    connection_settings = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_type_id = table.Column<int>(type: "integer", nullable: false),
                    address = table.Column<int>(type: "integer", nullable: false),
                    call_sign = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    communication_line_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_communication_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_devices_communication_lines_communication_line_id",
                        column: x => x.communication_line_id,
                        principalTable: "communication_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    tag_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    units = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    current_value = table.Column<string>(type: "jsonb", nullable: true),
                    last_update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    low_limit = table.Column<double>(type: "double precision", nullable: true),
                    high_limit = table.Column<double>(type: "double precision", nullable: true),
                    formula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_tags_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_communication_lines_channel_type",
                table: "communication_lines",
                column: "channel_type");

            migrationBuilder.CreateIndex(
                name: "idx_communication_lines_is_active",
                table: "communication_lines",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_communication_lines_name",
                table: "communication_lines",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_devices_communication_line_id",
                table: "devices",
                column: "communication_line_id");

            migrationBuilder.CreateIndex(
                name: "idx_devices_last_communication_at",
                table: "devices",
                column: "last_communication_at");

            migrationBuilder.CreateIndex(
                name: "idx_devices_name",
                table: "devices",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_devices_status",
                table: "devices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_tags_device_id_number",
                table: "tags",
                columns: new[] { "device_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tags_last_update_at",
                table: "tags",
                column: "last_update_at");

            migrationBuilder.CreateIndex(
                name: "idx_tags_status",
                table: "tags",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "communication_lines");
        }
    }
}
