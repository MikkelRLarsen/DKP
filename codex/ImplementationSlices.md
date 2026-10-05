# DKP – Implementation Slices

Dette dokument er projektets papirspor for den planlagte udviklingsrækkefølge. Hver slice skal implementeres, testes og dokumenteres, før den næste påbegyndes.

## Statusoversigt

| Slice | Funktion | Status |
|---|---|---|
| 1 | Foundation, login, users og characters | Færdig |
| 2 | DKP-transaktioner og egen historik | Færdig |
| 3 | Officer DKP Management | Færdig |
| 4 | Guild Members overview | Færdig |
| 4a | Main Character | Færdig |
| 5 | Player details | Færdig |
| 6 | Officer user administration | Færdig |
| 7 | Køb af Soft Reserves | Færdig |
| 8 | LootReserve copy-to-clipboard export | Færdig |
| 9 | Deployment og production hardening | Planlagt |

## Arkitektoniske regler

- Blazor afhænger kun af facade-kontrakter og IoC-registrering.
- Domain indeholder entities og domæneinvarianter uden UI- eller EF Core-afhængigheder.
- Application indeholder business logic og commands/use cases og implementerer facade-command interfaces.
- Facade indeholder kun DTO’er samt query- og command-kontrakter til Blazor.
- Infrastructure indeholder EF Core, PostgreSQL, migrations og implementerer facade-query interfaces samt Application persistence interfaces.
- `DKP.InversionOfControl` samler dependency injection, database og authentication.
- Read-only queries og state-changing commands holdes adskilt efter et CQRS-lignende mønster.
- Officer-funktioner skal beskyttes serverside; UI-skjulning er ikke tilstrækkelig.
- Alle databaseændringer leveres med en EF Core migration.
- Radzen anvendes som førstevalg til UI-komponenter.

Dependency-retningen for backend-kontrakter er:

```text
Blazor → Facade contracts
           ├─ queries → Infrastructure
           └─ commands → Application → Application persistence interfaces → Infrastructure
```

## Slice 1 – Foundation, login, users og characters

Status: Færdig.

Leveret:

- Discord OAuth2-login og logout.
- Automatisk oprettelse/opdatering af `User`.
- Member/Officer-rolle via konfigurerede Discord User IDs.
- Flere characters pr. bruger med `FirstName` og `LastName`.
- Dashboard med Discord-profil og characters.
- Opret, rediger og slet egne characters.
- PostgreSQL, EF Core, migrations og cold-start migration.
- Docker Compose med PostgreSQL og pgAdmin.
- Application, Facade, Infrastructure og IoC-lag.
- Unit tests for user provisioning, roller, characters og ejerskab.

## Slice 2 – DKP-transaktioner og egen historik

Status: Færdig.

Leveret:

- `DkpTransaction` med positive og negative heltalsbeløb.
- Balance beregnet som summen af brugerens transaktioner.
- Read-only DKP query- og facade-flow.
- Dashboard med beregnet saldo.
- `/my-dkp` med egen transaktionshistorik.
- EF Core relationer og migration `AddDkpTransactions`.
- Tests for saldo, sortering, brugerafgrænsning og tom historik.

### Mål

Gøre DKP-saldoen funktionel ved at modellere alle ændringer som transaktioner og vise brugerens egen historik.

### Scope

- Opret domain entity `DkpTransaction`.
- Brug heltalsbeløb med positive og negative værdier.
- Beregn saldo som `SUM(Amount)`; gem ikke en direkte saldo på `User`.
- Tilføj `CreatedByUserId` og `CreatedAtUtc` til audit-spor.
- Implementér read-only queries for saldo og egen historik.
- Opdater dashboardet til at vise den beregnede saldo.
- Implementér `/my-dkp` med RadzenDataGrid.
- Officerer skal ikke endnu kunne oprette transaktioner fra UI’et.

### Data

`DkpTransaction` skal indeholde:

- `Id`
- `UserId`
- `Amount` som `int`
- `Reason`
- `CreatedByUserId`
- `CreatedAtUtc`

Der skal være foreign keys til den berørte bruger og brugeren, der oprettede transaktionen. Negative saldi er tilladt.

### Kontrakter

Tilføj facade-DTO’er og query interfaces svarende til:

- `DkpTransactionDto`
- `DkpHistoryDto`
- `BalanceDto`
- `IDkpQueries`
- `IDkpTransactionQueries`

Blazor må ikke modtage domain entities eller bruge `DbContext` direkte.

### UI

`/my-dkp` viser:

- Aktuel saldo.
- Dato.
- Beløb.
- Årsag.
- Oprettet af.
- Loading, empty og error states.

Positive og negative beløb visualiseres med Radzen-komponenter.

### Acceptkriterier

- En brugers saldo beregnes korrekt ud fra alle transaktioner.
- Positive og negative transaktioner summeres korrekt.
- Historikken vises nyeste først.
- En bruger kan kun læse sin egen historik.
- Dashboardet viser den rigtige saldo i stedet for `0`.
- Ny EF Core migration er genereret.
- Unit- og infrastructure-tests består.

## Slice 3 – Officer DKP Management

Status: Færdig.

Leveret:

- Officer-only command-flow til Add DKP og Remove DKP.
- Serverside validering af Officer, beløb, årsag og target-bruger.
- Audit-spor via `CreatedByUserId` og `CreatedAtUtc`.
- Funktionel `/admin` med spiller-dropdown, confirmation, loading og notifications.
- Facade/query-flow til guild-brugere uden domain entities eller `DbContext` i Blazor.
- Tests for commands, authorization, validation, audit og opdateret saldo/historik.
- Ingen ny migration; den eksisterende `DkpTransactions`-tabel genbruges.
- Facade er reduceret til kontrakter; command implementations ligger i Application og query implementations i Infrastructure.

### Mål

Give Officer-brugere mulighed for at tilføje og fratrække DKP gennem en kontrolleret command-flow.

### Scope

- Add DKP.
- Remove DKP.
- Obligatorisk årsag.
- `CreatedByUserId` sættes fra den aktuelle officer.
- Negative saldi er fortsat tilladt.
- Officer authorization i application command og UI.
- Confirmation dialog, notifications og loading states.

### Kontrakter

- `IDkpTransactionCommands`
- `CreateDkpTransactionRequest`
- Facade-metoder for add/remove DKP.

### Acceptkriterier

- Officer kan oprette positive og negative transaktioner.
- Member afvises serverside.
- Tom årsag eller ugyldigt beløb afvises.
- Den korrekte officer gemmes som `CreatedByUserId`.
- Den nye saldo og historik vises efter gennemført transaktion.

## Slice 4 – Guild Members overview

Status: Færdig.

Leveret:

- `GuildMemberDto` og `IGuildMemberQueries` i Facade.
- `GuildMemberQueries` i Infrastructure med direkte EF Core projection.
- Funktionel `/members` for authenticated brugere.
- RadzenDataGrid med sorting, filtering, search og pagination.
- Discord-avatar, characters og beregnet DKP-balance.
- Brugere uden characters vises med saldo `0`.
- Tests for alle medlemmer, balance, characters og tomme character-lister.

### Mål

Give authenticated guild members en oversigt over guildens brugere, characters og DKP.

### Scope

- Query for alle brugere.
- Discord-navn og avatar.
- Characters.
- Aktuel DKP-balance.
- RadzenDataGrid med sorting, filtering, search og pagination.
- Ingen ændringsfunktionalitet.

### Acceptkriterier

- Alle relevante medlemmer vises.
- Balance beregnes pr. bruger.
- Brugere uden characters vises korrekt.
- Uauthenticated brugere afvises.
- Blazor modtager kun facade read-models/DTO’er.

## Slice 4a – Main Character

Status: Færdig.

Leveret:

- `Character.IsMain` i domain.
- `IsMain` i `CharacterDto` og dashboardets character-liste.
- `SetMainCharacterAsync` via Facade → Application → Infrastructure.
- Højst én main character pr. bruger via PostgreSQL unique filtered index.
- Dashboard med ⭐-markering og `Set as main`-handling.
- Migration `AddCharacterMainStatus`.
- Tests for valg, skift og ejerskab.

### Mål

Give hver bruger mulighed for at vælge én af sine characters som main character.

### Scope

- Tilføj main-status til `Character`.
- Højst én main character pr. bruger.
- Giv brugeren mulighed for at vælge main character fra dashboardet.
- Markér den valgte character tydeligt i UI’et.
- Flyt main-status ved valg af en ny character.
- Nulstil main-status hvis den valgte character slettes.
- Håndhæv ejerskab serverside gennem Application command logic.
- Tilføj EF Core migration og relevant unique constraint/index.

### Arkitektur

```text
Blazor dashboard
  → ICharacterCommands                 // Facade
  → CharacterCommandService             // Application
  → ICharacterRepository                // Application persistence
  → CharacterRepository                 // Infrastructure
  → DkpDbContext
```

### Kontrakter

Tilføj en command svarende til:

```csharp
Task<bool> SetMainCharacterAsync(
    string authenticatedDiscordId,
    Guid characterId,
    CancellationToken cancellationToken = default);
```

`CharacterDto` udvides med `IsMain`.

### Acceptkriterier

- En bruger kan vælge én af sine egne characters som main.
- En ny main character fjerner automatisk den tidligere main-status.
- En bruger kan ikke vælge en anden brugers character.
- En bruger kan ikke have mere end én main character.
- Main-status gemmes i databasen og bevares ved næste login.
- Sletning af main character efterlader brugeren uden main character.
- Dashboardet viser korrekt main-status efter ændringen.
- Migration, Application tests og Infrastructure tests består.

## Slice 5 – Player details

Status: Færdig.

Leveret:

- `PlayerDetailsDto` og `IPlayerDetailsQueries` i Facade.
- `PlayerDetailsQueries` i Infrastructure med profile, characters og DKP-historik.
- `/members/{userId}` for authenticated brugere.
- Discord-profil, main-markering, characters, beregnet DKP-balance og komplet historik.
- View-knap fra `/members`.
- Loading, empty, not-found og error states.
- Tests for detaljer, balance, characters, historik og ukendt bruger.

### Mål

Give brugere adgang til en detaljeret visning af en guildspiller.

### Scope

- Separat page eller RadzenDialog.
- Discord-information.
- Alle characters.
- Aktuel DKP.
- Komplet DKP-historik.
- Genbrug af eksisterende facade queries.

### Acceptkriterier

- Player details viser korrekt bruger, characters og transaktioner.
- Ukendt spiller håndteres som not found.
- Brugere kan ikke se domain entities eller databaseobjekter direkte.

## Slice 6 – Officer user administration

Status: Færdig.

Leveret:

- Officer-only brugeroversigt med Discord-navn, main character og rolle.
- `IUserAdministrationQueries` og `IUserRoleCommands` i Facade.
- Rolleændringer gennem Application business logic og Infrastructure persistence.
- Serverside Officer-authorization på role commands.
- Bootstrap-Officers fra `Discord:OfficerUserIds` kan ikke demoteres.
- Normale rolleændringer bevares ved efterfølgende Discord-login.
- Confirmation, loading, notifications og opdatering af brugerlisten i Administration.
- Administration er opdelt i `/admin`, `/admin/dkp` og `/admin/users` med en hierarkisk Radzen-menu.
- Tests for rolleændring, authorization, bootstrap-beskyttelse og provisioning.

### Mål

Give Officer-brugere mulighed for at administrere Member/Officer-roller.

### Scope

- Liste over brugere.
- Vis rolle.
- Skift rolle.
- Serverside Officer authorization.
- Beskyt bootstrap-officers fra utilsigtet fjernelse.

### Acceptkriterier

- Kun Officer kan ændre roller.
- Member kan ikke ændre egen eller andres rolle.
- Rolleændring anvendes ved næste login.
- Bootstrap-officers kan fortsat logge ind som Officer.

## Slice 7 – Køb af Soft Reserves

Status: Færdig.

### Mål

Give brugere mulighed for at købe ekstra Soft Reserves med DKP.

### Scope

- Domain entity `SoftReservePurchase`.
- Konfigurerbar pris og maksimum antal via `SoftReserve:DkpCost` og `SoftReserve:MaxReserves`.
- Balance-, maksimums- og duplicate-validering serverside.
- Atomisk oprettelse af purchase og negativ DKP-transaktion.
- Confirmation dialog og brugerflow på `/dkp-shop`.

`SoftReservePurchase` indeholder:

- `Id`
- `UserId`
- `Quantity`
- `CancelledAtUtc`
- `DkpCost`
- `CreatedAtUtc`

### Acceptkriterier

- Gyldigt køb opretter purchase og negativ DKP-transaktion atomisk.
- Utilstrækkelig saldo afvises.
- Maksimum samlet antal og positiv quantity håndhæves.
- Annullerede køb kan ikke annulleres igen.
- Ny EF Core migration er genereret.
- Brugerens saldo og Soft Reserve-liste opdateres efter køb.

Leveret:

- `ISoftReserveCommands` og `ISoftReserveQueries` i Facade.
- Application command med authenticated user, balancekontrol og atomic persistence.
- Infrastructure mapping, repositories, query og migrationerne `AddSoftReservePurchases` og `AddSoftReserveQuantityAndCancellation`.
- Konfiguration med defaults: 10 DKP pr. reserve og maksimalt 2 reserves.
- `/dkp-shop` med produkt-dropdown, quantity, confirmation, købshistorik og annullering.
- Tests for køb, refundering, quantity, saldo, brugerafgrænsning og query.

## Slice 8 – LootReserve copy-to-clipboard export

Status: Færdig.

### Mål

Generere en kopiérbar CSV-formateret tekstliste til LootReserve uden fil-download eller direkte addon-integration.

### Scope

- Officer-only `/admin/loot-reserve` under Administration.
- RadzenDataGrid med alle medlemmer, character-valg og Main Character som default.
- Midlertidig override af ReserveLimit og RollBonus.
- Visuel strikethrough, hvor fravalgte medlemmer udelades fra outputtet.
- Output i `RadzenTextArea` med `FirstName,LastName,ReserveLimit,RollBonus`.
- Copy-to-clipboard via browser Clipboard API; ingen CSV-fil downloades.
- Default ReserveLimit gemmes i guild settings.
- RollBonus-tiers seedes som shop-items med én aktiv bonus pr. bruger.

### Acceptkriterier

- Teksten har korrekt header og stabil rækkefølge.
- Specialtegn håndteres korrekt som CSV-tekst.
- Kun Officer kan hente data og ændre settings.
- Gennemstregede og ikke-klare medlemmer udelades.
- Teksten kan kopieres og indsættes direkte i addon’et.
- Ingen direkte kommunikation med World of Warcraft eller LootReserve.

Migration: `20261005172857_Slice8LootReserve`.

## Slice 9 – Deployment og production hardening

### Mål

Gøre systemet klar til en samlet deployment.

### Scope

- Dockerfile for Blazor-applikationen.
- Docker Compose med app, PostgreSQL og valgfri pgAdmin.
- Environment-based configuration.
- Production Discord redirect URI.
- Database health check.
- Logging og kontrolleret fejlhåndtering.
- Dokumentation af lokal opstart, migrationer, Discord-konfiguration og deployment.
- Ingen secrets i repository.

## Teststrategi for alle slices

Hver slice skal som minimum have:

- Application unit tests.
- Infrastructure/EF Core tests ved model- eller migrationændringer.
- Authorization tests for Member/Officer-grænser.
- Facade-kontrakt tests hvor nye DTO’er eller interfaces introduceres.
- Build og test af hele solutionen.
- Manuel smoke-test af den brugerrejse, slicen leverer.

## Beslutninger og antagelser

- DKP-beløb modelleres som `int`.
- Negative saldi er tilladt.
- Slice 2 indeholder kun brugerens egen historik.
- Officer add/remove kommer i Slice 3.
- LootReserve har ingen direkte integration til World of Warcraft.
- LootReserve-exporten er tekstbaseret og downloader ikke filer.
- Dokumentet skal opdateres med status, migrations og kendte begrænsninger, når hver slice implementeres.
