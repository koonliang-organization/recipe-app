# MyRecipeBox 📱🍽️

A full-stack mobile recipe management application built with React Native and .NET, featuring clean architecture, serverless backend, and seamless local development experience.

This project is implemented from scratch using Claude Code, no other project reference is used.

## 🎯 Overview

**MyRecipeBox** is a personal recipe management mobile app that allows users to create, organize, and discover recipes. The app features a modern dark theme with orange accents and provides both authenticated and anonymous demo modes for easy exploration.

### ✨ Key Features

- **User Authentication**: Secure signup, login, and password recovery
- **Recipe Management**: Full CRUD operations for personal recipes
- **Smart Organization**: Categories, search, and favorites functionality  
- **Image Support**: Photo upload and storage for recipes
- **Offline-First**: Anonymous demo mode with mock data
- **Responsive Design**: Optimized for both iOS and Android

## 🏗️ Architecture

The project follows **Clean Architecture** principles with clear separation between presentation, business logic, and infrastructure layers.

![image info](architecture.png)

## 🛠️ Technology Stack

### **Frontend (Mobile App)**
- **React Native** with **Expo** - Cross-platform mobile development
- **TypeScript** - Type safety and enhanced developer experience
- **Expo Router** - File-based navigation system
- **React Hook Form + Yup** - Form handling and validation
- **Expo Secure Store** - Secure credential storage
- **React Context** - State management for authentication

### **Backend (Serverless API)**
- **.NET 9** with **ASP.NET Core** - Modern web API framework
- **AWS Lambda** - Serverless compute (User, Recipe, Authorizer functions)
- **Entity Framework Core** - ORM with MySQL provider
- **MediatR** - CQRS pattern implementation
- **Centralized Authorization** - Custom JWT Authorizer Lambda with gateway-level security
- **JWT Authentication** - Secure token-based auth with proper separation of concerns
- **Serilog** - Structured logging

### **Local Development**
- **Express.js Gateway** - Local request routing between services
- **CORS Support** - Expo development server integration  
- **Hot Reload** - Fast development iteration
- **Swagger/OpenAPI** - API documentation and testing

### **Infrastructure & Deployment**
- **AWS API Gateway** - Production API routing and management
- **AWS SAM** - Infrastructure as Code templates
- **MySQL Database** - Relational data storage with host connectivity support
- **Docker & Docker Compose** - Full containerization with multi-service orchestration
- **Host MySQL Integration** - Seamless connection from containers to host database instances

## 📁 Project Structure

```
claude-recipe-app/
├── frontend/                 # React Native Mobile App
│   ├── src/
│   │   ├── screens/          # App screens (Login, Home, Recipe, etc.)
│   │   ├── components/       # Reusable UI components
│   │   ├── services/         # API clients and business logic
│   │   ├── hooks/            # Custom React hooks
│   │   ├── contexts/         # React Context providers
│   │   └── types/            # TypeScript type definitions
│   ├── app/                  # Expo Router app directory
│   └── assets/               # Images, fonts, and static assets
│
├── backend/                  # .NET Serverless Backend
│   ├── src/
│   │   ├── Core/
│   │   │   ├── Domain/       # Entities, Value Objects, Domain Events
│   │   │   └── Application/  # Use Cases, DTOs, Interfaces
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/  # EF Core, Repositories, Database
│   │   │   └── Messaging/    # SQS/SNS integration
│   │   ├── Lambdas/
│   │   │   ├── User/         # Authentication Lambda (port 5001)
│   │   │   ├── Recipe/       # Recipe Management Lambda (port 5000)
│   │   │   └── Authorizer/   # JWT Authorization Lambda (port 5002)
│   │   └── IaC/
│   │       ├── sam/          # AWS SAM templates
│   │       └── terraform/    # Terraform configurations
│   └── tests/                # Unit and integration tests
│
├── backend-web/              # Local Development Gateway
│   ├── local-gateway.js      # Express.js proxy server (port 3000)
│   └── package.json          # Node.js dependencies
│
├── docker-compose.yml        # Multi-service container orchestration
└── README.md                 # This file
```

## 🚀 Quick Start

Choose between **Docker** (recommended for quick setup) or **Native** development:

### 🐳 Option A: Docker Development (Recommended)

**Prerequisites:**
- **Docker** and **Docker Compose**
- **Node.js** 18+ and npm
- **Expo CLI**: `npm install -g @expo/cli`
- **MySQL** running on host (port 3306)

**Quick Setup:**
```bash
# 1. Setup host MySQL database
CREATE DATABASE RecipeApp_Dev;
CREATE USER 'admin'@'%' IDENTIFIED BY 'pass1234';
GRANT ALL PRIVILEGES ON RecipeApp_Dev.* TO 'admin'@'%';

# 2. Clone and start backend services and gateway
git clone <repository-url>
cd claude-recipe-app
docker-compose up -d
# Services will be available at:
# - Gateway Service: http://localhost:3000
# - User Service (Auth): http://localhost:5001
# - Recipe Service: http://localhost:5000  
# - Authorizer Service: http://localhost:5002

# 4. Start mobile app  
cd frontend && npm install && npm start
```

#### Container Management Commands

```bash
# View running containers and logs
docker-compose ps
docker-compose logs backend
docker-compose logs gateway

# Rebuild containers after code changes
docker-compose build backend
docker-compose up backend --build

# Stop and remove containers
docker-compose down

# View container resource usage
docker stats
```

### 🔧 Option B: Native Development

**Prerequisites:**
- **Node.js** 18+ and npm
- **.NET 9 SDK**
- **Expo CLI**: `npm install -g @expo/cli`
- **MySQL** (local or cloud instance)

#### 1. Clone the Repository

```bash
git clone <repository-url>
cd claude-recipe-app
```

#### 2. Backend Setup

```bash
# Install .NET dependencies and build
cd backend
dotnet restore
dotnet build

# Set up environment variables (copy and configure)
cp src/Lambdas/User/environment-variables.example src/Lambdas/User/.env
cp src/Lambdas/Recipe/environment-variables.example src/Lambdas/Recipe/.env
cp src/Lambdas/Authorizer/.env.example src/Lambdas/Authorizer/.env

# Start User Lambda (Terminal 1)
cd src/Lambdas/User
dotnet run  # Runs on http://localhost:5001

# Start Recipe Lambda (Terminal 2)  
cd src/Lambdas/Recipe
dotnet run  # Runs on http://localhost:5000

# Start Authorizer Lambda (Terminal 3)
cd src/Lambdas/Authorizer
dotnet run  # Runs on http://localhost:5002
```

#### 3. Local Gateway Setup

```bash
# Terminal 4: Start the local API gateway
cd backend-web
npm install
npm start  # Runs on http://localhost:3000
```

#### 4. Frontend Setup

```bash
# Terminal 5: Start the mobile app
cd frontend
npm install

# For authenticated mode (real API)
echo "EXPO_PUBLIC_ANONYMOUS_MODE=false" > .env
echo "EXPO_PUBLIC_API_BASE_URL=http://localhost:3000" >> .env

# Start Expo development server
npm start
```

#### 5. Open the App

- **iOS**: Press `i` in the Expo CLI or scan QR code with iOS Camera
- **Android**: Press `a` in the Expo CLI or scan QR code with Expo Go app
- **Web**: Press `w` in the Expo CLI for web preview

## 🎭 Demo Mode

MyRecipeBox includes a built-in **anonymous demo mode** for easy exploration without backend setup:

```bash
# Enable demo mode
echo "EXPO_PUBLIC_ANONYMOUS_MODE=true" > frontend/.env
cd frontend && npm start
```

In demo mode, the app uses mock data and simulates all API interactions locally.

## 📱 Core Features

### Authentication Flow
- **Signup**: Create account with email and password
- **Login**: Secure authentication with JWT tokens
- **Password Reset**: Email-based password recovery
- **Profile Management**: View and update user information

### Recipe Management
- **Create Recipes**: Add recipes with ingredients, steps, and photos
- **Browse Recipes**: View all recipes with search and category filtering
- **Recipe Details**: Full recipe view with cooking instructions
- **Edit & Delete**: Update or remove existing recipes
- **Favorites**: Mark recipes as favorites for quick access

### User Experience
- **Dark Theme**: Modern dark UI with orange accent colors
- **Responsive Design**: Optimized for various screen sizes
- **Offline Support**: Local data caching and offline capabilities
- **Form Validation**: Real-time validation with helpful error messages
- **Loading States**: Smooth loading indicators and skeleton screens

## 🔧 Development

### Local Development Workflow

#### Docker Development (Recommended)
1. **Start Backend Container**: `docker-compose up backend --build`
2. **Start Local Gateway**: `cd backend-web && npm start`
3. **Start Mobile App**: `cd frontend && npm start`
4. **Development**: Code, test, and hot-reload changes (volumes auto-sync)

#### Native Development  
1. **Start Backend Services**: Run User, Recipe, and Authorizer Lambdas individually
2. **Start Local Gateway**: Route requests with authorization middleware  
3. **Start Mobile App**: Launch Expo development server
4. **Development**: Code, test, and hot-reload changes

### Authorization Architecture

The app uses a **centralized authorization system** with proper separation of concerns:

- **User Lambda**: Handles authentication (login, signup, tokens)
- **Authorizer Lambda**: Validates JWT tokens and extracts user context
- **Recipe Lambda**: Focuses on business logic, trusts authorization headers
- **Gateway**: Orchestrates authorization flow before routing requests

### API Documentation

When running locally (Docker or Native), access Swagger documentation:
- **User API**: http://localhost:5001/swagger
- **Recipe API**: http://localhost:5000/swagger
- **Authorizer API**: http://localhost:5002/swagger
- **Gateway Health**: http://localhost:3000/health

#### Docker-Specific Endpoints
```bash
# Container health checks
docker-compose ps
docker-compose logs backend
curl http://localhost:5000/health  # Recipe service health
curl http://localhost:5001/health  # User service health
```

### Request Flow

```
Frontend Request (JWT token in header)
    ↓
Gateway (port 3000)
    ↓
Authorizer Lambda (port 5002) - Validates JWT & extracts user context
    ↓ [if authorized]
Recipe/User Lambda (ports 5000/5001) - Receives user context in headers
```

### Testing

#### Docker Environment
```bash
# Backend tests in container
docker-compose exec backend dotnet test

# Build and test specific services
docker-compose build backend --no-cache
docker-compose run --rm backend dotnet test

# Frontend type checking
cd frontend
npm run typecheck
npm run lint
```

#### Native Environment
```bash
# Backend tests
cd backend
dotnet test

# Frontend type checking
cd frontend
npm run typecheck
npm run lint
```

## 🌐 Deployment

### AWS Deployment

```bash
# Deploy backend infrastructure
cd backend/src/IaC/sam
sam deploy --parameter-overrides Environment=prod

# Build and deploy mobile app
cd frontend
expo build:ios
expo build:android
```

### Environment Configuration

Set the following environment variables for production:

**Frontend:**
```bash
EXPO_PUBLIC_ANONYMOUS_MODE=false
EXPO_PUBLIC_API_BASE_URL=https://your-api-gateway-url.amazonaws.com/prod
```

**Backend:**
```bash
ConnectionStrings__DefaultConnection=your-mysql-connection-string
Jwt__SecretKey=your-jwt-secret-key
Jwt__Issuer=your-app-name
Jwt__Audience=your-app-users
```

## 🤝 Contributing

1. **Code Style**: Follow existing patterns and use provided linting rules
2. **Architecture**: Maintain clean architecture principles
3. **Testing**: Add tests for new features and bug fixes
4. **Documentation**: Update README and code comments as needed

### Development Guidelines

- **Frontend**: Use TypeScript, follow React/Expo best practices
- **Backend**: Follow .NET conventions, implement SOLID principles  
- **API**: Design RESTful endpoints with proper HTTP status codes
- **Database**: Use EF Core migrations for schema changes

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using React Native, .NET, and AWS**

For detailed setup instructions, see the individual README files in the `frontend/` and `backend/` directories.