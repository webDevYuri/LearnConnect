using Microsoft.AspNetCore.Mvc;

namespace LearnConnect.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Dashboard()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Signin", "Account");
            }
            return View();
        }
    }
}
