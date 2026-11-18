using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TourAgent.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    channel_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    metadata = table.Column<string>(type: "json", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contact_id = table.Column<long>(type: "bigint", nullable: true),
                    assigned_to = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crm_contacts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    telegram_chat_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    telegram_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    zalo_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "telegram"),
                    stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "prospect"),
                    tags = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    last_active_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "llm_calls",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversation_id = table.Column<long>(type: "bigint", nullable: true),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: false),
                    tools_called = table.Column<string>(type: "json", nullable: true),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    finish_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tool_payloads = table.Column<string>(type: "json", nullable: true),
                    status_bar_snapshot = table.Column<string>(type: "text", nullable: true),
                    state_snapshot = table.Column<string>(type: "json", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_calls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "travels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    destinations = table.Column<string>(type: "json", nullable: true),
                    tags = table.Column<string>(type: "json", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    route = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    departure_city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    duration_nights = table.Column<int>(type: "integer", nullable: false),
                    price_from = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    inclusions = table.Column<string>(type: "json", nullable: true),
                    exclusions = table.Column<string>(type: "json", nullable: true),
                    visa_policy = table.Column<string>(type: "text", nullable: true),
                    cancellation_policy = table.Column<string>(type: "text", nullable: true),
                    child_policy = table.Column<string>(type: "text", nullable: true),
                    payment_policy = table.Column<string>(type: "text", nullable: true),
                    itinerary = table.Column<string>(type: "json", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_travels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversation_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    channel_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tool_payload = table.Column<string>(type: "json", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_escalations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversation_id = table.Column<long>(type: "bigint", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    chat_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    discount_percent = table.Column<short>(type: "smallint", nullable: true),
                    group_size = table.Column<short>(type: "smallint", nullable: true),
                    travel_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    admin_message_id = table.Column<int>(type: "integer", nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    travel_resolution_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    travel_resolution_confidence = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    metadata = table.Column<string>(type: "json", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_escalations", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_escalations_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_agent_escalations_travels_travel_id",
                        column: x => x.travel_id,
                        principalTable: "travels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    travel_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tagline = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    target_audience = table.Column<string>(type: "text", nullable: true),
                    hero_mechanism = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    core_promise = table.Column<string>(type: "text", nullable: true),
                    original_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    offer_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    deposit_percentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    duration_nights = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    max_slots_per_week = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    available_slots = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    urgency_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    guarantees = table.Column<string>(type: "jsonb", nullable: true),
                    payment_terms = table.Column<string>(type: "jsonb", nullable: true),
                    sales_script = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_offers_travels_travel_id",
                        column: x => x.travel_id,
                        principalTable: "travels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "travel_departures",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    travel_id = table.Column<long>(type: "bigint", nullable: false),
                    departure_date = table.Column<DateOnly>(type: "date", nullable: false),
                    price_adult = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    price_child = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    single_supplement = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_slots = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    available_slots = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_travel_departures", x => x.id);
                    table.ForeignKey(
                        name: "FK_travel_departures_travels_travel_id",
                        column: x => x.travel_id,
                        principalTable: "travels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offer_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    offer_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    original_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_highlighted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_offer_items_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tour_orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_id = table.Column<long>(type: "bigint", nullable: true),
                    travel_id = table.Column<long>(type: "bigint", nullable: true),
                    travel_departure_id = table.Column<long>(type: "bigint", nullable: true),
                    conversation_id = table.Column<long>(type: "bigint", nullable: true),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "manual"),
                    pax_adults = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    pax_children = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    deposit_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    deposit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    deposit_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    deposit_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remaining_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    remaining_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    remaining_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_account_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    visa_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "not_required"),
                    visa_notes = table.Column<string>(type: "text", nullable: true),
                    operator_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    operator_booking_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    departure_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tour_guide_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tour_guide_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tour_guide_notes = table.Column<string>(type: "text", nullable: true),
                    pre_departure_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "lead_consulting"),
                    ai_summary = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tour_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_tour_orders_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tour_orders_crm_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "crm_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tour_orders_travel_departures_travel_departure_id",
                        column: x => x.travel_departure_id,
                        principalTable: "travel_departures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tour_orders_travels_travel_id",
                        column: x => x.travel_id,
                        principalTable: "travels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_escalations_conversation_id",
                table: "agent_escalations",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_escalations_travel_id",
                table: "agent_escalations",
                column: "travel_id");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_email",
                table: "crm_contacts",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_last_active",
                table: "crm_contacts",
                column: "last_active_at");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_phone",
                table: "crm_contacts",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_stage",
                table: "crm_contacts",
                column: "stage");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_telegram_chat_id",
                table: "crm_contacts",
                column: "telegram_chat_id");

            migrationBuilder.CreateIndex(
                name: "idx_crm_contacts_zalo_id",
                table: "crm_contacts",
                column: "zalo_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_channel_message_id",
                table: "messages",
                column: "channel_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_conversation_id",
                table: "messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_offer_items_offer_id_type",
                table: "offer_items",
                columns: new[] { "offer_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_offers_slug",
                table: "offers",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offers_status_is_featured",
                table: "offers",
                columns: new[] { "status", "is_featured" });

            migrationBuilder.CreateIndex(
                name: "IX_offers_travel_id",
                table: "offers",
                column: "travel_id");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_contact_id",
                table: "tour_orders",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_conversation_id",
                table: "tour_orders",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_departure_date",
                table: "tour_orders",
                column: "departure_date");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_order_code",
                table: "tour_orders",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_status",
                table: "tour_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_travel_departure_id",
                table: "tour_orders",
                column: "travel_departure_id");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_travel_id",
                table: "tour_orders",
                column: "travel_id");

            migrationBuilder.CreateIndex(
                name: "IX_tour_orders_visa_status",
                table: "tour_orders",
                column: "visa_status");

            migrationBuilder.CreateIndex(
                name: "IX_travel_departures_travel_id",
                table: "travel_departures",
                column: "travel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_escalations");

            migrationBuilder.DropTable(
                name: "llm_calls");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "offer_items");

            migrationBuilder.DropTable(
                name: "tour_orders");

            migrationBuilder.DropTable(
                name: "offers");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "crm_contacts");

            migrationBuilder.DropTable(
                name: "travel_departures");

            migrationBuilder.DropTable(
                name: "travels");
        }
    }
}
