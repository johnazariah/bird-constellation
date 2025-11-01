# 🦉 Owlet
*Part of the Bird Constellation - Local-First AI Productivity*

## 🌌 The Bird Constellation Vision

The **Bird Constellation** is an ecosystem of specialized, interoperable services that work together to create a powerful local-first AI productivity environment. Each "bird" handles a specific domain with excellence:

- **🦉 Owlet** *(This Repository)* - The Librarian: Document indexing, search, and knowledge foundation
- **💡 Lumen** - The Illuminator: AI-powered insights, analysis, and question answering
- **🦢 Cygnet** - The Orchestrator: Workflow automation and document processing pipelines
- **🕊️ Dove** - The Messenger: Cross-service communication and event streaming

All services share common principles: **local-first architecture**, **privacy-preserving AI**, **simple installation**, and **constellation protocol** for seamless integration.

---

## About Owlet

Owlet is a production-ready, local-first document indexing and search application designed for seamless installation and operation on Windows machines. It runs as a Windows service with an embedded web UI, providing file discovery, content extraction, and semantic search capabilities while serving as the foundational knowledge layer for the entire Bird Constellation ecosystem.

## 🚀 Quick Start

### Development Environment
```bash
# Clone the repository
git clone https://github.com/yourusername/owlet.git
cd owlet

# Restore dependencies
dotnet restore

# Run with Aspire orchestration (recommended for development)
dotnet run --project src/Owlet.AppHost

# Or run the pure service (production mode)
dotnet run --project src/Owlet.Service
```

Access the Aspire dashboard at the URL shown in the console for service monitoring and logs.

### Production Installation
Download the latest MSI installer from the [releases page](https://github.com/yourusername/owlet/releases) and run as administrator. The service will start automatically and be accessible at `http://localhost:5555`.

## 📋 Documentation

### Essential Reading
- **📋 [Technical Specification](project/spec/owlet-specification.md)** - Complete architecture and implementation details
- **🏗️ [Architecture Decision Records](project/adr/README.md)** - Key design decisions and rationale
- **💻 [Coding Standards](.github/coding-standards.md)** - Development patterns and practices
- **🎯 [Vision Document](spec/00-vision.md)** - Project goals and philosophy

### Development Guide
- **🛠️ [Copilot Instructions](.github/copilot-instructions.md)** - AI assistant guidance for contributors
- **📝 [Feature Backlog](spec/backlog_1.md)** - Planned features and priorities

## 🏗️ Architecture

### Strategic Approach
- **Pure Windows Service First**: Simple MSI installer for end users
- **Aspire Development**: Rich orchestration for developers and constellation
- **Local-First**: All data stays on your machine
- **Production-Ready**: Enterprise-grade reliability and installation

### Technology Stack
- **.NET 9** with Aspire orchestration
- **Windows Service** with embedded web server
- **Carter** for functional API composition
- **SQLite** database with full-text search
- **Serilog** structured logging
- **WiX Toolset** MSI packaging

### Solution Structure
```
src/
├── Owlet.AppHost          # Aspire orchestration (dev + constellation)
├── Owlet.Service          # Pure Windows service (production)
├── Owlet.Core             # Business logic and domain models
├── Owlet.Api              # Web API with Carter modules
├── Owlet.Indexer          # File monitoring and processing
├── Owlet.Extractors       # Content extraction pipeline
├── Owlet.Infrastructure   # Data access and external services
└── Owlet.ServiceDefaults  # Aspire configuration

tools/
├── Owlet.TrayApp          # System tray application
└── Owlet.Diagnostics     # Health checks and diagnostics

packaging/
├── installer/             # WiX MSI installer
├── dependencies/          # Bundled runtime dependencies
└── scripts/               # Installation scripts
```

## 🔌 API Overview

### Core Endpoints
- `GET /api/search?q={query}` - Search indexed documents
- `POST /api/folders` - Add folder to watch list
- `GET /api/files` - List indexed files
- `POST /api/files/{id}/tags` - Tag management

### Constellation Integration
- `GET /constellation/capabilities` - Service capabilities
- `GET /constellation/info` - Service metadata
- `GET /api/events` - Event stream for ecosystem

### Health & Diagnostics
- `GET /health` - Service health check
- `GET /metrics` - Performance metrics

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Owlet.Tests.Unit
dotnet test tests/Owlet.Tests.Integration

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📦 Building & Packaging

```bash
# Build solution
dotnet build --configuration Release

# Publish self-contained service
dotnet publish src/Owlet.Service --configuration Release --self-contained --runtime win-x64

# Build MSI installer (requires WiX Toolset)
# See packaging/installer/README.md for detailed instructions
```

## 🎯 Implementation Roadmap

### Phase 0: Foundation (Weeks 1-2) ⏳
- [x] Solution structure and project organization
- [x] Architecture Decision Records (ADRs)
- [x] Technical specification
- [ ] CI/CD pipelines
- [ ] WiX installer project

### Phase 1: Core Service (Weeks 3-4)
- [ ] Windows service host with Aspire integration
- [ ] SQLite database with EF Core
- [ ] File system monitoring
- [ ] Basic content extraction
- [ ] Search API with Carter

### Phase 2: Production Hardening (Weeks 5-6)
- [ ] Error handling and resilience
- [ ] Performance monitoring
- [ ] Security hardening
- [ ] MSI installer with signing

### Phase 3: Advanced Features (Weeks 7-8)
- [ ] Semantic search with Ollama
- [ ] Image processing and metadata
- [ ] System tray application
- [ ] Constellation protocol

## 🤝 Contributing

1. **Read the documentation** - Start with the [technical specification](project/spec/owlet-specification.md)
2. **Check ADRs** - Understand architectural decisions in [project/adr/](project/adr/)
3. **Follow coding standards** - See [.github/coding-standards.md](.github/coding-standards.md)
4. **Create feature branch** - Use descriptive names like `feature/search-api`
5. **Write tests** - Follow the established testing patterns
6. **Submit PR** - Include clear description and reference relevant ADRs

### Development Environment Setup
1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
3. Install [WiX Toolset](https://wixtoolset.org/) (for installer development)
4. Clone repository and run `dotnet restore`

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Constellation Integration

Owlet is designed as the foundation service for the Bird Constellation. Other services discover and connect to Owlet through the **Constellation Protocol**:

- **Service Discovery**: Automatic detection of Owlet instance on local network
- **Event Streaming**: Real-time notifications of indexing and search events (`/api/events`)
- **Capability Negotiation**: Dynamic feature discovery (`/constellation/capabilities`)
- **Shared Knowledge**: All constellation services query Owlet for document knowledge

Future constellation services (Lumen, Cygnet, Dove) will build upon Owlet's indexing and search capabilities, creating a cohesive local-first AI productivity ecosystem.

---

## 📞 Support & Community

- **Constellation Repository**: [bird-constellation](https://github.com/yourusername/bird-constellation)
- **Issues**: [GitHub Issues](https://github.com/yourusername/owlet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/owlet/discussions)
- **Documentation**: [Project Wiki](https://github.com/yourusername/owlet/wiki)

---

*Part of the Bird Constellation - Built with ❤️ for local-first AI productivity*