using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SSMSWeb.Controllers;

public class ExplorerController : Controller
{
    [HttpGet]
    public IActionResult GetDatabases()
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            return Unauthorized("Vous n'êtes pas connecté au serveur SQL.");

        try
        {
            var databases = new List<string>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                databases.Add(reader["name"].ToString()!);
            return Json(databases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la récupération des bases : {ex.Message}");
        }
    }

    [HttpGet]
    public IActionResult GetTables(string database)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            return Unauthorized("Vous n'êtes pas connecté au serveur SQL.");
        if (string.IsNullOrWhiteSpace(database))
            return BadRequest("Nom de base de données requis.");

        try
        {
            var csBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database };
            var tables = new List<string>();
            var query = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                          WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";

            using var connection = new SqlConnection(csBuilder.ToString());
            connection.Open();
            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                tables.Add(reader["TABLE_NAME"].ToString()!);
            return Json(tables);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la récupération des tables : {ex.Message}");
        }
    }

    [HttpGet]
    public IActionResult GetLogins()
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            return Unauthorized("Vous n'êtes pas connecté au serveur SQL.");

        try
        {
            var logins = new List<object>();
            var query = @"SELECT name, type_desc, is_disabled
                          FROM sys.server_principals
                          WHERE type IN ('S', 'U', 'G')
                            AND name NOT LIKE '##%'
                          ORDER BY name";

            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                logins.Add(new
                {
                    name = reader["name"].ToString(),
                    typeDesc = reader["type_desc"].ToString(),
                    isDisabled = (bool)reader["is_disabled"]
                });
            }
            return Json(logins);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la récupération des logins : {ex.Message}");
        }
    }
}
