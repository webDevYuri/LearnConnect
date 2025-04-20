using Microsoft.AspNetCore.Mvc;

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
    }
}
