using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SSMSWeb.Models;

namespace SSMSWeb.Controllers;

public class QueryController : Controller
{
    [HttpPost]
    public IActionResult ExecuteQuery([FromBody] QueryRequest request)
    {
        var connectionString = Request.Cookies["DbConnectionString"];
        if (string.IsNullOrEmpty(connectionString)) return Unauthorized();
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
