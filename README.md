# Saber IntradayRisk Platform System

An advanced intraday risk management and P&L monitoring platform designed for traders, risk managers, and financial stakeholders. The system provides real-time risk metric calculations and analytical dashboards to ensure secure and informed trading operations.

## 🏗️ Project Architecture

The solution is built using .NET and structured into three primary layers:

* **Saber.Risk.Core** – Core business logic, risk calculation engines, and domain models.
* **Saber.Risk.Api** – RESTful API endpoints exposing risk metrics and system configuration.
* **Saber.Risk.Client** – Desktop UI application for traders providing real-time data visualization.
* **DataBase/** – Contains SQL migration scripts and stored procedures (T-SQL).

## 🚀 Tech Stack

* **Backend:** C# (.NET)
* **Database:** T-SQL (SQL Server)
* **Architecture:** MVVM (Client), Dependency Injection, Async/Await asynchronous processing

## 🛠️ Getting Started

### Prerequisites
* .NET SDK (matching the version in `Saber.IntradayRisk.Platform.sln`)
* SQL Server or LocalDB instance
* IDE: Visual Studio 2022 / JetBrains Rider / VS Code

### Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com
   cd Saber.IntradayRisk.Platform
   ```

2. **Database Initialization:**
   Execute the migration scripts located in the `DataBase/` directory sequentially (e.g., starting from `001_...`) on your local database instance.

3. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```

4. **Build the Solution:**
   ```bash
   dotnet build Saber.IntradayRisk.Platform.sln
   ```

5. **Run the Application:**
   * Run the API layer: `dotnet run --project Saber.Risk.Api`
   * Run the Client layer: `dotnet run --project Saber.Risk.Client`

## 🧪 Testing

The repository enforces strict unit and integration testing coverage:
```bash
dotnet test
```
* **Unit Tests:** Covers ViewModels and business logic using mocking patterns.
* **Integration Tests:** Verifies data-access logic and SQL procedures.

## 🌐 Language Policy

* **Code & Public APIs:** Fully documented in **English** via XML documentation comments (`/// <summary>`).
* **Business Domain:** Supplemental business explanations and specific trader-focused notes may be available in **Polish** inside the `docs/pl/` folder or marked with a `// PL:` prefix in short inline comments.

## 🤝 Contributing

We welcome contributions to the Saber Risk Platform! Before writing any code, please review our strict quality guidelines in [CONTRIBUTING.md](CONTRIBUTING.md).

Key requirements:
* Follow the rules enforced by the `.editorconfig` file.
* Always use non-blocking asynchronous operations (`async`/`await`) for I/O tasks.
* Include rollback scripts (`Down` section) for any database modification under `DataBase/`.
* Code impacting critical risk calculations requires review from a senior engineer and a domain expert.

## 📄 License

This project is proprietary and confidential. All rights reserved.
