# Movement Unlimited Demo

Movement Unlimited Demo is an ASP.NET Core MVC app for managing clients, staff access, and Pilates session workflows.

This started as a group project. This repo is my portfolio copy, with demo data, owner-level demo access, cleanup work, and a live hosted version for review.

## Live Demo

[Open the live demo](https://movement-unlimited-demo-bndcf6dwe0buevbd.canadaeast-01.azurewebsites.net)

Use the "Login with Demo Account" button on the login page. The demo signs in as Demo Owner Account, which has owner access so the full application can be reviewed.

## What It Includes

- Role-based staff access for Owner, Administration, and Trainer users
- Client profiles and session management
- Session flow from admin setup to trainer logging to closing review
- Progress indicator while moving through an active session workflow
- Reports and export tools
- Seeded demo data for portfolio review

## Documentation

- [Project README PDF](docs/README.pdf)
- [User Manual](docs/UserManual.pdf)
- [Default Demo Accounts](docs/DefaultAccounts.pdf)

## Run Locally

Open the solution in Visual Studio, or run:

```bash
dotnet build MU5PrototypeProject/MU5PrototypeProject.sln
dotnet run --project MU5PrototypeProject/MU5PrototypeProject/MU5PrototypeProject.csproj --launch-profile http
```

Then open:

```text
http://localhost:5155
```
