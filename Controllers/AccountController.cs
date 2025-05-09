﻿using Microsoft.AspNetCore.Mvc;
using LearnConnect.Models;
using LearnConnect.Data;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace LearnConnect.Controllers
{
    public class AccountController : Controller
    {
        private readonly LcDbContext _context;
        private readonly IPasswordHasher<UserProfile> _hasher;
        private readonly IWebHostEnvironment _environment;

        public AccountController(LcDbContext context, IPasswordHasher<UserProfile> hasher, IWebHostEnvironment environment)
        {
            _context = context;
            _hasher = hasher;
            _environment = environment;
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

            var user = _context.UserProfiles.FirstOrDefault(u => u.Email == email);
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
        public async Task<IActionResult> Signup(UserProfile model, string confirmPassword)
        {
            var phonePattern = new Regex(@"^09\d{9}$");
            if (string.IsNullOrWhiteSpace(model.Phone) || !phonePattern.IsMatch(model.Phone))
            {
                ModelState.AddModelError("Phone", "Phone must start with 09 and be exactly 11 digits.");
            }

            var passPattern = new Regex(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$");
            if (string.IsNullOrWhiteSpace(model.PasswordHash) || !passPattern.IsMatch(model.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash",
                    "Password must be at least 8 characters and include both letters and numbers.");
            }

            if (model.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError("",
                    "Passwords do not match.");
            }

            if (model.Birthday.AddYears(18) > DateTime.Today)
            {
                ModelState.AddModelError("Birthday",
                    "You must be at least 18 years old.");
            }

            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.FirstName) ||
                string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError("",
                    "Please complete all required information.");
            }

            if (_context.UserProfiles.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email",
                    "This email is already associated with another account.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var userProfile = new UserProfile
            {
                Email = model.Email,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Phone = model.Phone,
                Birthday = model.Birthday,
                Gender = model.Gender
            };

            userProfile.PasswordHash = _hasher.HashPassword(userProfile, model.PasswordHash);

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile-photos");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{DateTime.Now.Ticks}{Path.GetExtension(model.ProfilePhoto.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePhoto.CopyToAsync(stream);
                }

                userProfile.ProfilePhotoPath = $"/uploads/profile-photos/{uniqueFileName}";
            }

            try
            {
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Account created successfully. Please sign in to continue.";
                return RedirectToAction("Signin");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                return View(model);
            }
        }
    }
}
