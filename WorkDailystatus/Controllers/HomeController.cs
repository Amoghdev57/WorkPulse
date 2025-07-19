using Microsoft.AspNetCore.Mvc;
using WorkDailystatus.Models;

namespace WorkDailystatus.Controllers
{
    public class HomeController : Controller
    {
        private const string ValidUsername = "Amogh";
        private const string ValidPassword = "password123";

        private static List<StatusEntry> _statusEntries = new List<StatusEntry>();

        // GET: Login page
        public IActionResult Login()
        {
            return View();
        }

        // POST: Handle login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == ValidUsername && password == ValidPassword)
            {
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("username", username);
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View();
            }
        }

        // GET: Status page (only for authenticated users)
        public IActionResult Status(int? editSerial)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
            {
                ViewBag.Username = HttpContext.Session.GetString("username") ?? "Guest";
                ViewBag.EditSerialNumber = editSerial;
                return View(_statusEntries);
            }
            return RedirectToAction("Login");
        }

        // POST: Add a new status entry
        [HttpPost]
        public IActionResult AddStatus(string userStatus)
        {
            int nextSerialNumber = _statusEntries.Any() ? _statusEntries.Max(e => e.SerialNumber) + 1 : 1;

            _statusEntries.Add(new StatusEntry
            {
                SerialNumber = nextSerialNumber,
                UserStatus = userStatus,
                Date = DateTime.Now
            });
            return RedirectToAction("Status");
        }

        // POST: Delete a status entry
        [HttpPost]
        public IActionResult Delete(int serialNumber)
        {
            var entry = _statusEntries.FirstOrDefault(e => e.SerialNumber == serialNumber);
            if (entry != null)
            {
                _statusEntries.Remove(entry);

                // Reassign serial numbers to maintain sequence
                int currentSerial = 1;
                foreach (var item in _statusEntries.OrderBy(e => e.SerialNumber))
                {
                    item.SerialNumber = currentSerial++;
                }
            }
            return RedirectToAction("Status");
        }

        // ? POST: Handle inline edit
        [HttpPost]
        public IActionResult EditInline(StatusEntry updatedEntry)
        {
            var entry = _statusEntries.FirstOrDefault(e => e.SerialNumber == updatedEntry.SerialNumber);
            if (entry != null)
            {
                entry.UserStatus = updatedEntry.UserStatus;
                entry.Date = DateTime.Now; // Update timestamp
            }
            return RedirectToAction("Status");
        }

        // POST: Handle logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
