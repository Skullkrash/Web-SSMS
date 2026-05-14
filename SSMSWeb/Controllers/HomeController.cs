using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;
using System.Diagnostics;

namespace SSMSWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult Connect([FromBody] ConnectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.ConnectionString))
        {
            return BadRequest("La chaîne de connexion ne peut pas être vide.");
        }

        try
        {
            using (var connection = new SqlConnection(request.ConnectionString))
            {
                connection.Open(); // Exception if not working
            }

            Response.Cookies.Append("DbConnectionString", request.ConnectionString, new CookieOptions 
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

    [HttpGet]
    public IActionResult GetDatabases()
    {
        var connectionString = Request.Cookies["DbConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            return Unauthorized("Vous n'êtes pas connecté au serveur SQL.");
        }

        try
        {
            var databases = new List<string>();
            var query = "SELECT name FROM sys.databases ORDER BY name";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = new SqlCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    databases.Add(reader["name"].ToString());
                }
            }

            return Json(databases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la récupération des bases : {ex.Message}");
        }
    }
}

public class ConnectRequest
{
    public string? ConnectionString { get; set; }
}
