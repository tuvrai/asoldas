IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527103533_InitialCreate'
)
BEGIN
    CREATE TABLE [People] (
        [EntityId] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [BirthDate] date NOT NULL,
        [DeathDate] date NULL,
        CONSTRAINT [PK_People] PRIMARY KEY ([EntityId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527103533_InitialCreate'
)
BEGIN
    CREATE TABLE [WikiEvents] (
        [Id] nvarchar(450) NOT NULL,
        [Day] date NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WikiEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527103533_InitialCreate'
)
BEGIN
    CREATE TABLE [PersonWikiEvent] (
        [EventsId] nvarchar(450) NOT NULL,
        [PeopleEntityId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_PersonWikiEvent] PRIMARY KEY ([EventsId], [PeopleEntityId]),
        CONSTRAINT [FK_PersonWikiEvent_People_PeopleEntityId] FOREIGN KEY ([PeopleEntityId]) REFERENCES [People] ([EntityId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PersonWikiEvent_WikiEvents_EventsId] FOREIGN KEY ([EventsId]) REFERENCES [WikiEvents] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527103533_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonWikiEvent_PeopleEntityId] ON [PersonWikiEvent] ([PeopleEntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527103533_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250527103533_InitialCreate', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250527104401_mssql.local_migration_768'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250527104401_mssql.local_migration_768', N'9.0.5');
END;

COMMIT;
GO

