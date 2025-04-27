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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Signin", "Account");
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

            var minimumAge = 13;
            var minimumBirthday = DateTime.Today.AddYears(-minimumAge);
            if (model.Birthday > minimumBirthday)
            {
                ModelState.AddModelError("Birthday", $"You must be at least {minimumAge} years old to use this service.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Phone) && !System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^\+?[0-9]{10,15}$"))
            {
                ModelState.AddModelError("Phone", "Please enter a valid phone number.");
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

                if (newPassword.Length < 8)
                {
                    ModelState.AddModelError("", "New password must be at least 8 characters long.");
                    return View(model);
                }

                existingProfile.PasswordHash = _hasher.HashPassword(existingProfile, newPassword);
            }

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                if (profilePhoto.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Profile photo must be less than 5MB.");
                    return View(model);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Only JPG, JPEG, PNG, and GIF files are allowed.");
                    return View(model);
                }

                try
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

                    var uniqueFileName = $"{existingProfile.Id}_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePhoto.CopyToAsync(stream);
                    }

                    existingProfile.ProfilePhotoPath = $"/uploads/profile-photos/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while uploading the profile photo. Please try again.");
                    return View(model);
                }
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

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction("UpdateProfile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving your profile. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                return Json(new { success = false, message = "Current password is required." });
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "New password is required." });
            }

            if (string.IsNullOrEmpty(confirmNewPassword))
            {
                return Json(new { success = false, message = "Please confirm your new password." });
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "Session expired. Please sign in again." });
            }

            var user = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found. Please sign in again." });
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            if (newPassword != confirmNewPassword)
            {
                return Json(new { success = false, message = "New passwords do not match." });
            }

            if (newPassword.Length < 8)
            {
                return Json(new { success = false, message = "New password must be at least 8 characters long." });
            }

            var newPasswordResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, newPassword);
            if (newPasswordResult == PasswordVerificationResult.Success)
            {
                return Json(new { success = false, message = "New password cannot be the same as your current password." });
            }

            try
            {
                user.PasswordHash = _hasher.HashPassword(user, newPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while changing your password. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePhoto(IFormFile profilePhoto)
        {
            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                return Json(new { success = false, message = "Please select a photo to upload." });
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "Session expired. Please sign in again." });
            }

            var user = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found. Please sign in again." });
            }

            if (profilePhoto.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be less than 5MB." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Only JPG, JPEG, PNG, and GIF files are allowed." });
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile-photos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
                {
                    var oldPhotoPath = Path.Combine(_environment.WebRootPath, user.ProfilePhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhotoPath))
                    {
                        System.IO.File.Delete(oldPhotoPath);
                    }
                }

                var uniqueFileName = $"{user.Id}_{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }

                user.ProfilePhotoPath = $"/uploads/profile-photos/{uniqueFileName}";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile photo updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while uploading the photo. Please try again." });
            }
        }

    }
}