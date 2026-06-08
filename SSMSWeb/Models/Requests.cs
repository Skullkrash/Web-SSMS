namespace SSMSWeb.Models;

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

public class RestoreDbRequest
{
    public string? Name { get; set; }
    public string? Path { get; set; }
}

public class CreateLoginRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Password { get; set; }
}

public class DropLoginRequest
{
    public string? Name { get; set; }
}

public class ToggleLoginRequest
{
    public string? Name { get; set; }
    public bool Enable { get; set; }
}

public class ChangePasswordRequest
{
    public string? Name { get; set; }
    public string? Password { get; set; }
}

public class ToggleServerRoleRequest
{
    public string? LoginName { get; set; }
    public string? RoleName { get; set; }
    public bool Add { get; set; }
}

public class QueryRequest
{
    public string? Query { get; set; }
    public string? Database { get; set; }
}
