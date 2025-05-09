using Microsoft.AspNetCore.Mvc;
using LearnConnect.Models;
using LearnConnect.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NewsAPI.Constants;
using NewsAPI.Models;
using NewsAPI;
using static LearnConnect.Controllers.UserController;

namespace LearnConnect.Controllers
{
    public class UserController : Controller
    {
        private readonly LcDbContext _context;
        private readonly IPasswordHasher<UserProfile> _hasher;
        private readonly IWebHostEnvironment _environment;
        private readonly INewsService _newsService;

        public UserController(LcDbContext context, IPasswordHasher<UserProfile> hasher, IWebHostEnvironment environment, INewsService newsService)
        {
            _context = context;
            _hasher = hasher;
            _environment = environment;
            _newsService = newsService;
        }

        private IActionResult RedirectIfNotLoggedIn()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return null;
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Signin", "Account");
        }

        public IActionResult Dashboard()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            var posts = _context.Posts
                .Include(p => p.UserProfile)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.UserProfile)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.UserProfile = userProfile;
            return View(posts);
        }

        public async Task<IActionResult> News()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var articles = await _newsService.GetTechnologyHeadlinesAsync();
            return View(articles.ToList());
        }


        public interface INewsService
        {
            Task<IEnumerable<ArticleViewModel>> GetTechnologyHeadlinesAsync();
        }


        public class NewsService : INewsService
        {
            private readonly NewsApiClient _client;
            private readonly IMemoryCache _cache;
            private const string CacheKey = "TechHeadlines";
            private const string DEFAULT_IMAGE = "https://images.unsplash.com/photo-1649972904349-6e44c42644a7?q=80&w=2070&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDF8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D";

            public NewsService(string apiKey, IMemoryCache cache)
            {
                _client = new NewsApiClient(apiKey);
                _cache = cache;
            }

            public async Task<IEnumerable<ArticleViewModel>> GetTechnologyHeadlinesAsync()
            {
                if (_cache.TryGetValue(CacheKey, out List<ArticleViewModel> cached))
                    return cached;

                var response = await _client.GetTopHeadlinesAsync(new TopHeadlinesRequest
                {
                    Category = Categories.Technology,
                    PageSize = 12
                });

                if (response.Status != Statuses.Ok)
                    return Enumerable.Empty<ArticleViewModel>();

                var articles = response.Articles.Select(a => new ArticleViewModel
                {
                    Title = a.Title,
                    Description = a.Description,
                    Url = a.Url,
                    ImageUrl = GetImageUrl(a),
                    PublishedAt = a.PublishedAt ?? DateTime.UtcNow,
                    Source = a.Source?.Name ?? "Unknown Source"
                }).ToList();

                _cache.Set(CacheKey, articles, TimeSpan.FromMinutes(30));

                return articles;
            }

            private string GetImageUrl(Article article)
            {
                if (!string.IsNullOrWhiteSpace(article.UrlToImage) &&
                    Uri.IsWellFormedUriString(article.UrlToImage, UriKind.Absolute))
                {
                    return article.UrlToImage;
                }

                return DEFAULT_IMAGE;
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshNews()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var cacheField = typeof(NewsService).GetField("CacheKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string cacheKey = cacheField?.GetValue(null) as string ?? "TechHeadlines";

            if (!string.IsNullOrEmpty(cacheKey))
            {
                var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
                cache?.Remove(cacheKey);
            }

            var articles = await _newsService.GetTechnologyHeadlinesAsync();
            return Ok(new { success = true, count = articles.Count() });
        }

        public IActionResult Question()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            var questions = _context.Questions
                .Include(q => q.UserProfile)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.UserProfile)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Upvotes)
                .OrderByDescending(q => q.CreatedAt)
                .ToList();

            // Add a flag to each answer to indicate if the current user has upvoted it
            foreach (var question in questions)
            {
                foreach (var answer in question.Answers)
                {
                    answer.UpvotedByCurrentUser = answer.Upvotes.Any(u => u.UserProfileId == userProfile.Id);
                }
            }

            return View(questions);
        }

        public IActionResult Shop()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            return View();
        }

        public IActionResult UpdateProfile()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (userProfile == null)
            {
                return RedirectToAction("Signin", "Account");
            }

            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfile model, string? currentPassword = null, string? newPassword = null, string? confirmNewPassword = null, IFormFile? profilePhoto = null)
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

            if (!string.IsNullOrEmpty(currentPassword) || !string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmNewPassword))
            {
                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
                {
                    ModelState.AddModelError("", "All password fields are required to change the password.");
                    return View(model);
                }

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
                catch (Exception)
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
            catch (Exception)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string content, IFormFile mediaFile)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            if (userProfile == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrWhiteSpace(content) && mediaFile == null)
            {
                TempData["Error"] = "Post must contain either text or media.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                var post = new Post
                {
                    Content = content ?? "",
                    UserProfileId = userProfile.Id,
                    CreatedAt = DateTime.Now
                };

                if (mediaFile != null && mediaFile.Length > 0)
                {
                    if (mediaFile.Length > 50 * 1024 * 1024)
                    {
                        TempData["Error"] = "Media file must be less than 10MB.";
                        return RedirectToAction("Dashboard");
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                    var fileExtension = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Only JPG, JPEG, PNG, GIF, MP4, and MOV files are allowed.";
                        return RedirectToAction("Dashboard");
                    }

                    try
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{userProfile.Id}_{DateTime.Now.Ticks}{fileExtension}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await mediaFile.CopyToAsync(stream);
                        }

                        bool isVideo = fileExtension == ".mp4" || fileExtension == ".mov";
                        post.MediaPath = $"/uploads/posts/{uniqueFileName}";
                        post.MediaType = isVideo ? "video" : "image";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Error uploading media: {ex.Message}";
                        return RedirectToAction("Dashboard");
                    }
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Post created successfully.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating post: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            if (userProfile == null)
            {
                TempData["Error"] = "Session expired. Please sign in again.";
                return RedirectToAction("Dashboard");
            }

            var post = _context.Posts.FirstOrDefault(p => p.Id == postId && p.UserProfileId == userProfile.Id);

            if (post == null)
            {
                TempData["Error"] = "You are not authorized to delete this post.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Post deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the post. Please try again.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            if (userProfile == null)
            {
                TempData["Error"] = "Session expired. Please sign in again.";
                return RedirectToAction("Dashboard");
            }

            var post = _context.Posts.Include(p => p.Likes).FirstOrDefault(p => p.Id == postId);
            if (post == null)
            {
                TempData["Error"] = "Post not found.";
                return RedirectToAction("Dashboard");
            }

            var existingLike = post.Likes.FirstOrDefault(l => l.UserProfileId == userProfile.Id);

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
                TempData["Success"] = "You unliked the post.";
            }
            else
            {
                var newLike = new PostLike
                {
                    PostId = postId,
                    UserProfileId = userProfile.Id,
                    CreatedAt = DateTime.Now
                };
                _context.PostLikes.Add(newLike);
                TempData["Success"] = "You liked the post.";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while updating the like status.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);

            if (userProfile == null)
            {
                TempData["Error"] = "Session expired. Please sign in again.";
                return RedirectToAction("Dashboard", "User");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Comment content cannot be empty.";
                return RedirectToAction("Dashboard", "User");
            }

            var post = _context.Posts.FirstOrDefault(p => p.Id == postId);
            if (post == null)
            {
                TempData["Error"] = "Post not found.";
                return RedirectToAction("Dashboard", "User");
            }

            var comment = new PostComment
            {
                Content = content,
                PostId = postId,
                UserProfileId = userProfile.Id,
                CreatedAt = DateTime.Now
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment added successfully.";
            return RedirectToAction("Dashboard", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskQuestion(string title, string details, string? tags)
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

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(details))
            {
                TempData["Error"] = "Title and details are required.";
                return RedirectToAction("Question");
            }

            var question = new Question
            {
                Title = title,
                Details = details,
                Tags = tags,
                UserProfileId = userProfile.Id,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your question has been posted successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while posting your question. Please try again.";
            }

            return RedirectToAction("Question");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnswer(int questionId, string content)
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

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Answer content cannot be empty.";
                return RedirectToAction("Question");
            }

            var answer = new Answer
            {
                Content = content,
                QuestionId = questionId,
                UserProfileId = userProfile.Id,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Answers.Add(answer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your answer has been posted successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while posting your answer. Please try again.";
            }

            return RedirectToAction("Question");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUpvoteAnswer([FromBody] AnswerUpvote request)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var userProfile = _context.UserProfiles.FirstOrDefault(u => u.Email == userEmail);
            if (userProfile == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var answer = _context.Answers.Include(a => a.Upvotes).FirstOrDefault(a => a.Id == request.AnswerId);
            if (answer == null)
            {
                return Json(new { success = false, message = "Answer not found." });
            }

            var existingUpvote = answer.Upvotes.FirstOrDefault(u => u.UserProfileId == userProfile.Id);

            if (existingUpvote != null)
            {
                // Remove the upvote
                _context.AnswerUpvotes.Remove(existingUpvote);
            }
            else
            {
                // Add the upvote
                _context.AnswerUpvotes.Add(new AnswerUpvote
                {
                    AnswerId = request.AnswerId,
                    UserProfileId = userProfile.Id
                });
            }

            _context.SaveChanges();

            // Return the updated upvote count and whether the user has upvoted
            var isUpvoted = existingUpvote == null; // If no existing upvote, the user just upvoted
            var upvoteCount = answer.Upvotes.Count;

            return Json(new { success = true, isUpvoted, upvotes = upvoteCount });
        }
    }
}