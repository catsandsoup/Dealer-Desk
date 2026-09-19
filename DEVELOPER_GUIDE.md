# Dealer Desk - Developer Guide

## Architecture Overview
Dealer Desk is a robust, resilient bullion dealing system built with modern Microsoft technologies.
- **UI Framework**: WinUI 3 (Windows App SDK) on .NET 10
- **Database**: PostgreSQL with exact precision `numeric(18,6)` for financial calculations to prevent floating-point drift.
- **Hardware Integrations**: Direct local camera integration via `CommunityToolkit.WinUI.Helpers` for compliance (ID and item captures), and COM port serial ingestion for weight hardware.
- **Auditability**: Cryptographic SHA-256 PDF generation hashing.

## Prerequisites
To build and run this project, you will need:
1. **.NET 10 SDK** (Version 10.0.400 or higher)
2. **PostgreSQL 15+** running locally (default connection expects `localhost`, port `5432`).
3. **Visual Studio 2022** (17.8+) with the following workloads:
   - .NET desktop development
   - Universal Windows Platform development (for Windows App SDK tools)
   - Windows 10/11 SDK (10.0.26100.0 or matching the TargetPlatformVersion)
4. **Git** for version control.

## Getting Started
1. Clone the repository.
2. Ensure your local PostgreSQL server is running and the credentials match `appsettings.json`.
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the solution:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run --project QuotingEngine.UI/QuotingEngine.UI.csproj
   ```

## Database Migrations
Entity Framework Core with the Npgsql provider is used for data access. To add or apply migrations:
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <MigrationName> --project QuotingEngine.Infrastructure --startup-project QuotingEngine.UI
dotnet ef database update --project QuotingEngine.Infrastructure --startup-project QuotingEngine.UI
```

## Code Signing & MSIX Installation (Local Testing)
Because MSIX packages require digital signatures to install on Windows, a local self-signed certificate has been generated for testing purposes: `DealerDeskLocalCert.cer` (located in the project root). 

Before you can install the `.msix` package on your local machine, you must trust this certificate:
1. Double-click `DealerDeskLocalCert.cer`.
2. Click **Install Certificate...**
3. Select **Local Machine** and click Next.
4. Select **Place all certificates in the following store** and click **Browse...**
5. Choose **Trusted Root Certification Authorities** and click OK.
6. Click Next, then Finish.

Once trusted, Windows will allow the installation of the test MSIX package.

## Packaging & Distribution
The application uses Single-Project MSIX packaging for clean deployment.
To create a release MSIX installer, run the publishing command:
```bash
dotnet publish QuotingEngine.UI/QuotingEngine.UI.csproj -c Release -p:RuntimeIdentifier=win-x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false
```
*(Note: For production distribution, you must configure a trusted signing certificate (`.pfx`) in the `.csproj` file and enable `AppxPackageSigningEnabled=true`)*.
