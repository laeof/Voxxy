using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "followings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_followings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "genres",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_genres", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moods",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    image_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artists",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    image_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artists", x => x.id);
                    table.ForeignKey(
                        name: "fk_artists_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artists_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_artists_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    image_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlists", x => x.id);
                    table.ForeignKey(
                        name: "fk_playlists_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "releases",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    additional_information = table.Column<string>(type: "text", nullable: false),
                    copyright = table.Column<string>(type: "text", nullable: false),
                    image_key = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    release_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_releases", x => x.id);
                    table.ForeignKey(
                        name: "fk_releases_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_user",
                schema: "public",
                columns: table => new
                {
                    roles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    users_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_user", x => new { x.roles_id, x.users_id });
                    table.ForeignKey(
                        name: "fk_role_user_roles_roles_id",
                        column: x => x.roles_id,
                        principalSchema: "public",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_user_users_users_id",
                        column: x => x.users_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_release",
                schema: "public",
                columns: table => new
                {
                    artists_id = table.Column<Guid>(type: "uuid", nullable: false),
                    releases_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_release", x => new { x.artists_id, x.releases_id });
                    table.ForeignKey(
                        name: "fk_artist_release_artists_artists_id",
                        column: x => x.artists_id,
                        principalSchema: "public",
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artist_release_releases_releases_id",
                        column: x => x.releases_id,
                        principalSchema: "public",
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_key = table.Column<string>(type: "text", nullable: false),
                    audio_key = table.Column<string>(type: "text", nullable: false),
                    album_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracks_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "public",
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_track",
                schema: "public",
                columns: table => new
                {
                    artists_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracks_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_track", x => new { x.artists_id, x.tracks_id });
                    table.ForeignKey(
                        name: "fk_artist_track_artists_artists_id",
                        column: x => x.artists_id,
                        principalSchema: "public",
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artist_track_tracks_tracks_id",
                        column: x => x.tracks_id,
                        principalSchema: "public",
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "genre_track",
                schema: "public",
                columns: table => new
                {
                    genres_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracks_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_genre_track", x => new { x.genres_id, x.tracks_id });
                    table.ForeignKey(
                        name: "fk_genre_track_genres_genres_id",
                        column: x => x.genres_id,
                        principalSchema: "public",
                        principalTable: "genres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_genre_track_tracks_tracks_id",
                        column: x => x.tracks_id,
                        principalSchema: "public",
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mood_track",
                schema: "public",
                columns: table => new
                {
                    moods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracks_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mood_track", x => new { x.moods_id, x.tracks_id });
                    table.ForeignKey(
                        name: "fk_mood_track_moods_moods_id",
                        column: x => x.moods_id,
                        principalSchema: "public",
                        principalTable: "moods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mood_track_tracks_tracks_id",
                        column: x => x.tracks_id,
                        principalSchema: "public",
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_track",
                schema: "public",
                columns: table => new
                {
                    playlists_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracks_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlist_track", x => new { x.playlists_id, x.tracks_id });
                    table.ForeignKey(
                        name: "fk_playlist_track_playlists_playlists_id",
                        column: x => x.playlists_id,
                        principalSchema: "public",
                        principalTable: "playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_playlist_track_tracks_tracks_id",
                        column: x => x.tracks_id,
                        principalSchema: "public",
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_artist_release_releases_id",
                schema: "public",
                table: "artist_release",
                column: "releases_id");

            migrationBuilder.CreateIndex(
                name: "ix_artist_track_tracks_id",
                schema: "public",
                table: "artist_track",
                column: "tracks_id");

            migrationBuilder.CreateIndex(
                name: "ix_artists_created_by_user_id",
                schema: "public",
                table: "artists",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_artists_name",
                schema: "public",
                table: "artists",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artists_updated_by_user_id",
                schema: "public",
                table: "artists",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_artists_user_id",
                schema: "public",
                table: "artists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_genre_track_tracks_id",
                schema: "public",
                table: "genre_track",
                column: "tracks_id");

            migrationBuilder.CreateIndex(
                name: "ix_genres_name",
                schema: "public",
                table: "genres",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mood_track_tracks_id",
                schema: "public",
                table: "mood_track",
                column: "tracks_id");

            migrationBuilder.CreateIndex(
                name: "ix_moods_name",
                schema: "public",
                table: "moods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_role_id",
                schema: "public",
                table: "permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlist_track_tracks_id",
                schema: "public",
                table: "playlist_track",
                column: "tracks_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlists_created_by",
                schema: "public",
                table: "playlists",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "public",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_releases_approved_by_id",
                schema: "public",
                table: "releases",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_user_users_id",
                schema: "public",
                table: "role_user",
                column: "users_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                schema: "public",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracks_release_id",
                schema: "public",
                table: "tracks",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artist_release",
                schema: "public");

            migrationBuilder.DropTable(
                name: "artist_track",
                schema: "public");

            migrationBuilder.DropTable(
                name: "followings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "genre_track",
                schema: "public");

            migrationBuilder.DropTable(
                name: "mood_track",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "playlist_track",
                schema: "public");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "role_user",
                schema: "public");

            migrationBuilder.DropTable(
                name: "artists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "genres",
                schema: "public");

            migrationBuilder.DropTable(
                name: "moods",
                schema: "public");

            migrationBuilder.DropTable(
                name: "playlists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tracks",
                schema: "public");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "releases",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");
        }
    }
}
