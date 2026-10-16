using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// ---------- Authentication ----------

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>Organiser or Participant.</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>Invite code. Only required when registering as an Organiser.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

// ---------- Profile ----------

public class UpdateProfileRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }
}

public class ProfileResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Bio { get; set; }
}

// ---------- Events ----------

public class EventRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime? Date { get; set; }

    [Required, MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Distance of the event in kilometres.</summary>
    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class EventResponse
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
}

// ---------- Categories ----------

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class CategoryResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ---------- Enrolments ----------

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
}

// ---------- Results ----------

public class ResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time, e.g. 00:52:30.</summary>
    [Required, MaxLength(20)]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Position { get; set; }

    [Range(0, int.MaxValue)]
    public int? Points { get; set; }
}

public class ResultUpdateRequest
{
    [Required, MaxLength(20)]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Position { get; set; }

    [Range(0, int.MaxValue)]
    public int? Points { get; set; }
}

public class ResultResponse
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public int? Points { get; set; }
}
