using Microsoft.AspNetCore.Mvc;
using LearnConnect.Models;

namespace LearnConnect.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Signin()
        {
            return View();
        }

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(SignupViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError("Email", "Email is required.");
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required.");
            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                ModelState.AddModelError("ConfirmPassword", "Please confirm your password.");

            if(!string.IsNullOrWhiteSpace(model.Password)
                && !string.IsNullOrWhiteSpace(model.ConfirmPassword)
                && model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            return RedirectToAction("Signin");
        }
    }
}
