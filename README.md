# SQL_RCE 

**⚠️ For Educational and Authorized Security Testing Only**

This tool is a C# console application that connects to a Microsoft SQL Server instance using Windows Integrated Authentication and interacts with `xp_cmdshell` to execute system-level commands. It is intended for **ethical hacking labs, penetration testing demos, and research environments** where you have **explicit authorization**.

---

## ⚙️ Features

- Connects to a specified SQL Server instance using integrated (Windows) authentication.
- Attempts to impersonate the `sa` login via `EXECUTE AS LOGIN`.
- Enables advanced options and `xp_cmdshell` using `sp_configure`.
- Provides an interactive command-line shell to run system commands via SQL Server.

---

## 📦 Requirements

- .NET SDK (for building the project)
- SQL Server with:
  - `xp_cmdshell` available (usually requires `sa` or sysadmin rights)
  - Integrated Security (i.e., Windows authentication)
  - Permissions to execute `EXECUTE AS LOGIN` and `xp_cmdshell`

---

## 🧪 Usage Instructions

### 🛠️ Build

You can build the project with the .NET CLI:

    ```bash
      dotnet build

▶️ Run

        SQL_RCE.exe

⌨️ Interact

You'll be prompted to enter the SQL Server instance:

          Enter the SQL Server instance: YOUR_SQL_SERVER\INSTANCE
          Auth success!

Then you can run commands:

        $command :::::> whoami
        sqlserver\administrator
        $command :::::> ipconfig
        ...
        $command :::::> exit
