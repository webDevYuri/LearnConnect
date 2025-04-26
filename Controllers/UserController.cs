using Microsoft.AspNetCore.Mvc;
using LearnConnect.Models;
using LearnConnect.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace LearnConnect.Controllers
{
    public class UserController : Controller
    {
        private readonly LcDbContext _context;
        private readonly IPasswordHasher<UserProfile> _hasher;
        private readonly IWebHostEnvironment _environment;

        public UserController(LcDbContext context, IPasswordHasher<UserProfile> hasher, IWebHostEnvironment environment)
        {
            _context = context;
            _hasher = hasher;
            _environment = environment;
        }

        public IActionResult Dashboard()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return View();
        }

        public IActionResult News()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return View();
        }

        public IActionResult Question()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return View();
        }

        public IActionResult Shop()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return View();
        }

        public IActionResult UpdateProfile()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }

            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (userProfile == null)
            {
                return RedirectToAction("Signin", "Account");
            }

            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfile model, string currentPassword, string newPassword, string confirmNewPassword, IFormFile profilePhoto)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }

            var existingProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (existingProfile == null)
            {
                return RedirectToAction("Signin", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
            {
                var result = _hasher.VerifyHashedPassword(existingProfile, existingProfile.PasswordHash, currentPassword);
                if (result == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View(model);
                }

                if (newPassword != confirmNewPassword)
                {
                    ModelState.AddModelError("", "New passwords do not match.");
                    return View(model);
                }

                existingProfile.PasswordHash = _hasher.HashPassword(existingProfile, newPassword);
            }

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile-photos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(existingProfile.ProfilePhotoPath))
                {
                    var oldPhotoPath = Path.Combine(_environment.WebRootPath, existingProfile.ProfilePhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhotoPath))
                    {
                        System.IO.File.Delete(oldPhotoPath);
                    }
                }

                var uniqueFileName = $"{existingProfile.Id}_{DateTime.Now.Ticks}{Path.GetExtension(profilePhoto.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }

                existingProfile.ProfilePhotoPath = $"/uploads/profile-photos/{uniqueFileName}";
            }

            existingProfile.FirstName = model.FirstName;
            existingProfile.MiddleName = model.MiddleName;
            existingProfile.LastName = model.LastName;
            existingProfile.Phone = model.Phone;
            existingProfile.Birthday = model.Birthday;
            existingProfile.Gender = model.Gender;

            if (model.Email != existingProfile.Email)
            {
                if (_context.UserProfiles.Any(u => u.Email == model.Email && u.Id != existingProfile.Id))
                {
                    ModelState.AddModelError("Email", "This email is already associated with another account.");
                    return View(model);
                }
                existingProfile.Email = model.Email;
                HttpContext.Session.SetString("UserEmail", model.Email);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("UpdateProfile");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Signin", "Account");
        }
    }
}
