using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;
    private readonly IAdminRoleService _roleService;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(
        IAdminUserService userService,
        IAdminRoleService roleService,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _roleService = roleService;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserListItem>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDetail>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var roles = await _userService.GetUserRolesAsync(user);
        return Ok(new AdminUserDetail(
            user.Id,
            user.Email,
            user.UserName,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            user.IsActive,
            roles));
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var roles = await _userService.GetUserRolesAsync(user);
        return Ok(roles);
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDetail>> CreateUser(
        [FromBody] AdminUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateEmail(request.Email);
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var userForValidation = new ApplicationUser
            {
                Email = request.Email?.Trim(),
                UserName = request.UserName?.Trim() ?? request.Email?.Trim()
            };
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, userForValidation, request.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        validation.AddFieldError("password", error.Description);
                    }
                }
            }
        }

        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid user data."));
        }

        try
        {
            var user = await _userService.CreateUserAsync(request, cancellationToken);
            var roles = await _userService.GetUserRolesAsync(user);
            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new AdminUserDetail(
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.PhoneNumber,
                    user.EmailConfirmed,
                    user.TwoFactorEnabled,
                    user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    user.IsActive,
                    roles));
        }
        catch (InvalidOperationException ex)
        {
            validation.AddGeneralError(ex.Message);
            return ValidationProblem(validation.ToProblemDetails("User creation failed."));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] AdminUserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateEmail(request.Email);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid user data."));
        }

        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var result = await _userService.UpdateUserAsync(user, request, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                validation.AddGeneralError(error.Description);
            }
            return ValidationProblem(validation.ToProblemDetails("Failed to update user."));
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var result = await _userService.DeactivateUserAsync(user, cancellationToken);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Failed to deactivate user.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(
        Guid id,
        [FromBody] AdminUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var validation = new AdminValidationResult();
        var desiredRoles = request.Roles?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        var existingRoles = await _roleService.GetRolesAsync(cancellationToken);
        var existingRoleNames = existingRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalidRoles = desiredRoles.Where(role => !existingRoleNames.Contains(role)).ToList();
        if (invalidRoles.Count > 0)
        {
            validation.AddFieldError("roles", $"Unknown roles: {string.Join(", ", invalidRoles)}.");
        }

        var actorId = GetActorUserId();
        if (actorId.HasValue && actorId.Value == user.Id)
        {
            var targetWillBeAdmin = await WillUserBeAdminAsync(user.Id, desiredRoles, cancellationToken);
            var isOnlyAdmin = await IsLastAdminAsync(user.Id, cancellationToken);
            if (!targetWillBeAdmin && isOnlyAdmin)
            {
                validation.AddGeneralError("You are the last admin. Assign another admin before removing admin access.");
            }
        }
        else
        {
            var targetWillBeAdmin = await WillUserBeAdminAsync(user.Id, desiredRoles, cancellationToken);
            var isOnlyAdmin = await IsLastAdminAsync(user.Id, cancellationToken);
            if (!targetWillBeAdmin && isOnlyAdmin)
            {
                validation.AddGeneralError("Cannot remove admin access from the last admin user.");
            }
        }

        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid role assignment."));
        }

        var result = await _userService.SetUserRolesAsync(user, desiredRoles);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                validation.AddGeneralError(error.Description);
            }
            return ValidationProblem(validation.ToProblemDetails("Failed to update user roles."));
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> SetLock(
        Guid id,
        [FromBody] AdminUserLockRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var actorId = GetActorUserId();
        if (request.Locked && actorId.HasValue && actorId.Value == user.Id)
        {
            var validation = new AdminValidationResult();
            validation.AddGeneralError("You cannot lock your own account.");
            return ValidationProblem(validation.ToProblemDetails("Invalid lock request."));
        }

        await _userService.SetLockoutAsync(user, request.Locked, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] AdminUserResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var validation = new AdminValidationResult();
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            validation.AddFieldError("newPassword", "New password is required.");
            return ValidationProblem(validation.ToProblemDetails("Invalid password."));
        }

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, request.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    validation.AddFieldError("newPassword", error.Description);
                }
            }
        }

        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid password."));
        }

        var result = await _userService.ResetPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                validation.AddGeneralError(error.Description);
            }
            return ValidationProblem(validation.ToProblemDetails("Failed to reset password."));
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/resend-activation")]
    public async Task<IActionResult> ResendActivation(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var validation = new AdminValidationResult();
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            validation.AddFieldError("email", "User must have an email to resend activation.");
            return ValidationProblem(validation.ToProblemDetails("Activation is not available."));
        }

        if (user.EmailConfirmed)
        {
            validation.AddGeneralError("User email is already confirmed.");
            return ValidationProblem(validation.ToProblemDetails("Activation is not available."));
        }

        if (!user.IsActive)
        {
            validation.AddGeneralError("User is deactivated. Reactivate the account before resending activation.");
            return ValidationProblem(validation.ToProblemDetails("Activation is not available."));
        }

        var result = await _userService.ResendActivationAsync(user, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                validation.AddGeneralError(error.Description);
            }
            return ValidationProblem(validation.ToProblemDetails("Activation email could not be sent."));
        }

        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<AdminRoleListItem>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    private Guid? GetActorUserId()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<bool> IsLastAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var adminRoleIds = await _dbContext.RolePermissions
            .Where(item => item.Permission.Key == "system.admin")
            .Select(item => item.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (adminRoleIds.Count == 0)
        {
            return false;
        }

        var adminUserIds = await _dbContext.UserRoles
            .Where(item => adminRoleIds.Contains(item.RoleId))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return adminUserIds.Count == 1 && adminUserIds.Contains(userId);
    }

    private async Task<bool> WillUserBeAdminAsync(Guid userId, IReadOnlyCollection<string> desiredRoles, CancellationToken cancellationToken)
    {
        if (desiredRoles.Count == 0)
        {
            return false;
        }

        var adminRoleNames = await _dbContext.RolePermissions
            .Where(item => item.Permission.Key == "system.admin")
            .Select(item => item.Role.Name)
            .Where(name => name != null)
            .Distinct()
            .ToListAsync(cancellationToken);

        return desiredRoles.Any(role => adminRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
