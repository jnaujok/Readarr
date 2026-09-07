# Readarr diagrams

Architecture and behaviour of this Grok rewrite of Readarr (`.NET 10`, default metadata [rreading-glasses](https://github.com/blampe/rreading-glasses)). Labels match the English UI and `NzbDrone.*` types.

Operator procedures: [`docs/user-guide/index.html`](docs/user-guide/index.html).

## System context

```mermaid
flowchart LR
    Operator[Operator browser]
    Indexers[Usenet and torrent indexers]
    Clients[Download clients]
    Meta[rreading-glasses / Hardcover / self-hosted]
    Disk[Library on disk]
    Calibre[Calibre Content Server]
    Readarr[Readarr instance]

    Operator -->|HTTP 8787| Readarr
    Indexers -->|RSS / search| Readarr
    Readarr -->|grab NZB or torrent| Clients
    Clients -->|completed download| Readarr
    Meta -->|author and book metadata| Readarr
    Readarr -->|import / rename| Disk
    Readarr -->|optional add / convert| Calibre
```

## Runtime architecture

```mermaid
flowchart TB
    UI[frontend React SPA]
    API[Readarr.Api.V1]
    Host[NzbDrone.Host ASP.NET Core]
    Core[NzbDrone.Core]
    Store[Datastore SQLite or PostgreSQL]

    UI --> API
    API --> Host
    Host --> Core
    Core --> Store

    Core --> Books[Authors Books Editions]
    Core --> Media[Import rename tags]
    Core --> Index[Indexers download clients]
    Core --> Meta[Metadata proxy]
    Core --> Decision[Decision engine]
```

## Docker image

```mermaid
flowchart LR
    subgraph build [docker build]
        Node[node:20 webpack UI]
        SDK[dotnet/sdk:10.0 linux-x64]
        Node --> SDK
    end
    subgraph runtime [dotnet/aspnet:10.0]
        Apt[curl tzdata ca-certificates libsqlite3-0]
        App[/app Readarr.dll]
        Apt --> App
    end
    SDK -->|publish| runtime
    Cfg["/config"] --> App
```

`libsqlite3-0` is required because `System.Data.SQLite.Core.Servarr` loads `libsqlite3.so.0`.

## Domain model

```mermaid
erDiagram
    Author ||--o{ Book : writes
    Author }o--|| QualityProfile : uses
    Book ||--o{ Edition : has
    Edition ||--o{ BookFile : files
    Book }o--o{ QualityProfile : "inherits wanted formats"
    QualityProfile ||--o{ QualityItem : allows
    QualityItem }o--|| Quality : is

    Author {
        int Id
        int QualityProfileId
        bool Monitored
        string Path
    }
    Book {
        int Id
        int AuthorMetadataId
        bool Monitored
        bool AnyEditionOk
        list WantedFormatKinds
    }
    Edition {
        int Id
        int BookId
        bool Monitored
        string Isbn13
        string Asin
    }
    BookFile {
        int Id
        int EditionId
        string Path
        QualityModel Quality
        MediaInfoModel MediaInfo
    }
    QualityProfile {
        string Name
        bool UpgradeAllowed
        int Cutoff
        list WantedFormatKinds
    }
```

```mermaid
classDiagram
    class Author {
        +int Id
        +int QualityProfileId
        +bool Monitored
        +string Path
        +AddAuthorOptions AddOptions
    }
    class Book {
        +int Id
        +int AuthorMetadataId
        +bool Monitored
        +bool AnyEditionOk
        +List~BookFormatKind~ WantedFormatKinds
    }
    class Edition {
        +int Id
        +int BookId
        +bool Monitored
        +string Title
        +string Isbn13
        +string Asin
    }
    class BookFile {
        +int Id
        +int EditionId
        +string Path
        +QualityModel Quality
        +MediaInfoModel MediaInfo
        +int Part
    }
    class QualityProfile {
        +string Name
        +bool UpgradeAllowed
        +int Cutoff
        +List~QualityProfileQualityItem~ Items
        +List~BookFormatKind~ WantedFormatKinds
    }
    class Quality {
        +int Id
        +string Name
        +bool IsAudio
        +BookFormatKind FormatKind
    }
    class BookFormatPreference {
        +GetKind(Quality)* BookFormatKind
        +GetWantedKinds(Book, QualityProfile)* List
        +GetCollectedKinds(files)* HashSet
        +GetMissingKinds(Book, profile, files)* List
        +IsMissing(Book, profile, files)* bool
    }
    Author "1" --> "*" Book
    Book "1" --> "*" Edition
    Edition "1" --> "*" BookFile
    Author --> QualityProfile
    BookFile --> Quality
    Quality --> BookFormatKind
    BookFormatPreference ..> Book
    BookFormatPreference ..> QualityProfile
    BookFormatPreference ..> BookFile
```

## Format kinds

```mermaid
flowchart TB
    Q[Quality.Id]
    Q -->|PDF 1| Pdf[BookFormatKind.Pdf]
    Q -->|MOBI 2 EPUB 3 AZW3 4 Unknown 0| Ebook[BookFormatKind.Ebook]
    Q -->|MP3 10 FLAC 11 M4B 12 Unknown Audio| Audio[BookFormatKind.Audiobook]
```

An EPUB does not satisfy PDF or audiobook. Upgrades stay inside one kind (MOBI → EPUB → AZW3, or MP3 → M4B → FLAC).

## Wanted formats: who decides

```mermaid
flowchart TD
    Start[Resolve wanted kinds for a book]
    Start --> Override{Book.WantedFormatKinds set?}
    Override -->|yes| UseBook[Use the book list]
    Override -->|no| Prof{Profile.WantedFormatKinds empty?}
    Prof -->|yes| Legacy[Legacy: any allowed file completes the book]
    Prof -->|no| UseProf[Use the profile list]
    UseBook --> Missing
    UseProf --> Missing
    Legacy --> Files{Any BookFile?}
    Files -->|no| Incomplete[Missing]
    Files -->|yes| Complete[Complete]
    Missing[Wanted minus collected kinds]
    Missing --> Empty{Any missing kind?}
    Empty -->|yes| Incomplete
    Empty -->|no| Complete
```

UI: **Settings → Profiles → Wanted Formats** (`Ebooks (EPUB, AZW3, MOBI)`, `PDFs`, `Audiobooks`). Per book: **Override wanted formats for this book**.

## RSS / search grab

```mermaid
sequenceDiagram
    participant RSS as RSS or search
    participant Parse as Parser
    participant DE as Decision engine
    participant Q as QualityAllowedByProfile
    participant U as UpgradeDisk
    participant C as Cutoff
    participant DL as Download client

    RSS->>Parse: release title
    Parse->>DE: RemoteBook + Quality
    DE->>Q: is quality allowed?
    Q-->>DE: accept or reject
    Note over Q: If several listed formats are allowed, pick the best profile index
    DE->>U: existing files of the same BookFormatKind?
    alt no file of that kind
        U-->>DE: accept
    else file of that kind exists
        U-->>DE: accept only if upgradable
    end
    DE->>C: cutoff unmet for that kind?
    C-->>DE: accept or reject
    DE->>DL: grab
```

## Import and upgrade on disk

```mermaid
flowchart TD
    Completed[Download completed]
    Completed --> Identify[IdentificationService match edition]
    Identify --> Ambiguous{Equal distance different books?}
    Ambiguous -->|yes| Unmatched[Leave unmatched for manual import]
    Ambiguous -->|no| Import[ImportApprovedBooks]
    Import --> Same{Same book part and extension?}
    Same -->|yes skip| Skip[Skip duplicate]
    Same -->|no| Kind{Same format kind already on disk?}
    Kind -->|different kind| Keep[Keep both files]
    Kind -->|same kind| Replace{ShouldReplace quality?}
    Replace -->|yes| Swap[Replace file of that kind]
    Replace -->|no| Keep
    Keep --> Rename[FileNameBuilder path]
    Swap --> Rename
    Rename --> Omit{OmitAuthorFolderOnRename?}
    Omit -->|yes| Parent[Parent of author folder]
    Omit -->|no| AuthorDir[Author folder]
```

## Missing wanted formats

```mermaid
flowchart TD
    A[BooksWithoutFiles]
    A --> Zero[SQL: monitored released books with no files]
    A --> Extra[All monitored books plus files]
    Extra --> Pref[BookFormatPreference.IsMissing]
    Pref --> Merge[Merge unique books]
    Merge --> Sort[Order by Title then Id]
    Sort --> Page[Page for Wanted Missing / search]
```

## Add author

```mermaid
sequenceDiagram
    participant UI as Add New
    participant API as Author API
    participant Add as AddAuthorService
    participant Meta as Metadata proxy
    participant Refresh as RefreshAuthorService
    participant Mon as BookMonitoredService

    UI->>API: author + addOptions monitor AnyEditionOk
    API->>Add: AddAuthor
    Add->>Meta: GetAuthorInfo
    Add-->>API: author saved
    API->>Refresh: AuthorAdded
    Refresh->>Mon: SetBookMonitoredStatus
    Note over Mon: Apply Monitor plus AnyEditionOk to books
```

## Change monitored edition

```mermaid
sequenceDiagram
    participant UI as Select Edition
    participant Ed as EditionService
    participant Files as MediaFileRepository

    UI->>Ed: SetMonitored edition
    Ed->>Ed: mark that edition monitored
    Ed->>Files: SetEditionForBook bookId editionId
    Files-->>Ed: BookFile.EditionId updated
```

## Naming tokens

```mermaid
flowchart LR
    Template[StandardBookFormat]
    Template --> Regex[TitleRegex token optional customFormat optional fallback]
    Regex --> Handler{Token handler registered?}
    Handler -->|value| Use[Use token value]
    Handler -->|empty| FB{Fallback after pipe?}
    FB -->|yes| Fall[Use fallback text]
    FB -->|no| Empty[Empty string]
    Use --> Clean[Illegal character replacement]
    Fall --> Clean
```

Examples from the naming picker: `{Book Series|Standalone}`, `{Book TitleNoEdition}`, `{Book SeriesPosition:00}`, `{Isbn}`, `{Asin}`, `{Narrator}`.

## Unmapped files: Map Selected

```mermaid
flowchart TD
    Sel[Selected unmapped paths]
    Sel --> Common[getCommonFolder]
    Common --> Ancestor{Shared ancestor?}
    Ancestor -->|yes| Modal[InteractiveImportModal that folder]
    Ancestor -->|no different roots| Null[folder null: picker]
```

## First-run authentication

```mermaid
stateDiagram-v2
    [*] --> NewInstance
    NewInstance --> AuthModal: Authentication Required
    AuthModal --> Invalid: Authentication Method None
    Invalid --> AuthModal: Please select a valid authentication method
    AuthModal --> Saved: Save username and password
    Saved --> SignedIn: SPA
    SignedIn --> LoginPage: later visit
    LoginPage --> SignedIn: Login
```

The login page heading is **SIGN IN TO CONTINUE**. Fields: **Username**, **Password**, **Remember Me**, **Login**.

## CI and image publish

```mermaid
flowchart LR
    Push[push to develop]
    Push --> FE[Frontend lint webpack]
    Push --> BE[Backend tests Coverlet floor]
    Push --> SAST[SAST filesystem scan]
    FE --> Docker[Docker image]
    BE --> Docker
    Docker --> GHCR[ghcr.io/jnaujok/readarr:develop]
    Docker --> SHA[ghcr.io/jnaujok/readarr:sha-...]
    BE --> Pkg[linux-x64 package]
    FE --> Pkg
```

Coverlet: new slices locally ≥ 80%. CI fails only if merged whole-tree coverage is below `COVERAGE_FLOOR` (40%).
