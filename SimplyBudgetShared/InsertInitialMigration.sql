CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
INSERT INTO "__EFMigrationsHistory" VALUES (
    "20201013035834_InitialCreate",
    "3.1.8"
)