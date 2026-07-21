using IndustrialMonitor.Models;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndustrialMonitor.Services;

/// <summary>
/// 认证服务：用户登录验证、权限检查、用户管理
/// </summary>
public class AuthService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public User? CurrentUser { get; private set; }

    public AuthService()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.db");
        _connectionString = $"Data Source={_dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = @"CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            Role TEXT NOT NULL DEFAULT 'Viewer',
            IsActive INTEGER DEFAULT 1
        );";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();

        // 确保默认管理员存在
        var checkSql = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
        using var checkCmd = new SqliteCommand(checkSql, conn);
        var count = (long)checkCmd.ExecuteScalar()!;
        if (count == 0)
        {
            var insertSql = "INSERT INTO Users (Username, PasswordHash, Role) VALUES ('admin', $hash, 'Admin')";
            using var insCmd = new SqliteCommand(insertSql, conn);
            insCmd.Parameters.AddWithValue("$hash", HashPassword("admin123"));
            insCmd.ExecuteNonQuery();

            // 再创建一个操作员
            insertSql = "INSERT INTO Users (Username, PasswordHash, Role) VALUES ('operator', $hash, 'Operator')";
            using var insCmd2 = new SqliteCommand(insertSql, conn);
            insCmd2.Parameters.AddWithValue("$hash", HashPassword("operator123"));
            insCmd2.ExecuteNonQuery();

            // 再创建一个观察者
            insertSql = "INSERT INTO Users (Username, PasswordHash, Role) VALUES ('viewer', $hash, 'Viewer')";
            using var insCmd3 = new SqliteCommand(insertSql, conn);
            insCmd3.Parameters.AddWithValue("$hash", HashPassword("viewer123"));
            insCmd3.ExecuteNonQuery();
        }
    }

    public bool Login(string username, string password)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = "SELECT Id, Username, Role, IsActive FROM Users WHERE Username = $u AND PasswordHash = $h";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("$u", username);
        cmd.Parameters.AddWithValue("$h", HashPassword(password));

        using var reader = cmd.ExecuteReader();
        if (reader.Read() && reader.GetInt32(3) == 1)
        {
            CurrentUser = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Role = reader.GetString(2),
                IsActive = true
            };
            return true;
        }
        return false;
    }

    public void Logout() => CurrentUser = null;

    public bool HasPermission(string requiredRole) => CurrentUser?.Role switch
    {
        "Admin" => true,
        "Operator" => requiredRole != "Admin",
        "Viewer" => false,
        _ => false
    };

    public bool CanAccessPage(string pageKey) => CurrentUser?.Role switch
    {
        "Admin" => true,
        "Operator" => pageKey is not ("Devices" or "Settings" or "Users"),
        "Viewer" => pageKey is "Dashboard" or "Monitoring" or "Alarms" or "History",
        _ => false
    };

    public List<User> GetAllUsers()
    {
        var users = new List<User>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT Id, Username, Role, IsActive FROM Users", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Role = reader.GetString(2),
                IsActive = reader.GetInt32(3) == 1
            });
        return users;
    }

    public void AddUser(string username, string password, string role)
    {
        if (role == "Admin")
            throw new InvalidOperationException("管理员账户有且仅有一个，不能创建第二个管理员");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = "INSERT INTO Users (Username, PasswordHash, Role) VALUES ($u, $h, $r)";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("$u", username);
        cmd.Parameters.AddWithValue("$h", HashPassword(password));
        cmd.Parameters.AddWithValue("$r", role);
        cmd.ExecuteNonQuery();
    }

    public void ChangePassword(int userId, string newPassword)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = "UPDATE Users SET PasswordHash = $h WHERE Id = $id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("$h", HashPassword(newPassword));
        cmd.Parameters.AddWithValue("$id", userId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteUser(int userId, string role)
    {
        if (role == "Admin")
            throw new InvalidOperationException("管理员账户不可删除");
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("DELETE FROM Users WHERE Id = $id", conn);
        cmd.Parameters.AddWithValue("$id", userId);
        cmd.ExecuteNonQuery();
    }

    public void SetUserActive(int userId, bool isActive)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = "UPDATE Users SET IsActive = $a WHERE Id = $id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("$a", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", userId);
        cmd.ExecuteNonQuery();
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
