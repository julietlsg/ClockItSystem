using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return View(model);
        }

        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            /*
             * Do not reveal whether an email address exists.
             * This prevents account/email enumeration.
             */
            if (user == null || !user.EmailConfirmed)
            {
                return View("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme);

            /*
             * TEMPORARY DEVELOPMENT ONLY
             *
             * We don't have SMTP configured yet.
             * This allows us to test the complete reset workflow.
             *
             * REMOVE THIS when Stage 2 email is implemented.
             */
            TempData["DevelopmentResetLink"] = resetUrl;

            return View("ForgotPasswordConfirmation");
        }

        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(
            string? userId,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to reset the password.");

                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.Password);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Your password has been reset successfully. You can now log in.";

                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // ============================================================
        // REGISTER
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel
            {
                Clients = await GetActiveClientsAsync()
            };

            ViewBag.Roles = GetRoles();

            return View(model);
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Roles = GetRoles();

            // Remove Client validation for Admin users.
            if (model.Role == "Admin")
            {
                model.ClientId = null;
                ModelState.Remove(nameof(model.ClientId));
            }
            else if (!model.ClientId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Please select a client.");
            }

            if (!ModelState.IsValid)
            {
                model.Clients = await GetActiveClientsAsync();
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Selected role does not exist.");

                model.Clients = await GetActiveClientsAsync();

                return View(model);
            }

            // Validate selected client for non-admin users.
            Client? client = null;

            if (model.Role != "Admin")
            {
                client = await _context.Clients
                    .FirstOrDefaultAsync(x =>
                        x.ClientId == model.ClientId!.Value &&
                        x.IsActive);

                if (client == null)
                {
                    ModelState.AddModelError(
                        nameof(model.ClientId),
                        "Selected client does not exist or is inactive.");

                    model.Clients = await GetActiveClientsAsync();

                    return View(model);
                }
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.Clients = await GetActiveClientsAsync();

                return View(model);
            }

            // Assign the selected role.
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                model.Role);

            if (!roleResult.Succeeded)
            {
                // Clean up the newly created user if role assignment fails.
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.Clients = await GetActiveClientsAsync();

                return View(model);
            }

            // Admin users have access to all clients,
            // so they do not need a UserClient record.
            if (model.Role != "Admin" && model.ClientId.HasValue)
            {
                var userClient = new UserClient
                {
                    UserId = user.Id,
                    ClientId = model.ClientId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.UserClients.Add(userClient);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Clean up the Identity user if the client assignment fails.
                    await _userManager.DeleteAsync(user);

                    ModelState.AddModelError(
                        string.Empty,
                        "The user could not be assigned to the selected client.");

                    model.Clients = await GetActiveClientsAsync();

                    return View(model);
                }
            }

            TempData["Success"] =
                "User registered and client access assigned successfully.";

            return RedirectToAction(nameof(Login));
        }

        private async Task<List<SelectListItem>> GetActiveClientsAsync()
        {
            return await _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.ClientId.ToString(),
                    Text = x.Name
                })
                .ToListAsync();
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // ROLES
        // ============================================================

        private List<SelectListItem> GetRoles()
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Admin",
                    Value = "Admin"
                },

                new SelectListItem
                {
                    Text = "Facilitator",
                    Value = "Facilitator"
                },

                new SelectListItem
                {
                    Text = "Project Manager",
                    Value = "Project Manager"
                }
            };
        }
    }
}