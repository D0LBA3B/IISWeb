using IISWeb.Services;

namespace IISWeb;

public static class CommandLine
{
    public static bool IsCommand(string[] args, out string command)
    {
        command = string.Empty;
        if (args.Length == 0) return false;
        var first = args[0];
        if (string.Equals(first, "seed-admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(first, "--seed-admin", StringComparison.OrdinalIgnoreCase))
        {
            command = "seed-admin";
            return true;
        }
        return false;
    }

    public static async Task<int> SeedAdminAsync(IUserService users, string[] args)
    {
        string? username = null;
        string? password = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--username" or "-u" when i + 1 < args.Length:
                    username = args[++i]; break;
                case "--password" or "-p" when i + 1 < args.Length:
                    password = args[++i]; break;
                case "--help" or "-h":
                    PrintUsage(); return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            PrintUsage();
            return 2;
        }

        if (string.IsNullOrEmpty(password))
        {
            password = ReadPasswordFromConsole("Password: ");
            var confirm = ReadPasswordFromConsole("Confirm:  ");
            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Passwords do not match.");
                return 3;
            }
        }

        try
        {
            await users.CreateAdminAsync(username!, password!);
            Console.WriteLine($"Admin '{username}' has been created.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to create admin: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  IISWeb.exe seed-admin --username <user> [--password <pwd>]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("If --password is omitted you will be prompted (input is not echoed).");
        Console.Error.WriteLine("Passwords must be at least 12 characters long.");
    }

    private static string ReadPasswordFromConsole(string prompt)
    {
        Console.Write(prompt);
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                sb.Append(key.KeyChar);
        }
        return sb.ToString();
    }
}
