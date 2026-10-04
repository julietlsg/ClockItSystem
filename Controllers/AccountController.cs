using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        [HttpPost]
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
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
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
        public IActionResult Register()
        {
            ViewBag.Roles = GetRoles();

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            ViewBag.Roles = GetRoles();

            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Selected role does not exist.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                //UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

                TempData["Success"] =
                    "User registered successfully.";

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
        // LOGOUT
        // ============================================================

        [HttpPost]
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