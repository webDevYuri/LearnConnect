using Microsoft.AspNetCore.Mvc;
using LearnConnect.Models;
using LearnConnect.Data;
using Microsoft.AspNetCore.Identity;

namespace LearnConnect.Controllers
{
    public class AccountController : Controller
    {
        private readonly LcDbContext _context;
        private readonly IPasswordHasher<User> _hasher;

        public AccountController(LcDbContext context, IPasswordHasher<User> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public IActionResult Signin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return RedirectToAction("Signin");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Signin");
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                TempData["Error"] = "Incorrect password.";
                return RedirectToAction("Signin");
            }
            HttpContext.Session.SetString("UserEmail", user.Email);
            TempData["Success"] = "Login successfully.";
            return RedirectToAction("Dashboard", "User");
        }


        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(SignupViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrEmpty(model.Password))

            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
            }

            if (model.Password != model.ConfirmPassword)
                ModelState.AddModelError(string.Empty, "Passwords dot not match.");

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError(string.Empty, "This email is already associated with another account.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            };

            var user = new User
            {
                Email = model.Email
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Account created successfully.";
            return RedirectToAction("Signin");
        }
    }
}
