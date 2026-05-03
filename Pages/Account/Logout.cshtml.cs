using IISWeb.Models;
using IISWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IISWeb.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly IAuditService _audit;
    public LogoutModel(IAuditService audit) => _audit = audit;

    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await _audit.LogAsync(AuditActions.Logout, null, true);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}
