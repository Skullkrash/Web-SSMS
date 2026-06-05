using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;
using System.Diagnostics;

namespace SSMSWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Login");
    }

    #region VIEWS

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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #endregion

    #region CONNECTION

    [HttpPost]
    public IActionResult Connect([FromBody] ConnectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.ServerName))
        {
            return BadRequest("Le nom du serveur ne peut pas être vide.");
        }

        string connectionString = "";

        if (request.AuthType == "windows")
        {
            // Authentification Windows
            connectionString = $"Server={request.ServerName};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        }
        else if (request.AuthType == "sql")
        {
            // Authentification SQL (login & password)
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Nom d'utilisateur et mot de passe requis pour l'authentification SQL Server.");
            }
            connectionString = $"Server={request.ServerName};Database=master;User Id={request.Username};Password={request.Password};TrustServerCertificate=True;";
        }
        else
        {
            return BadRequest("Type d'authentification invalide.");
        }

        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open(); // Teste la connexion
            }

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
        // On supprime le cookie contenant la chaîne de connexion
        Response.Cookies.Delete("DbConnectionString");
        return Ok(new { success = true });
    }

    #endregion

    #region FETCH INFORMATION

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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    databases.Add(reader["name"].ToString()!);
            }
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

            using (var connection = new SqlConnection(csBuilder.ToString()))
            {
                connection.Open();
                using var command = new SqlCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    tables.Add(reader["TABLE_NAME"].ToString()!);
            }
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

            using (var connection = new SqlConnection(connectionString))
            {
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
            }
            return Json(logins);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la récupération des logins : {ex.Message}");
        }
    }

    #endregion

    #region DB MANAGEMENT

    [HttpPost]
    public IActionResult CreateDatabase([FromBody] CreateDbRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest("Nom de base requis.");

        // Validation simple : lettres, chiffres, underscores, tirets
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[\w\-]+$"))
            return BadRequest("Nom de base invalide.");

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
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest("Nom de base requis.");

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            // Force la déconnexion des sessions actives avant suppression
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

        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { message = "Nom de base requis." });

        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { message = "Chemin de destination requis." });

        var allowedTypes = new[] { "FULL", "DIFFERENTIAL", "LOG" };
        if (!allowedTypes.Contains(request.Type?.ToUpperInvariant()))
            return BadRequest(new { message = "Type de sauvegarde invalide." });

        var backupType = request.Type!.ToUpperInvariant();

        // Construction de la commande T-SQL selon le type
        string sql = backupType switch
        {
            "FULL" => $"""
            BACKUP DATABASE [{request.Name}]
            TO DISK = N'{request.Path.Replace("'", "''")}'
            WITH FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")}-Full', STATS = 10;
            """,

            "DIFFERENTIAL" => $"""
            BACKUP DATABASE [{request.Name}]
            TO DISK = N'{request.Path.Replace("'", "''")}'
            WITH DIFFERENTIAL, FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")}-Diff', STATS = 10;
            """,

            "LOG" => $"""
            BACKUP LOG [{request.Name}]
            TO DISK = N'{request.Path.Replace("'", "''")}'
            WITH FORMAT, INIT, NAME = N'{request.Name.Replace("'", "''")}-Log', STATS = 10;
            """,

            _ => throw new InvalidOperationException("Type invalide.")
        };

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Timeout élevé car les backups peuvent être longs
            using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
            cmd.ExecuteNonQuery();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region LOGIN MANAGEMENT

    [HttpPost]
    public IActionResult CreateLogin([FromBody] CreateLoginRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest("Nom de login requis.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[\w\\\-\.]+$"))
            return BadRequest("Nom de login invalide.");

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = request.Type == "windows"
                ? $"CREATE LOGIN [{request.Name}] FROM WINDOWS;"
                : $"CREATE LOGIN [{request.Name}] WITH PASSWORD = '{request.Password?.Replace("'", "''")}';";

            using var cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
            return Ok(new { success = true });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult DropLogin([FromBody] DropLoginRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest("Nom de login requis.");

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand($"DROP LOGIN [{request.Name}]", connection);
            cmd.ExecuteNonQuery();
            return Ok(new { success = true });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    #endregion

    [HttpPost]
    public IActionResult ExecuteQuery([FromBody] QueryRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request?.Query))
            return Ok(new { type = "error", text = "Requête vide." });

        try
        {
            var csBuilder = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrWhiteSpace(request.Database))
                csBuilder.InitialCatalog = request.Database;

            using var connection = new SqlConnection(csBuilder.ToString());
            connection.Open();
            using var command = new SqlCommand(request.Query, connection) { CommandTimeout = 30 };
            using var reader = command.ExecuteReader();

            if (reader.FieldCount > 0)
            {
                var columns = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.GetName(i)).ToList();

                const int maxRows = 1000;
                var rows = new List<List<string?>>();
                bool truncated = false;

                while (reader.Read())
                {
                    if (rows.Count >= maxRows) { truncated = true; break; }
                    var row = new List<string?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row.Add(reader.IsDBNull(i) ? null : reader[i]?.ToString());
                    rows.Add(row);
                }

                return Ok(new { type = "results", columns, rows, truncated });
            }
            else
            {
                var msg = reader.RecordsAffected >= 0
                    ? $"{reader.RecordsAffected} ligne(s) affectée(s)."
                    : "Commande exécutée avec succès.";
                return Ok(new { type = "message", text = msg });
            }
        }
        catch (Exception ex)
        {
            return Ok(new { type = "error", text = ex.Message });
        }
    }
}

public class ConnectRequest
{
    public string? ServerName { get; set; }
    public string? AuthType { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class CreateDbRequest
{
    public string? Name { get; set; }
}

public class DropDbRequest
{
    public string? Name { get; set; }
}

public class BackupDbRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Path { get; set; }
}

public class CreateLoginRequest
{ 
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Password { get; set; }
}

public class DropLoginRequest {
    public string? Name { get; set; }
}

public class QueryRequest
{
    public string? Query { get; set; }
    public string? Database { get; set; }
}
