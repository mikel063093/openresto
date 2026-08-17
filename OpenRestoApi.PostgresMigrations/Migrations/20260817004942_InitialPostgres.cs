using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenRestoApi.PostgresMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordSalt = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PvqQuestion = table.Column<string>(type: "text", nullable: true),
                    PvqAnswerHash = table.Column<string>(type: "text", nullable: true),
                    PvqAnswerSalt = table.Column<string>(type: "text", nullable: true),
                    ResetToken = table.Column<string>(type: "text", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PrimaryColor = table.Column<string>(type: "text", nullable: false),
                    AccentColor = table.Column<string>(type: "text", nullable: true),
                    HeaderImageUrl = table.Column<string>(type: "text", nullable: true),
                    FaviconIcon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    CopyrightText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    HighlightsHeading = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    HighlightsSubheading = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    HeaderImageFit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMutationIdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    MutationScope = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplayKey = table.Column<string>(type: "text", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMutationIdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailFailures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingRef = table.Column<string>(type: "text", nullable: true),
                    RecipientEmail = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailFailures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Host = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    EncryptedPassword = table.Column<string>(type: "text", nullable: false),
                    EnableSsl = table.Column<bool>(type: "boolean", nullable: false),
                    FromName = table.Column<string>(type: "text", nullable: true),
                    FromEmail = table.Column<string>(type: "text", nullable: true),
                    SendBookingConfirmations = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Highlights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IconKey = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Highlights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperatorPrincipals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    NormalizedIdentifier = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorPrincipals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    OpenTime = table.Column<string>(type: "text", nullable: false),
                    CloseTime = table.Column<string>(type: "text", nullable: false),
                    OpenDays = table.Column<string>(type: "text", nullable: false),
                    OpenHoursJson = table.Column<string>(type: "text", nullable: true),
                    Timezone = table.Column<string>(type: "text", nullable: false),
                    BookingsPausedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MenuUrl = table.Column<string>(type: "text", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    IsWhatsAppTestEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HandoffWhatsAppE164 = table.Column<string>(type: "text", nullable: true),
                    WalkInOnly = table.Column<bool>(type: "boolean", nullable: false),
                    WalkInDays = table.Column<string>(type: "text", nullable: true),
                    DefaultBookingDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    BookingSlotIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxTableOversizeSeats = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IconKey = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperatorAgentCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorPrincipalId = table.Column<int>(type: "integer", nullable: false),
                    CredentialKeyId = table.Column<string>(type: "text", nullable: false),
                    TokenDigest = table.Column<string>(type: "text", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationPreset = table.Column<string>(type: "text", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorAgentCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentials_OperatorPrincipals_OperatorPrincip~",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminPushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminPushSubscriptions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorRestaurantScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorPrincipalId = table.Column<int>(type: "integer", nullable: false),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorRestaurantScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorRestaurantScopes_OperatorPrincipals_OperatorPrincip~",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorRestaurantScopes_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOccasionCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EstimatedPriceCop = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOccasionCatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOccasionCatalogItems_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminCredentialManagementAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorAdminCredentialId = table.Column<int>(type: "integer", nullable: true),
                    ActorEmailSnapshot = table.Column<string>(type: "text", nullable: false),
                    TargetOperatorPrincipalId = table.Column<int>(type: "integer", nullable: true),
                    OperatorAgentCredentialId = table.Column<int>(type: "integer", nullable: true),
                    CredentialKeyIdSnapshot = table.Column<string>(type: "text", nullable: true),
                    TargetOperatorIdentifierSnapshot = table.Column<string>(type: "text", nullable: false),
                    ScopeRestaurantIdsSnapshot = table.Column<string>(type: "text", nullable: false),
                    TtlHoursSnapshot = table.Column<int>(type: "integer", nullable: true),
                    ExpirationPresetSnapshot = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCredentialManagementAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_AdminCredentials_ActorAdmin~",
                        column: x => x.ActorAdminCredentialId,
                        principalTable: "AdminCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_OperatorAgentCredentials_Op~",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_OperatorPrincipals_TargetOp~",
                        column: x => x.TargetOperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OperatorAgentCredentialScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorAgentCredentialId = table.Column<int>(type: "integer", nullable: false),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorAgentCredentialScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentialScopes_OperatorAgentCredentials_Oper~",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentialScopes_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tables_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    SectionId = table.Column<int>(type: "integer", nullable: true),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerEmail = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    CustomerPhoneE164 = table.Column<string>(type: "text", nullable: true),
                    CustomerPhoneNormalized = table.Column<string>(type: "text", nullable: true),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    SpecialRequests = table.Column<string>(type: "text", nullable: true),
                    BookingRef = table.Column<string>(type: "text", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByOperatorId = table.Column<int>(type: "integer", nullable: true),
                    CreatedViaChannel = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyToken = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_OperatorPrincipals_CreatedByOperatorId",
                        column: x => x.CreatedByOperatorId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bookings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bookings_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bookings_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    BookingRef = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    RestaurantName = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PushSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PushError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingOccasionSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    RestaurantOccasionCatalogItemId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EstimatedPriceCop = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingOccasionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingOccasionSnapshots_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorActionAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorPrincipalId = table.Column<int>(type: "integer", nullable: true),
                    OperatorPrincipalIdSnapshot = table.Column<int>(type: "integer", nullable: false),
                    OperatorAgentCredentialId = table.Column<int>(type: "integer", nullable: true),
                    RestaurantId = table.Column<int>(type: "integer", nullable: true),
                    RestaurantIdSnapshot = table.Column<int>(type: "integer", nullable: false),
                    RestaurantNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorActionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_OperatorAgentCredentials_OperatorAgent~",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppHandoffAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    VerifiedPhoneE164 = table.Column<string>(type: "text", nullable: false),
                    VerifiedPhoneNormalized = table.Column<string>(type: "text", nullable: false),
                    SummarySnapshot = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    HandoffDestinationSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppHandoffAudits", x => x.Id);
                    table.CheckConstraint("CK_WhatsAppHandoffAudits_HandoffDestinationSnapshot_MaxLength", "length(\"HandoffDestinationSnapshot\") <= 32");
                    table.CheckConstraint("CK_WhatsAppHandoffAudits_SummarySnapshot_MaxLength", "length(\"SummarySnapshot\") <= 1024");
                    table.ForeignKey(
                        name: "FK_WhatsAppHandoffAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhatsAppHandoffAudits_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_ActorAdminCredentialId",
                table: "AdminCredentialManagementAudits",
                column: "ActorAdminCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_CreatedAtUtc",
                table: "AdminCredentialManagementAudits",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_OperatorAgentCredentialId_C~",
                table: "AdminCredentialManagementAudits",
                columns: new[] { "OperatorAgentCredentialId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_TargetOperatorPrincipalId_C~",
                table: "AdminCredentialManagementAudits",
                columns: new[] { "TargetOperatorPrincipalId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentials_Email",
                table: "AdminCredentials",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_BookingId",
                table: "AdminNotifications",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_RestaurantId_CreatedAt",
                table: "AdminNotifications",
                columns: new[] { "RestaurantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_RestaurantId_IsRead",
                table: "AdminNotifications",
                columns: new[] { "RestaurantId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminPushSubscriptions_Endpoint_RestaurantId",
                table: "AdminPushSubscriptions",
                columns: new[] { "Endpoint", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminPushSubscriptions_RestaurantId",
                table: "AdminPushSubscriptions",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingOccasionSnapshots_BookingId",
                table: "BookingOccasionSnapshots",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalo~",
                table: "BookingOccasionSnapshots",
                columns: new[] { "BookingId", "RestaurantOccasionCatalogItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedByOperatorId_RestaurantId_Date",
                table: "Bookings",
                columns: new[] { "CreatedByOperatorId", "RestaurantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerPhoneNormalized_RestaurantId_Date",
                table: "Bookings",
                columns: new[] { "CustomerPhoneNormalized", "RestaurantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RestaurantId",
                table: "Bookings",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SectionId",
                table: "Bookings",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TableId",
                table: "Bookings",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMutationIdempotencyRecords_Channel_MutationScope_Ide~",
                table: "ChannelMutationIdempotencyRecords",
                columns: new[] { "Channel", "MutationScope", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMutationIdempotencyRecords_Channel_ReplayKey",
                table: "ChannelMutationIdempotencyRecords",
                columns: new[] { "Channel", "ReplayKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_BookingId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "BookingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_OperatorAgentCredentialId",
                table: "OperatorActionAudits",
                column: "OperatorAgentCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_OperatorPrincipalId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "OperatorPrincipalId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_RestaurantId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "RestaurantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_CredentialKeyId",
                table: "OperatorAgentCredentials",
                column: "CredentialKeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_ExpiresAt_RevokedAt",
                table: "OperatorAgentCredentials",
                columns: new[] { "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_OperatorPrincipalId",
                table: "OperatorAgentCredentials",
                column: "OperatorPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentialScopes_OperatorAgentCredentialId_Res~",
                table: "OperatorAgentCredentialScopes",
                columns: new[] { "OperatorAgentCredentialId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentialScopes_RestaurantId",
                table: "OperatorAgentCredentialScopes",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorPrincipals_NormalizedIdentifier",
                table: "OperatorPrincipals",
                column: "NormalizedIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorRestaurantScopes_OperatorPrincipalId_RestaurantId",
                table: "OperatorRestaurantScopes",
                columns: new[] { "OperatorPrincipalId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorRestaurantScopes_RestaurantId",
                table: "OperatorRestaurantScopes",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOccasionCatalogItems_RestaurantId_SortOrder",
                table: "RestaurantOccasionCatalogItems",
                columns: new[] { "RestaurantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Sections_RestaurantId",
                table: "Sections",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_SectionId",
                table: "Tables",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_BookingId",
                table: "WhatsAppHandoffAudits",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_RestaurantId_CreatedAtUtc",
                table: "WhatsAppHandoffAudits",
                columns: new[] { "RestaurantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_VerifiedPhoneNormalized_CreatedAtUtc",
                table: "WhatsAppHandoffAudits",
                columns: new[] { "VerifiedPhoneNormalized", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminCredentialManagementAudits");

            migrationBuilder.DropTable(
                name: "AdminNotifications");

            migrationBuilder.DropTable(
                name: "AdminPushSubscriptions");

            migrationBuilder.DropTable(
                name: "BookingOccasionSnapshots");

            migrationBuilder.DropTable(
                name: "BrandSettings");

            migrationBuilder.DropTable(
                name: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropTable(
                name: "EmailFailures");

            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.DropTable(
                name: "Highlights");

            migrationBuilder.DropTable(
                name: "OperatorActionAudits");

            migrationBuilder.DropTable(
                name: "OperatorAgentCredentialScopes");

            migrationBuilder.DropTable(
                name: "OperatorRestaurantScopes");

            migrationBuilder.DropTable(
                name: "RestaurantOccasionCatalogItems");

            migrationBuilder.DropTable(
                name: "SocialLinks");

            migrationBuilder.DropTable(
                name: "WhatsAppHandoffAudits");

            migrationBuilder.DropTable(
                name: "AdminCredentials");

            migrationBuilder.DropTable(
                name: "OperatorAgentCredentials");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "OperatorPrincipals");

            migrationBuilder.DropTable(
                name: "Tables");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Restaurants");
        }
    }
}
