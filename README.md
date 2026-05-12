# Movement Unlimited Demo

ASP.NET Core MVC scheduling and session workflow application for Movement Unlimited.

## Portfolio Note

This project was originally built as a group project. This repository is my personal portfolio copy, adapted with demo data, owner-level demo access, and cleanup work so the application can be reviewed independently.

## Features

- Staff login with role-based access for Owner, Administration, and Trainer users
- Demo owner account for reviewing the full application
- Client and session management
- Session workflow from admin setup to trainer logging to final closing review
- Reports and export tools
- Seeded demo data for portfolio review

## Documentation

- [Project README PDF](docs/README.pdf)
- [User Manual](docs/UserManual.pdf)
- [Default Demo Accounts](docs/DefaultAccounts.pdf)

## Demo Access

Use the **Login with Demo Account** button on the login page. The seeded demo account has Owner access so reviewers can see the full application.

## Running Locally

Open the solution in Visual Studio or run:

```bash
dotnet build MU5PrototypeProject/MU5PrototypeProject.sln
dotnet run --project MU5PrototypeProject/MU5PrototypeProject/MU5PrototypeProject.csproj --launch-profile http
```

Then open:

```text
http://localhost:5155
```
