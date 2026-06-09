using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;
using System.Text.RegularExpressions;

namespace SSMSWeb.Controllers;

public class DatabaseController : Controller
{
    [HttpPost]
    public IActionResult CreateDatabase([FromBody] CreateDbRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de base requis." });
        if (!Regex.IsMatch(request.Name, @"^[\w\-]+$")) return BadRequest(new { message = "Nom de base invalide." });

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand($"CREATE DATABASE [{request.Name}]", connection);
            cmd.ExecuteNonQuery();
            return Ok(new { success = true });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult DropDatabase([FromBody] DropDbRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de base requis." });

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var sql = $"""
                ALTER DATABASE [{request.Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{request.Name}];
                """;
            using var cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
            return Ok(new { success = true });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult BackupDatabase([FromBody] BackupDbRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de base requis." });
        if (string.IsNullOrWhiteSpace(request.Path)) return BadRequest(new { message = "Chemin de destination requis." });

        var allowedTypes = new[] { "FULL", "DIFFERENTIAL", "LOG" };
        if (!allowedTypes.Contains(request.Type?.ToUpperInvariant()))
            return BadRequest(new { message = "Type de sauvegarde invalide." });

        var backupType = request.Type!.ToUpperInvariant();
        string sql = backupType switch
        {
            "FULL" => $"BACKUP DATABASE [{request.Name}] TO DISK = N'{request.Path.Replace("'", "''")}' WITH FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")} - Full', STATS = 10;",
            "DIFFERENTIAL" => $"BACKUP DATABASE [{request.Name}] TO DISK = N'{request.Path.Replace("'", "''")}' WITH DIFFERENTIAL, FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")} - Diff', STATS = 10;",
            _ => $"BACKUP LOG [{request.Name}] TO DISK = N'{request.Path.Replace("'", "''")}' WITH FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")} - Log', STATS = 10;"
        };

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
            cmd.ExecuteNonQuery();
            return Ok(new { success = true });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult RestoreDatabase([FromBody] RestoreDbRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de base requis." });
        if (string.IsNullOrWhiteSpace(request.Path)) return BadRequest(new { message = "Chemin requis." });
        if (!Regex.IsMatch(request.Name, @"^[\w\-]+$")) return BadRequest(new { message = "Nom de base invalide." });

        try
        {
            var csBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            using var connection = new SqlConnection(csBuilder.ToString());
            connection.Open();

            var safeName = request.Name.Replace("'", "''");
            var killSql = $"IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{safeName}') " +
                          $"ALTER DATABASE [{request.Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            using (var cmd = new SqlCommand(killSql, connection) { CommandTimeout = 30 })
                cmd.ExecuteNonQuery();

            using (var cmd = new SqlCommand($"RESTORE DATABASE [{request.Name}] FROM DISK = @path WITH REPLACE, RECOVERY", connection) { CommandTimeout = 1800 })
            {
                cmd.Parameters.AddWithValue("@path", request.Path);
                cmd.ExecuteNonQuery();
            }
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
