# TripTogether API

A mobile-first collaborative platform designed to eliminate the friction of group travel planning and management.

## 🌟 Overview

TripTogether unifies travel planning, decision-making, expense tracking, and memory sharing into a single cohesive platform. It replaces scattered chat groups and spreadsheets with a democratic workflow featuring an "Electric Bento" styled interface.

## 🚀 Key Features

### 📊 Democratized Planning
- **Unified Polling System**: Interactive voting cards for group consensus on dates, destinations, and budgets
- **Collaborative Itinerary**: Drag-and-drop scheduler that transforms ideas into concrete timelines
- **Flexible Time Slots**: Support for both fixed activities and adaptable scheduling

### 💰 Smart Expense Management
- **Granular Expense Tracking**: Precise "Split Between" logic (e.g., separate alcohol costs from shared meals)
- **Automated Settlement**: Calculate net balances and facilitate P2P settlement via QR codes
- **Transparent Ledger**: No one gets stuck with the bill

### 🎒 Intelligent Logistics
- **Dual-Layer Packing System**:
  - **Shared Gear**: Items claimed by one person for the group (tents, cooking equipment)
  - **Personal Needs**: Individual requirements (passports, medications)
- **Trip Dashboard**: Centralized metadata storage (Wi-Fi passwords, emergency contacts, accommodations)

### 🎮 Social Gamification
- **Private Trip Gallery**: Shared photo feed for trip memories
- **Achievement System**: Data-driven badges rewarding engagement ("Planner Pro", "Early Bird")

## 🏗️ Technical Architecture

### Tech Stack
- **.NET 8** - Modern C# framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL** - Primary database with JSONB support
- **Clean Architecture** - Separation of concerns with clear boundaries

### Key Design Patterns
- **Repository Pattern** with Unit of Work
- **Global Query Filters** for soft deletes
- **Strongly Typed JSON Mapping** for flexible schema-less data
- **Enum String Conversion** for database readability
- **UUID Primary Keys** for robust identification

### Project Structure
```
TripTogether.API/
├── TripTogether.API/           # Web API layer
├── TripTogether.Application/   # Application services & DTOs
├── TripTogether.Domain/        # Domain entities & business logic
└── TripTogether.Infrastructure/ # Data access & external services
```

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (optional)

## 🛠️ Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/TripT0gether/TripTogether.API.git
cd TripTogether.API
```

### 2. Database Setup

#### Option A: Using Docker Compose
```bash
docker-compose up -d
```

#### Option B: Local PostgreSQL
1. Create a PostgreSQL database named `triptogether`
2. Update the connection string in `appsettings.json`

### 3. Configure Application Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=triptogether;Username=your_user;Password=your_password"
  }
}
```

### 4. Apply Database Migrations
```bash
dotnet ef database update --project TripTogether.Domain
```

### 5. Run the Application
```bash
dotnet run --project TripTogether.API
```

The API will be available at `https://localhost:7095` (HTTPS) or `http://localhost:5095` (HTTP).

## 🗄️ Database Schema

### Core Entities
- **User** - Platform users with authentication
- **Group** - Travel groups with member management
- **Trip** - Individual trips with metadata and settings
- **Activity** - Scheduled and unscheduled trip activities
- **Poll** - Democratic decision-making with voting
- **Expense** - Financial tracking with smart splitting
- **PackingItem** - Shared and personal packing management

### Advanced Features
- **JSONB Storage** for flexible trip settings and badge criteria
- **Soft Deletes** with global query filters
- **Audit Trails** with created/updated timestamps
- **Role-Based Access** for group member permissions

## 🔧 Development Commands

### Entity Framework
```bash
# Add a new migration
dotnet ef migrations add MigrationName --project TripTogether.Domain

# Update database
dotnet ef database update --project TripTogether.Domain

# Remove last migration
dotnet ef migrations remove --project TripTogether.Domain
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Build & Package
```bash
# Build solution
dotnet build

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## 📁 Key Components

### Repository Pattern
- **GenericRepository**: Base repository with common CRUD operations
- **UnitOfWork**: Transaction management and repository coordination
- **Entity-Specific Repositories**: Specialized logic for complex entities

### Services
- **IBlobService**: File storage and management
- **ICurrentTime**: Time abstraction for testing
- **IClaimsService**: User context and authorization

### Domain Models
- **BaseEntity**: Common properties (Id, timestamps, soft delete)
- **Strong Typing**: Enum conversions and value objects
- **Business Rules**: Domain logic encapsulation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the coding standards defined in `.github/instructions/`
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

### Coding Standards
- Follow Microsoft .NET coding conventions
- Use file-scoped namespaces
- Implement proper error handling with `nameof`
- Write XML documentation for public APIs
- Ensure all tests pass before submitting

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- [API Documentation](docs/api.md)
- [Database Schema](docs/database.md)
- [Deployment Guide](docs/deployment.md)
- [Contributing Guidelines](.github/CONTRIBUTING.md)

## 📞 Support

For support, email support@triptogether.app or join our [Discord community](https://discord.gg/triptogether).

---

Made with ❤️ by the TripTogether team

