-- ============================================
-- RaceDay System - Database Schema
-- Matches ERD in /docs/raceday_erd.png
-- ============================================

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ============================================
-- Table: User
-- ============================================
CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- ============================================
-- Table: UserProfile
-- ============================================
CREATE TABLE UserProfile (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Bio NVARCHAR(500) NULL,
    CONSTRAINT FK_UserProfile_User FOREIGN KEY (UserId) REFERENCES [User](Id)
);
GO

-- ============================================
-- Table: Event
-- ============================================
CREATE TABLE Event (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Date DATETIME NOT NULL,
    Location NVARCHAR(255) NOT NULL,
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserId) REFERENCES [User](Id)
);
GO

-- ============================================
-- Table: Category
-- ============================================
CREATE TABLE Category (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventId) REFERENCES Event(Id)
);
GO

-- ============================================
-- Table: Enrolment
-- ============================================
CREATE TABLE Enrolment (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantId) REFERENCES [User](Id),
    CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventId) REFERENCES Event(Id),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryId) REFERENCES Category(Id)
);
GO

-- ============================================
-- Table: Result
-- ============================================
CREATE TABLE Result (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime NVARCHAR(20) NULL,
    Position INT NULL,
    Points INT NULL,
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolment(Id)
);
GO

-- ============================================
-- Sample data
-- ============================================

INSERT INTO [User] (Email, PasswordHash, Role) VALUES
('organiser1@raceday.com', 'hashed_password_1', 'Organiser'),
('participant1@raceday.com', 'hashed_password_2', 'Participant'),
('participant2@raceday.com', 'hashed_password_3', 'Participant');
GO

INSERT INTO UserProfile (UserId, FullName, Phone, Bio) VALUES
(1, 'Thabo Mokoena', '0821234567', 'Race organiser with 5 years experience'),
(2, 'Naledi Dlamini', '0839876543', 'Weekend runner, training for a half marathon'),
(3, 'Sipho Nkosi', '0847654321', 'Trail running enthusiast');
GO

INSERT INTO Event (OrganiserId, Name, Description, Date, Location) VALUES
(1, 'Johannesburg City Run 2026', 'Annual road race through the city centre', '2026-11-15', 'Johannesburg CBD');
GO

INSERT INTO Category (EventId, Name, Description) VALUES
(1, '5km Fun Run', 'Beginner-friendly short distance'),
(1, '10km Road Race', 'Standard road race distance'),
(1, 'Half Marathon', '21.1km competitive category');
GO

INSERT INTO Enrolment (ParticipantId, EventId, CategoryId) VALUES
(2, 1, 2),
(3, 1, 3);
GO

INSERT INTO Result (EnrolmentId, FinishTime, Position, Points) VALUES
(1, '00:52:30', 12, 88),
(2, '01:45:10', 5, 95);
GO
