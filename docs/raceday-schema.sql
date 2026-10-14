-- ============================================================
-- RaceDay System - Database Schema (revision 2)
-- Matches the ERD in /docs/raceday_erd.png
-- Revision 2 adds Event.DistanceKm and Event.EventType, which the
-- Part 2 brief requires for every event.
-- Run in SSMS. Safe to re-run: it drops and recreates the tables.
-- ============================================================

IF DB_ID('RaceDayDB') IS NULL
    CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- Drop in reverse dependency order so the script can be re-run
DROP TABLE IF EXISTS Result;
DROP TABLE IF EXISTS Enrolment;
DROP TABLE IF EXISTS Category;
DROP TABLE IF EXISTS Event;
DROP TABLE IF EXISTS UserProfile;
DROP TABLE IF EXISTS [User];
GO

-- ============================================================
-- Table: User  (authentication record + role)
-- ============================================================
CREATE TABLE [User] (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Email        NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role         NVARCHAR(50)  NOT NULL,
    CONSTRAINT UQ_User_Email UNIQUE (Email),
    CONSTRAINT CHK_User_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- ============================================================
-- Table: UserProfile  (1:1 with User)
-- ============================================================
CREATE TABLE UserProfile (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    UserId   INT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone    NVARCHAR(20)  NULL,
    Bio      NVARCHAR(500) NULL,
    CONSTRAINT UQ_UserProfile_UserId UNIQUE (UserId),
    CONSTRAINT FK_UserProfile_User FOREIGN KEY (UserId)
        REFERENCES [User](Id) ON DELETE CASCADE
);
GO

-- ============================================================
-- Table: Event  (created by an Organiser)
-- ============================================================
CREATE TABLE Event (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    Name        NVARCHAR(150)  NOT NULL,
    Description NVARCHAR(1000) NULL,
    Date        DATETIME       NOT NULL,
    Location    NVARCHAR(255)  NOT NULL,
    DistanceKm  DECIMAL(6,2)   NOT NULL,
    EventType   NVARCHAR(10)   NOT NULL,
    CONSTRAINT CHK_Event_DistanceKm CHECK (DistanceKm > 0),
    CONSTRAINT CHK_Event_EventType  CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES [User](Id)
);
GO

-- ============================================================
-- Table: Category  (belongs to one Event)
-- ============================================================
CREATE TABLE Category (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    EventId     INT NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventId)
        REFERENCES Event(Id) ON DELETE CASCADE
);
GO

-- ============================================================
-- Table: Enrolment  (Participant + Event + Category)
-- A participant can enter a given event only once.
-- ============================================================
CREATE TABLE Enrolment (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId       INT NOT NULL,
    CategoryId    INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Enrolment_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantId)
        REFERENCES [User](Id),
    CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventId)
        REFERENCES Event(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryId)
        REFERENCES Category(Id)
);
GO

-- ============================================================
-- Table: Result  (1:1 with Enrolment)
-- ============================================================
CREATE TABLE Result (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL,
    FinishTime  NVARCHAR(20) NULL,
    Position    INT NULL,
    Points      INT NULL,
    CONSTRAINT UQ_Result_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT CHK_Result_Position CHECK (Position IS NULL OR Position > 0),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentId)
        REFERENCES Enrolment(Id) ON DELETE CASCADE
);
GO

-- ============================================================
-- Indexes for common lookups
-- ============================================================
CREATE INDEX IX_Event_OrganiserId        ON Event(OrganiserId);
CREATE INDEX IX_Category_EventId         ON Category(EventId);
CREATE INDEX IX_Enrolment_EventId_Category ON Enrolment(EventId, CategoryId);
GO

-- ============================================================
-- Sample data
-- (PasswordHash values here are placeholders. Real accounts are created
-- through the API, which stores PBKDF2 hashes.)
-- ============================================================
INSERT INTO [User] (Email, PasswordHash, Role) VALUES
('organiser1@raceday.com',  'placeholder_hash_1', 'Organiser'),
('participant1@raceday.com','placeholder_hash_2', 'Participant'),
('participant2@raceday.com','placeholder_hash_3', 'Participant');
GO

INSERT INTO UserProfile (UserId, FullName, Phone, Bio) VALUES
(1, 'Thabo Mokoena',  '0821234567', 'Race organiser with 5 years experience'),
(2, 'Naledi Dlamini', '0839876543', 'Weekend runner, training for a half marathon'),
(3, 'Sipho Nkosi',    '0847654321', 'Trail running enthusiast');
GO

INSERT INTO Event (OrganiserId, Name, Description, Date, Location, DistanceKm, EventType) VALUES
(1, 'Johannesburg City Run 2026', 'Annual road race through the city centre', '2026-11-15', 'Johannesburg CBD', 21.10, 'Run'),
(1, 'Soweto Family Walk',         'Relaxed community walk',                   '2026-12-05', 'Soweto',           5.00,  'Walk'),
(1, 'Magaliesberg Cycle Classic', 'Scenic road cycling event',                '2027-01-24', 'Magaliesburg',     60.00, 'Cycle');
GO

INSERT INTO Category (EventId, Name, Description) VALUES
(1, '10km',   'Standard road race distance'),
(1, '21km',   'Half marathon, competitive category'),
(1, 'Under 20','Age category for runners under 20'),
(2, 'Family', 'All ages welcome'),
(3, 'Senior', 'Riders 40 and older');
GO

INSERT INTO Enrolment (ParticipantId, EventId, CategoryId) VALUES
(2, 1, 1),
(3, 1, 2),
(2, 2, 4);
GO

INSERT INTO Result (EnrolmentId, FinishTime, Position, Points) VALUES
(1, '00:52:30', 12, 88),
(2, '01:45:10', 5, 95);
GO

-- Quick verification
SELECT * FROM [User];
SELECT * FROM UserProfile;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;
GO
