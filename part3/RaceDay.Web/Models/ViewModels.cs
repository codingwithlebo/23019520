using System.ComponentModel.DataAnnotations;
using System.Globalization;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

// ---------- Account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your full name."), StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser invite code")]
    public string? OrganiserCode { get; set; }
}

// ---------- Events ----------

public class EventFilter
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public decimal? MinDistance { get; set; }
    public decimal? MaxDistance { get; set; }
    public bool IncludePast { get; set; }

    public string ToQueryString()
    {
        var parts = new List<string>();
        if (!IncludePast) parts.Add("upcomingOnly=true");
        if (!string.IsNullOrWhiteSpace(Search)) parts.Add($"search={Uri.EscapeDataString(Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(Type)) parts.Add($"type={Uri.EscapeDataString(Type)}");
        if (MinDistance.HasValue) parts.Add($"minDistanceKm={MinDistance.Value.ToString(CultureInfo.InvariantCulture)}");
        if (MaxDistance.HasValue) parts.Add($"maxDistanceKm={MaxDistance.Value.ToString(CultureInfo.InvariantCulture)}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}

public class HomeViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}

// ---------- Organiser ----------

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.Date >= DateTime.Now).OrderBy(e => e.Date).ToList();
}

public class EventFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the event name."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter the location."), StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "Enter a distance between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public bool HasBanner { get; set; }
}

public class CategoryFormViewModel
{
    [Required(ErrorMessage = "Enter a category name (for example Under 20, Senior, 10km)."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class CategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
}

// ---------- Profile ----------

public class ProfileViewModel
{
    public ProfileDto Profile { get; set; } = new();

    [Required(ErrorMessage = "Enter your full name."), StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }
}

// ---------- Errors ----------

public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
