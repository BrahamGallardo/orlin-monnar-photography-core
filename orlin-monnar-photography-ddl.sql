CREATE TABLE [AuthRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(250) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_AuthRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ContactMessages] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RespondedAt] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GalleryCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Slug] nvarchar(150) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_GalleryCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Packages] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Includes] nvarchar(2000) NULL,
    [Duration] nvarchar(100) NULL,
    [Price] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_Packages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RevokedTokens] (
    [Id] int NOT NULL IDENTITY,
    [Token] nvarchar(850) NOT NULL,
    [RevokedAt] datetime2 NOT NULL,
    [UserId] int NULL,
    [Reason] nvarchar(250) NULL,
    [ExpiresAt] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_RevokedTokens] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AuthUsers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(100) NOT NULL,
    [RoleId] int NOT NULL,
    [EmailVerified] bit NOT NULL,
    [HasPassword] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_AuthUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuthUsers_AuthRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AuthRoles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Photos] (
    [Id] int NOT NULL IDENTITY,
    [GalleryCategoryId] int NOT NULL,
    [Title] nvarchar(200) NULL,
    [AltText] nvarchar(300) NULL,
    [ThumbPath] nvarchar(500) NOT NULL,
    [MediumPath] nvarchar(500) NOT NULL,
    [LargePath] nvarchar(500) NOT NULL,
    [Width] int NOT NULL,
    [Height] int NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsFeatured] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_Photos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Photos_GalleryCategories_GalleryCategoryId] FOREIGN KEY ([GalleryCategoryId]) REFERENCES [GalleryCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [PackageId] int NOT NULL,
    [AppointmentDate] datetime2 NOT NULL,
    [Location] nvarchar(250) NULL,
    [Notes] nvarchar(2000) NULL,
    [Status] nvarchar(50) NOT NULL,
    [ConfirmedDate] datetime2 NULL,
    [CancelledDate] datetime2 NULL,
    [AdminNotes] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AuthSessions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedDate] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [Activated] bit NOT NULL,
    CONSTRAINT [PK_AuthSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuthSessions_AuthUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AuthUsers] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activated', N'CreatedBy', N'CreatedDate', N'Description', N'Name', N'UpdatedBy', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[AuthRoles]'))
    SET IDENTITY_INSERT [AuthRoles] ON;
INSERT INTO [AuthRoles] ([Id], [Activated], [CreatedBy], [CreatedDate], [Description], [Name], [UpdatedBy], [UpdatedDate])
VALUES (1, CAST(1 AS bit), NULL, '2026-01-01T00:00:00.0000000Z', N'Administrador con acceso total al sistema', N'Admin', NULL, NULL),
(2, CAST(1 AS bit), NULL, '2026-01-01T00:00:00.0000000Z', N'Usuario con acceso limitado', N'User', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activated', N'CreatedBy', N'CreatedDate', N'Description', N'Name', N'UpdatedBy', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[AuthRoles]'))
    SET IDENTITY_INSERT [AuthRoles] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activated', N'CreatedBy', N'CreatedDate', N'Email', N'EmailVerified', N'HasPassword', N'LastName', N'Name', N'PasswordHash', N'RoleId', N'UpdatedBy', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[AuthUsers]'))
    SET IDENTITY_INSERT [AuthUsers] ON;
INSERT INTO [AuthUsers] ([Id], [Activated], [CreatedBy], [CreatedDate], [Email], [EmailVerified], [HasPassword], [LastName], [Name], [PasswordHash], [RoleId], [UpdatedBy], [UpdatedDate])
VALUES (1, CAST(1 AS bit), NULL, '2026-01-01T00:00:00.0000000Z', N'braham.gc@gmail.com', CAST(1 AS bit), CAST(1 AS bit), N'Cruz', N'Abraham', N'$2a$12$1djuNPTG9ai6nB4FX3F6megWmLCGeSd1kKBI8qToJ3X8Yg6x5G7F6', 1, NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activated', N'CreatedBy', N'CreatedDate', N'Email', N'EmailVerified', N'HasPassword', N'LastName', N'Name', N'PasswordHash', N'RoleId', N'UpdatedBy', N'UpdatedDate') AND [object_id] = OBJECT_ID(N'[AuthUsers]'))
    SET IDENTITY_INSERT [AuthUsers] OFF;
GO


CREATE INDEX [IX_Appointments_AppointmentDate] ON [Appointments] ([AppointmentDate]);
GO


CREATE INDEX [IX_Appointments_Email] ON [Appointments] ([Email]);
GO


CREATE INDEX [IX_Appointments_PackageId] ON [Appointments] ([PackageId]);
GO


CREATE INDEX [IX_Appointments_Status] ON [Appointments] ([Status]);
GO


CREATE UNIQUE INDEX [IX_AuthRoles_Name] ON [AuthRoles] ([Name]);
GO


CREATE INDEX [IX_AuthSessions_IsActive] ON [AuthSessions] ([IsActive]);
GO


CREATE INDEX [IX_AuthSessions_UserId] ON [AuthSessions] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_AuthUsers_Email] ON [AuthUsers] ([Email]);
GO


CREATE INDEX [IX_AuthUsers_RoleId] ON [AuthUsers] ([RoleId]);
GO


CREATE INDEX [IX_ContactMessages_CreatedDate] ON [ContactMessages] ([CreatedDate]);
GO


CREATE INDEX [IX_ContactMessages_Email] ON [ContactMessages] ([Email]);
GO


CREATE INDEX [IX_ContactMessages_Status] ON [ContactMessages] ([Status]);
GO


CREATE INDEX [IX_GalleryCategories_DisplayOrder] ON [GalleryCategories] ([DisplayOrder]);
GO


CREATE UNIQUE INDEX [IX_GalleryCategories_Slug] ON [GalleryCategories] ([Slug]);
GO


CREATE INDEX [IX_Packages_DisplayOrder] ON [Packages] ([DisplayOrder]);
GO


CREATE INDEX [IX_Photos_GalleryCategoryId_DisplayOrder] ON [Photos] ([GalleryCategoryId], [DisplayOrder]);
GO


CREATE INDEX [IX_Photos_IsFeatured] ON [Photos] ([IsFeatured]);
GO


CREATE INDEX [IX_RevokedTokens_ExpiresAt] ON [RevokedTokens] ([ExpiresAt]);
GO


CREATE INDEX [IX_RevokedTokens_Token] ON [RevokedTokens] ([Token]);
GO


