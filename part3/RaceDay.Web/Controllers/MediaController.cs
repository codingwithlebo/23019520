using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Serves images to the browser. The bytes come from the API, which reads them from Azure Blob
/// Storage, so the MVC app never talks to Azure itself.
/// </summary>
public class MediaController(ApiClient api) : AppController
{
    [HttpGet]
    public async Task<IActionResult> EventBanner(int id)
    {
        var result = await api.GetFileAsync($"/api/events/{id}/banner");
        return result.Success ? File(result.Data!.Bytes, result.Data.ContentType) : NotFound();
    }

    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> ProfilePicture(int id)
    {
        var result = await api.GetFileAsync($"/api/users/{id}/profile/picture");
        return result.Success ? File(result.Data!.Bytes, result.Data.ContentType) : NotFound();
    }
}
