using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using WorkPulse.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;


namespace WorkPulse.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public HomeController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _env = env;
        }

        // ── TEMPORARY HASH UTILITIES ── delete both after use ──────────────
        public IActionResult GenHash()
        {
            string hash = BCrypt.Net.BCrypt.HashPassword("P@ssword123", workFactor: 12);
            return Content($@"
                <html><body style='font-family:monospace;padding:2rem;'>
                <h3>Generated Hash for P@ssword123</h3>
                <p style='word-break:break-all;background:#f4f4f4;padding:1rem;border-radius:8px;'>{hash}</p>
                <p style='color:red;'><b>Copy this hash, paste it into your SQL UPDATE, then delete this action.</b></p>
                </body></html>
            ", "text/html");
        }

        public IActionResult HashUtil()
        {
            var results = new Dictionary<string, string>
            {
                { "Amogh", BCrypt.Net.BCrypt.HashPassword("P@ssword123", workFactor: 12) }
            };
            string output = string.Join("<br/><br/>", results.Select(r =>
                $"<b>{r.Key}</b>:<br/><span style='word-break:break-all'>{r.Value}</span>"));
            return Content($@"
                <html><body style='font-family:monospace;padding:2rem;'>
                <h3>BCrypt Hash Utility</h3>{output}
                <br/><br/><p style='color:red;'><b>Delete this action after use.</b></p>
                </body></html>
            ", "text/html");
        }
        // ── END TEMPORARY ───────────────────────────────────────────────────

        // ── LOGIN ────────────────────────────────────────────────────────────
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
                return RedirectToAction("Index");
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT id, password FROM logindetails WHERE username=@username";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = Convert.ToInt32(reader["id"]);
                        string storedHash = reader["password"].ToString()!;
                        bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

                        if (isValid)
                        {
                            HttpContext.Session.Clear();
                            HttpContext.Session.SetString("IsAuthenticated", "true");
                            HttpContext.Session.SetString("username", username);
                            HttpContext.Session.SetInt32("UserId", userId);
                            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                            Response.Headers["Pragma"] = "no-cache";
                            Response.Headers["Expires"] = "0";
                            return RedirectToAction("Index");
                        }
                    }
                }

                ViewBag.ErrorMessage = "Invalid username or password.";
                return View();
            }
        }

        // ── FORGOT PASSWORD ───────────────────────────────────────────────────
        [HttpPost]
        public IActionResult CheckUsername([FromBody] CheckUsernameRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username))
                return Json(new { exists = false });

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM logindetails WHERE username=@username";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", req.Username);
                bool exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                return Json(new { exists });
            }
        }

        [HttpPost]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Json(new { success = false, message = "Invalid request." });

            string pw = req.NewPassword;
            if (pw.Length < 8 || !pw.Any(char.IsUpper) || !pw.Any(char.IsLower) ||
                !pw.Any(char.IsDigit) || !pw.Any(c => "!@#$%^&*()_+-=[]{};\':\"\\|,.<>/?`~".Contains(c)))
                return Json(new { success = false, message = "Password does not meet requirements." });

            string hashed = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE logindetails SET password=@password WHERE username=@username";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@password", hashed);
                cmd.Parameters.AddWithValue("@username", req.Username);
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    return Json(new { success = false, message = "Username not found." });
            }
            return Json(new { success = true });
        }

        // ── REGISTER ──────────────────────────────────────────────────────────
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            { ViewBag.Error = "Username is required."; return View(); }

            if (password != confirmPassword)
            { ViewBag.Error = "Passwords do not match."; return View(); }

            if (password.Length < 8)
            { ViewBag.Error = "Password must be at least 8 characters."; return View(); }

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
            { ViewBag.Error = "Password must contain at least 1 uppercase and 1 lowercase letter."; return View(); }

            if (!password.Any(char.IsDigit))
            { ViewBag.Error = "Password must contain at least 1 number."; return View(); }

            string specialChars = "!@#$%^&*()_+-=[]{};\':\"\\|,.<>/?`~";
            if (!password.Any(c => specialChars.Contains(c)))
            { ViewBag.Error = "Password must contain at least 1 special character (!@#$%^&* etc)."; return View(); }

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(*) FROM logindetails WHERE username=@username";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@username", username);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                { ViewBag.Error = "Username already exists. Please choose another."; return View(); }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                string insertQuery = "INSERT INTO logindetails (username, password) VALUES (@username, @password)";
                MySqlCommand cmd = new MySqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                cmd.ExecuteNonQuery();
            }

            ViewBag.Message = "Registration successful! Please login.";
            return View();
        }

        // ── INDEX ─────────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Login");
            return View();
        }

        public IActionResult Holidays()
        {
            return View();
        }
        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult Reimbursements()
        {
            return View();
        }

        public IActionResult Announcements()
        {
            return View();
        }

        // ── PROFILE IMAGE SERVING ─────────────────────────────────────────────
        /// <summary>
        /// Serves the BLOB image for the logged-in user directly as an HTTP image response.
        /// Use in Razor views as: src="@Url.Action("ProfileImage")"
        /// </summary>
        public IActionResult ProfileImage()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return NotFound();

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT profile_image, profile_image_type FROM employee_details WHERE user_id=@userId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && reader["profile_image"] != DBNull.Value)
                    {
                        byte[] imageBytes = (byte[])reader["profile_image"];
                        string mimeType = reader["profile_image_type"] != DBNull.Value
                            ? reader["profile_image_type"].ToString()!
                            : "image/jpeg";
                        return File(imageBytes, mimeType);
                    }
                }
            }
            return NotFound();
        }

        // ── EMPLOYEE PROFILE ──────────────────────────────────────────────────
        public IActionResult EmployeeProfile()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            EmployeeDetails emp = new EmployeeDetails();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Only check existence of blob — don't pull the bytes here
                string query = @"SELECT name, employee_code, department, designation, email, company,
                                        (profile_image IS NOT NULL) AS has_image
                                 FROM employee_details WHERE user_id=@userId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        emp.Name = reader["name"].ToString()!;
                        emp.EmployeeCode = reader["employee_code"].ToString()!;
                        emp.Department = reader["department"].ToString()!;
                        emp.Designation = reader["designation"].ToString()!;
                        emp.Email = reader["email"].ToString()!;
                        emp.Company = reader["company"] != DBNull.Value ? reader["company"].ToString()! : "";

                        // ProfileImagePath is set to the image-serving URL when a blob exists,
                        // or null when there is no photo — the Razor view uses this as <img src="...">
                        bool hasImage = Convert.ToInt32(reader["has_image"]) == 1;
                        emp.ProfileImagePath = hasImage ? Url.Action("ProfileImage") : null;
                    }
                }
            }
            return View(emp);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile(EmployeeDetails model, IFormFile? profileImage)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            if (!ModelState.IsValid) return View("EmployeeProfile", model);
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            byte[]? imageBytes = null;
            string? imageMime = null;
            if (profileImage != null && profileImage.Length > 0)
                (imageBytes, imageMime) = await ReadImageBytes(profileImage);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(*) FROM employee_details WHERE user_id=@userId";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    return RedirectToAction("EmployeeProfile");

                string insertQuery = @"INSERT INTO employee_details 
                                       (user_id, name, employee_code, department, designation,
                                        email, company, profile_image, profile_image_type)
                                       VALUES (@userId, @name, @code, @dept, @desg,
                                               @mail, @company, @image, @imageType)";
                MySqlCommand cmd = new MySqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@code", model.EmployeeCode);
                cmd.Parameters.AddWithValue("@dept", model.Department);
                cmd.Parameters.AddWithValue("@desg", model.Designation);
                cmd.Parameters.AddWithValue("@mail", model.Email);
                cmd.Parameters.AddWithValue("@company", model.Company ?? "");
                cmd.Parameters.AddWithValue("@image", (object?)imageBytes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@imageType", (object?)imageMime ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("EmployeeProfile");
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(
            EmployeeDetails model,
            IFormFile? profileImage,
            [FromForm] string? removeProfileImage)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            if (!ModelState.IsValid) return View("EmployeeProfile", model);
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            bool wantsRemove = removeProfileImage == "true";

            string updateQuery;
            MySqlCommand cmd;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                if (wantsRemove)
                {
                    // Null out both blob columns
                    updateQuery = @"UPDATE employee_details 
                                    SET name=@name, employee_code=@code, department=@dept,
                                        designation=@desg, email=@mail, company=@company,
                                        profile_image=NULL, profile_image_type=NULL
                                    WHERE user_id=@userId";
                    cmd = new MySqlCommand(updateQuery, conn);
                }
                else if (profileImage != null && profileImage.Length > 0)
                {
                    // Replace blob with newly uploaded image
                    var (imageBytes, imageMime) = await ReadImageBytes(profileImage);
                    updateQuery = @"UPDATE employee_details 
                                    SET name=@name, employee_code=@code, department=@dept,
                                        designation=@desg, email=@mail, company=@company,
                                        profile_image=@image, profile_image_type=@imageType
                                    WHERE user_id=@userId";
                    cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@image", imageBytes);
                    cmd.Parameters.AddWithValue("@imageType", imageMime);
                }
                else
                {
                    // Leave existing blob untouched
                    updateQuery = @"UPDATE employee_details 
                                    SET name=@name, employee_code=@code, department=@dept,
                                        designation=@desg, email=@mail, company=@company
                                    WHERE user_id=@userId";
                    cmd = new MySqlCommand(updateQuery, conn);
                }

                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@code", model.EmployeeCode);
                cmd.Parameters.AddWithValue("@dept", model.Department);
                cmd.Parameters.AddWithValue("@desg", model.Designation);
                cmd.Parameters.AddWithValue("@mail", model.Email);
                cmd.Parameters.AddWithValue("@company", model.Company ?? "");
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("EmployeeProfile");
        }

        [HttpPost]
        public IActionResult DeleteProfile()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            // Blob is deleted automatically with the row — no file cleanup needed
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string deleteQuery = "DELETE FROM employee_details WHERE user_id=@userId";
                MySqlCommand cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("EmployeeProfile");
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        /// <summary>Reads an IFormFile entirely into a byte array and returns its MIME type.</summary>
        private static async Task<(byte[] bytes, string mime)> ReadImageBytes(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            string mime = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType;
            return (ms.ToArray(), mime);
        }

        // ── STATUS ────────────────────────────────────────────────────────────
        public IActionResult Status(int? editSerial)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            List<StatusEntry> list = new List<StatusEntry>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"SELECT serial_number, user_status, date 
                                 FROM empstatus_entries 
                                 WHERE user_id=@userId 
                                 ORDER BY serial_number ASC";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(new StatusEntry
                        {
                            SerialNumber = Convert.ToInt32(reader["serial_number"]),
                            UserStatus = reader["user_status"].ToString()!,
                            Date = Convert.ToDateTime(reader["date"])
                        });
                }
            }
            ViewBag.EditSerialNumber = editSerial;
            return View(list);
        }

        [HttpPost]
        public IActionResult AddStatus(string userStatus)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string getSerialQuery = @"SELECT IFNULL(MAX(serial_number), 0) + 1 
                                          FROM empstatus_entries WHERE user_id=@userId";
                MySqlCommand getCmd = new MySqlCommand(getSerialQuery, conn);
                getCmd.Parameters.AddWithValue("@userId", userId);
                int nextSerial = Convert.ToInt32(getCmd.ExecuteScalar());

                string insertQuery = @"INSERT INTO empstatus_entries (serial_number, user_status, date, user_id) 
                                       VALUES (@serial, @status, @date, @userId)";
                MySqlCommand cmd = new MySqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@serial", nextSerial);
                cmd.Parameters.AddWithValue("@status", userStatus);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Status");
        }

        [HttpPost]
        public IActionResult EditInline(int serialNumber, string userStatus)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"UPDATE empstatus_entries 
                                 SET user_status=@status 
                                 WHERE serial_number=@serial AND user_id=@userId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@status", userStatus);
                cmd.Parameters.AddWithValue("@serial", serialNumber);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Status");
        }

        [HttpPost]
        public IActionResult Delete(int serialNumber)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true") return RedirectToAction("Login");
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string deleteQuery = @"DELETE FROM empstatus_entries 
                                       WHERE serial_number=@serial AND user_id=@userId";
                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@serial", serialNumber);
                deleteCmd.Parameters.AddWithValue("@userId", userId);
                deleteCmd.ExecuteNonQuery();

                string shiftQuery = @"UPDATE empstatus_entries 
                                      SET serial_number = serial_number - 1 
                                      WHERE serial_number > @serial AND user_id=@userId";
                MySqlCommand shiftCmd = new MySqlCommand(shiftQuery, conn);
                shiftCmd.Parameters.AddWithValue("@serial", serialNumber);
                shiftCmd.Parameters.AddWithValue("@userId", userId);
                shiftCmd.ExecuteNonQuery();
            }
            return RedirectToAction("Status");
        }

        // ── SALARY ───────────────────────────────────────────────────────────────

        public IActionResult Salary()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Login");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var list = new List<WorkPulse.Models.SalaryEntry>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT * FROM salary_details WHERE user_id=@uid ORDER BY year DESC, month DESC",
                    conn);
                cmd.Parameters.AddWithValue("@uid", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new WorkPulse.Models.SalaryEntry
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserId = Convert.ToInt32(reader["user_id"]),
                        Month = Convert.ToInt32(reader["month"]),
                        Year = Convert.ToInt32(reader["year"]),
                        BasicSalary = Convert.ToDecimal(reader["basic_salary"]),
                        HRA = Convert.ToDecimal(reader["hra"]),
                        Bonus = Convert.ToDecimal(reader["bonus"]),
                        PFDeduction = Convert.ToDecimal(reader["pf_deduction"]),
                        TaxDeduction = Convert.ToDecimal(reader["tax_deduction"]),
                        NetPay = Convert.ToDecimal(reader["net_pay"])
                    });
                }
            }
            return View(list);
        }

        [HttpPost]
        public IActionResult AddSalary(int month, int year, decimal basicSalary,
            decimal hra, decimal bonus, decimal pfDeduction,
            decimal taxDeduction, decimal netPay)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Login");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Upsert — update if exists, insert if not
                var cmd = new MySqlCommand(@"
            INSERT INTO salary_details
                (user_id, month, year, basic_salary, hra, bonus, pf_deduction, tax_deduction, net_pay)
            VALUES
                (@uid, @month, @year, @basic, @hra, @bonus, @pf, @tax, @net)
            ON DUPLICATE KEY UPDATE
                basic_salary  = @basic,
                hra           = @hra,
                bonus         = @bonus,
                pf_deduction  = @pf,
                tax_deduction = @tax,
                net_pay       = @net", conn);

                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@basic", basicSalary);
                cmd.Parameters.AddWithValue("@hra", hra);
                cmd.Parameters.AddWithValue("@bonus", bonus);
                cmd.Parameters.AddWithValue("@pf", pfDeduction);
                cmd.Parameters.AddWithValue("@tax", taxDeduction);
                cmd.Parameters.AddWithValue("@net", netPay);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Salary");
        }

        public IActionResult DownloadPayslip(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Login");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            WorkPulse.Models.SalaryEntry? entry = null;
            string empName = HttpContext.Session.GetString("username") ?? "Employee";
            string empCode = "", dept = "", desig = "";

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // Get salary entry
                var cmd = new MySqlCommand(
                    "SELECT * FROM salary_details WHERE id=@id AND user_id=@uid", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        entry = new WorkPulse.Models.SalaryEntry
                        {
                            Id = Convert.ToInt32(r["id"]),
                            Month = Convert.ToInt32(r["month"]),
                            Year = Convert.ToInt32(r["year"]),
                            BasicSalary = Convert.ToDecimal(r["basic_salary"]),
                            HRA = Convert.ToDecimal(r["hra"]),
                            Bonus = Convert.ToDecimal(r["bonus"]),
                            PFDeduction = Convert.ToDecimal(r["pf_deduction"]),
                            TaxDeduction = Convert.ToDecimal(r["tax_deduction"]),
                            NetPay = Convert.ToDecimal(r["net_pay"])
                        };
                    }
                }

                if (entry == null) return NotFound();

                // Get employee details
                var empCmd = new MySqlCommand(
                    "SELECT name, employee_code, department, designation FROM employee_details WHERE user_id=@uid",
                    conn);
                empCmd.Parameters.AddWithValue("@uid", userId);
                using var er = empCmd.ExecuteReader();
                if (er.Read())
                {
                    empName = er["name"].ToString() ?? empName;
                    empCode = er["employee_code"].ToString() ?? "";
                    dept = er["department"].ToString() ?? "";
                    desig = er["designation"].ToString() ?? "";
                }
            }

            // ── Generate PDF with iTextSharp ──
            using var ms = new MemoryStream();
            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Fonts
            var fontTitle = iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 18,
                                 new iTextSharp.text.BaseColor(30, 58, 95));
            var fontH2 = iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 11,
                                 new iTextSharp.text.BaseColor(30, 58, 95));
            var fontBold = iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 10,
                                 iTextSharp.text.BaseColor.BLACK);
            var fontNormal = iTextSharp.text.FontFactory.GetFont("Helvetica", 10,
                                 new iTextSharp.text.BaseColor(55, 65, 81));
            var fontSmall = iTextSharp.text.FontFactory.GetFont("Helvetica", 9,
                                 new iTextSharp.text.BaseColor(107, 114, 128));
            var fontGreen = iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 11,
                                 new iTextSharp.text.BaseColor(22, 163, 74));
            var fontRed = iTextSharp.text.FontFactory.GetFont("Helvetica", 10,
                                 new iTextSharp.text.BaseColor(220, 38, 38));
            var white = iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 14,
                                 iTextSharp.text.BaseColor.WHITE);
            var whiteSmall = iTextSharp.text.FontFactory.GetFont("Helvetica", 9,
                                 new iTextSharp.text.BaseColor(147, 173, 200));

            var navy = new iTextSharp.text.BaseColor(30, 58, 95);
            var light = new iTextSharp.text.BaseColor(244, 246, 251);
            var lineC = new iTextSharp.text.BaseColor(229, 231, 235);

            string monthYear = new DateTime(entry.Year, entry.Month, 1).ToString("MMMM yyyy");

            // ── HEADER BANNER ──
            var headerTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
            headerTable.SetWidths(new float[] { 2f, 1f });

            var leftCell = new iTextSharp.text.pdf.PdfPCell
            {
                BackgroundColor = navy,
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                Padding = 18
            };
            leftCell.AddElement(new iTextSharp.text.Paragraph("WorkPulse", white));
            leftCell.AddElement(new iTextSharp.text.Paragraph("Employee Payslip", whiteSmall));
            headerTable.AddCell(leftCell);

            var rightCell = new iTextSharp.text.pdf.PdfPCell
            {
                BackgroundColor = navy,
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                Padding = 18,
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT
            };
            rightCell.AddElement(new iTextSharp.text.Paragraph(monthYear, white)
            { Alignment = iTextSharp.text.Element.ALIGN_RIGHT });
            headerTable.AddCell(rightCell);
            doc.Add(headerTable);

            doc.Add(new iTextSharp.text.Paragraph(" "));

            // ── EMPLOYEE INFO ──
            var infoTable = new iTextSharp.text.pdf.PdfPTable(4) { WidthPercentage = 100 };
            infoTable.SetWidths(new float[] { 1f, 2f, 1f, 2f });

            void AddInfoRow(string k1, string v1, string k2, string v2)
            {
                infoTable.AddCell(new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(k1, fontSmall))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, PaddingBottom = 5 });
                infoTable.AddCell(new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(v1, fontBold))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, PaddingBottom = 5 });
                infoTable.AddCell(new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(k2, fontSmall))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, PaddingBottom = 5 });
                infoTable.AddCell(new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(v2, fontBold))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, PaddingBottom = 5 });
            }

            AddInfoRow("Employee", empName, "Emp Code", empCode);
            AddInfoRow("Department", dept, "Designation", desig);
            AddInfoRow("Pay Period", monthYear, "Pay Date", DateTime.Now.ToString("dd MMM yyyy"));
            doc.Add(infoTable);

            // doc.Add(new iTextSharp.text.pdf.LineSeparator(0.5f, 100f, lineC, iTextSharp.text.Element.ALIGN_CENTER, -4));
            // doc.Add(new iTextSharp.text.Paragraph(" "));

            doc.Add(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, lineC, iTextSharp.text.Element.ALIGN_CENTER, -4));
            doc.Add(new iTextSharp.text.Paragraph(" "));

            // ── EARNINGS & DEDUCTIONS ──
            var salTable = new iTextSharp.text.pdf.PdfPTable(4) { WidthPercentage = 100 };
            salTable.SetWidths(new float[] { 2f, 1.5f, 2f, 1.5f });

            iTextSharp.text.pdf.PdfPCell HeaderCell(string t) =>
                new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(t, fontH2))
                {
                    BackgroundColor = light,
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    Padding = 8,
                    PaddingBottom = 10
                };

            iTextSharp.text.pdf.PdfPCell DataCell(string t, iTextSharp.text.Font f, int align = iTextSharp.text.Element.ALIGN_LEFT) =>
                new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(t, f))
                {
                    Border = iTextSharp.text.Rectangle.BOTTOM_BORDER,
                    BorderColor = lineC,
                    BorderWidth = 0.5f,
                    Padding = 7,
                    HorizontalAlignment = align
                };

            salTable.AddCell(HeaderCell("Earnings"));
            salTable.AddCell(HeaderCell("Amount (₹)"));
            salTable.AddCell(HeaderCell("Deductions"));
            salTable.AddCell(HeaderCell("Amount (₹)"));

            string Fmt(decimal v) => v.ToString("N2");

            salTable.AddCell(DataCell("Basic Salary", fontNormal));
            salTable.AddCell(DataCell(Fmt(entry.BasicSalary), fontNormal, iTextSharp.text.Element.ALIGN_RIGHT));
            salTable.AddCell(DataCell("PF (12% of Basic)", fontNormal));
            salTable.AddCell(DataCell(Fmt(entry.PFDeduction), fontRed, iTextSharp.text.Element.ALIGN_RIGHT));

            salTable.AddCell(DataCell("HRA (40% of Basic)", fontNormal));
            salTable.AddCell(DataCell(Fmt(entry.HRA), fontNormal, iTextSharp.text.Element.ALIGN_RIGHT));
            salTable.AddCell(DataCell("Tax / TDS (10% of Basic)", fontNormal));
            salTable.AddCell(DataCell(Fmt(entry.TaxDeduction), fontRed, iTextSharp.text.Element.ALIGN_RIGHT));

            salTable.AddCell(DataCell("Bonus", fontNormal));
            salTable.AddCell(DataCell(Fmt(entry.Bonus), fontNormal, iTextSharp.text.Element.ALIGN_RIGHT));
            salTable.AddCell(DataCell("", fontNormal));
            salTable.AddCell(DataCell("", fontNormal));

            // Totals row
            iTextSharp.text.pdf.PdfPCell TotalCell(string t, iTextSharp.text.Font f) =>
                new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(t, f))
                {
                    BackgroundColor = light,
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    Padding = 9
                };

            salTable.AddCell(TotalCell("Gross Pay", fontBold));
            salTable.AddCell(TotalCell(Fmt(entry.GrossPay), fontBold));
            salTable.AddCell(TotalCell("Total Deductions", fontBold));
            salTable.AddCell(TotalCell(Fmt(entry.TotalDeductions), fontBold));

            doc.Add(salTable);
            doc.Add(new iTextSharp.text.Paragraph(" "));

            // ── NET PAY BOX ──
            var netTable = new iTextSharp.text.pdf.PdfPTable(1) { WidthPercentage = 100 };
            var netCell = new iTextSharp.text.pdf.PdfPCell
            {
                BackgroundColor = navy,
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                Padding = 14,
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
            };
            netCell.AddElement(new iTextSharp.text.Paragraph(
                $"Net Take-Home Pay:  ₹{entry.NetPay:N2}",
                iTextSharp.text.FontFactory.GetFont("Helvetica-Bold", 14, iTextSharp.text.BaseColor.WHITE))
            { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
            netTable.AddCell(netCell);
            doc.Add(netTable);

            doc.Add(new iTextSharp.text.Paragraph(" "));
            doc.Add(new iTextSharp.text.Paragraph(
                "This is a system generated payslip. — WorkPulse",
                iTextSharp.text.FontFactory.GetFont("Helvetica", 8,
                    new iTextSharp.text.BaseColor(156, 163, 175)))
            { Alignment = iTextSharp.text.Element.ALIGN_CENTER });

            doc.Close();

            string fileName = $"Payslip_{empName.Replace(" ", "_")}_{monthYear.Replace(" ", "_")}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }

        // ── LOGOUT ────────────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("Login");
        }
    }

    public class CheckUsernameRequest { public string Username { get; set; } = ""; }
    public class ResetPasswordRequest
    {
        public string Username { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}