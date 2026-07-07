# Assetly

Assetly is an open source personal asset manager built with Java. It helps users organize their belongings, warranties, documents and maintenance history in a simple, offline-first way.

Assetly é um gerenciador pessoal open source de bens, garantias, documentos e manutenções. O objetivo é ajudar pessoas comuns a manterem seus itens importantes organizados, com controle de prazos, histórico e documentos locais.

## Status

Project in early MVP development. The current stage defines the base Java project structure, architecture direction and a minimal JavaFX application shell.

## Goals

- Register personal assets such as electronics, appliances, vehicles, tools and work equipment.
- Track warranties, expiration dates and support information.
- Keep document metadata for invoices, receipts, manuals and photos.
- Record maintenance history and upcoming maintenance dates.
- Generate local alerts for expiring warranties, overdue maintenance and missing documents.

## Tech Stack

- Java 21
- JavaFX
- Maven
- SQLite, planned for local persistence
- JUnit 5
- AssertJ

## Architecture

Assetly follows a lightweight architecture inspired by Domain-Driven Design and Clean Architecture.

- `domain`: business concepts and rules. It must not depend on JavaFX, SQLite or infrastructure details.
- `application`: use cases, application services and DTOs.
- `infrastructure`: database access, repositories and local file storage.
- `presentation`: JavaFX controllers, views and view models.

SQLite will be used as an embedded local database. The application will create the local database automatically in a folder ignored by Git in a future persistence stage.

Personal documents will be stored locally under `storage/`, also ignored by Git. The database should keep only metadata and local paths.

## Current Structure

```text
Assetly/
├── README.md
├── LICENSE
├── .gitignore
├── pom.xml
└── src/
    ├── main/
    │   ├── java/
    │   │   ├── module-info.java
    │   │   └── com/assetly/
    │   │       ├── AssetlyApplication.java
    │   │       ├── domain/
    │   │       ├── application/
    │   │       ├── infrastructure/
    │   │       └── presentation/
    │   └── resources/
    │       ├── css/
    │       ├── fxml/
    │       └── database/
    └── test/
        └── java/com/assetly/
```

## Running

Requirements:

- JDK 21 or newer
- Maven 3.9 or newer

Run the application:

```bash
mvn clean javafx:run
```

Run tests:

```bash
mvn test
```

## Privacy

Assetly is designed as a local-first application:

- no account required;
- no cloud dependency;
- no telemetry in the MVP;
- data stays on the user's computer;
- documents are stored in local folders ignored by Git.

Do not commit real databases, invoices, receipts, manuals, photos, backups, exports or personal documents.

## Planned Features

- Asset registration, editing, listing and status changes.
- Warranty registration and expiration indicators.
- Maintenance scheduling and history.
- Alert dashboard.
- Document metadata registration and local file storage.
- CSV and JSON export.
- OCR for invoices.
- QR Code support for invoices.
- Encrypted backups.
- Optional synchronization.
- Family mode.
- Vehicle and property tracking.
- Estimated depreciation.
- Light and dark themes.
- Internationalization.

## Contributing

Contributions are welcome once the MVP foundation is stable. Keep changes focused, readable and covered by tests when they touch domain rules.

Before opening a pull request, make sure local databases, documents and environment files are not included.

## License

MIT License. See [LICENSE](LICENSE).
