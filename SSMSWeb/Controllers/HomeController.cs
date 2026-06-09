using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;
using System.Diagnostics;

namespace SSMSWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Login");

    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(Request.Cookies["DbConnectionString"]))
            return RedirectToAction("Console");
        return View();
    }

    public IActionResult Console()
    {
        var connStr = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connStr))
            return RedirectToAction("Login");

        var builder = new SqlConnectionStringBuilder(connStr);
        ViewData["ServerName"] = builder.DataSource;
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [HttpPost]
    public IActionResult Connect([FromBody] ConnectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.ServerName))
            return BadRequest("Le nom du serveur ne peut pas être vide.");

        string connectionString;

        if (request.AuthType == "windows")
        {
            connectionString = $"Server={request.ServerName};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        }
        else if (request.AuthType == "sql")
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Nom d'utilisateur et mot de passe requis pour l'authentification SQL Server.");
            connectionString = $"Server={request.ServerName};Database=master;User Id={request.Username};Password={request.Password};TrustServerCertificate=True;";
        }
        else
        {
            return BadRequest("Type d'authentification invalide.");
        }

        try
        {
            using (var connection = new SqlConnection(connectionString))
                connection.Open();

            Response.Cookies.Append("DbConnectionString", connectionString, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Échec de la connexion : {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult Disconnect()
    {
        Response.Cookies.Delete("DbConnectionString");
        return Ok(new { success = true });
    }
}
