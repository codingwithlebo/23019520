namespace RaceDay.Api.Models;

// Named AppUser (not User) so it does not clash with ControllerBase.User.
// It still maps to the [User] table from the ERD.
public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Participant;

    public UserProfile? Profile { get; set; }
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Bio { get; set; }

    public AppUser? User { get; set; }
}

public class Event
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = Models.EventTypes.Run;

    public AppUser? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Category
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    public AppUser? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}

public class Result
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public int? Points { get; set; }

    public Enrolment? Enrolment { get; set; }
}
