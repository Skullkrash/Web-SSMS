using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;
using System.Text.RegularExpressions;

namespace SSMSWeb.Controllers;

public class LoginController : Controller
{
    [HttpPost]
    public IActionResult CreateLogin([FromBody] CreateLoginRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de login requis." });
        if (!Regex.IsMatch(request.Name, @"^[\w\\\-\.]+$")) return BadRequest(new { message = "Nom de login invalide." });

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
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom de login requis." });

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

    [HttpPost]
    public IActionResult ToggleLogin([FromBody] ToggleLoginRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest(new { message = "Nom requis." });

        try
        {
            var safeName = request.Name.Replace("]", "]]");
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand($"ALTER LOGIN [{safeName}] {(request.Enable ? "ENABLE" : "DISABLE")}", connection);
            cmd.ExecuteNonQuery();
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult ChangeLoginPassword([FromBody] ChangePasswordRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name) || string.IsNullOrWhiteSpace(request?.Password))
            return BadRequest(new { message = "Nom et mot de passe requis." });

        try
        {
            var safeName = request.Name.Replace("]", "]]");
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand($"ALTER LOGIN [{safeName}] WITH PASSWORD = @pwd", connection);
            cmd.Parameters.AddWithValue("@pwd", request.Password);
            cmd.ExecuteNonQuery();
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet]
    public IActionResult GetServerRoles(string loginName)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(loginName)) return BadRequest(new { message = "Nom de login requis." });

        try
        {
            var query = @"
                SELECT
                    r.name,
                    CASE WHEN rm.member_principal_id IS NOT NULL THEN 1 ELSE 0 END AS is_member
                FROM sys.server_principals r
                LEFT JOIN sys.server_role_members rm
                    ON rm.role_principal_id = r.principal_id
                    AND rm.member_principal_id = (
                        SELECT principal_id FROM sys.server_principals WHERE name = @loginName
                    )
                WHERE r.type = 'R' AND r.name != 'public'
                ORDER BY r.name";

            var roles = new List<object>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@loginName", loginName);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                roles.Add(new
                {
                    name = reader["name"].ToString(),
                    isMember = (int)reader["is_member"] == 1
                });
            }
            return Json(roles);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpPost]
    public IActionResult ToggleServerRole([FromBody] ToggleServerRoleRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.LoginName) || string.IsNullOrWhiteSpace(request?.RoleName))
            return BadRequest(new { message = "Login et rôle requis." });

        try
        {
            var safeRole = request.RoleName.Replace("]", "]]");
            var safeLogin = request.LoginName.Replace("]", "]]");
            var action = request.Add ? "ADD MEMBER" : "DROP MEMBER";
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand($"ALTER SERVER ROLE [{safeRole}] {action} [{safeLogin}]", connection);
            cmd.ExecuteNonQuery();
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
