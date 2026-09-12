<!-- BACKEND: .NET Core Setup -->

# Backend Installation Instructions

## Prerequisites
- .NET 6.0 or higher
- SQL Server (or SQL Server Express)
- Visual Studio 2022 or VS Code

## Setup Instructions

```bash
# Create new .NET project
dotnet new webapi -n ViolinClassAPI
cd ViolinClassAPI

# Add required NuGet packages
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Cors
dotnet add package Twilio

# Create database
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the API
dotnet run
```

API will be available at: https://localhost:5001 or http://localhost:5000

## Database Connection String
Update appsettings.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ViolinClassDB;Trusted_Connection=true;"
  }
}
```

## Environment Variables
Create appsettings.Development.json:
```json
{
  "Twilio": {
    "AccountSid": "your_twilio_account_sid",
    "AuthToken": "your_twilio_auth_token",
    "PhoneNumber": "+1234567890"
  },
  "Admin": {
    "Username": "joseph_edison",
    "Password": "secure_password_here"
  }
}
```
