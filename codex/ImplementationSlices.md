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
| 8a | Event-sourced DKP ledger og shop-køb | Færdig |
| 8b | Mine aktive køb og forbrug | Færdig |
| 9 | Shop catalog og shop-item administration | Færdig |
| 10 | Admin shop-overview og køb for brugere | Færdig |
| 11 | Blokering af medlemmer | Planlagt |
| 12 | DKP management presets | Delvist implementeret |
| 13 | DKP acquisition overview | Planlagt |
| 13a | Achievement-baserede DKP awards | Planlagt |
| 13b | Achievement-gated shop-items | Planlagt |
| 14 | Deployment og production hardening | Planlagt |

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

## Slice 8a – Event-sourced DKP ledger og shop-køb

Status: Færdig.

### Mål

Gøre event store til source of truth for DKP-balance, DKP-hændelser, shop-køb, annulleringer og refunderinger. Balance og aktive item-quantity beregnes fra immutable events og vises gennem genopbyggelige read projections.

### Scope

- Append-only typed events for `DkpCredited`, `DkpDebited`, `ShopPurchaseCreated` og `ShopPurchaseCancelled`.
- Atomisk event append og projection-opdatering i samme PostgreSQL-transaktion.
- Balance projection som summen af credit/debit events.
- Shop purchase projection til historik, aktive køb og `MaxPerUser`-validering.
- Database lock under saldo- og item-limit-validering.
- Annullering som modgående credit event; dobbelt-refundering afvises.
- RollBonus med maksimalt ét aktivt køb pr. bruger.
- Projection rebuild/replay uden at skabe nye events.
- Eksisterende tabeller udfases som source of truth; databasen er tom, så historiske records migreres ikke.

### Acceptkriterier

- Nye DKP- og shop-commands skriver events og ikke gamle transaction-/purchase-records.
- Dashboard, `/my-dkp`, `/dkp-shop` og LootReserve læser event-baserede projections.
- Saldo, refunderinger og item limits beregnes korrekt efter replay.
- Samtidige køb kan ikke overskride saldo eller `MaxPerUser`.
- Event store er immutable og har aggregate sequence/correlation-id beskyttelse.
- Build, tests og EF migration passerer.

Migration: `20261005175846_Slice8aEventSourcedLedger`.

Den generiske shop- og DKP-command path skriver nu immutable events og opdaterer projections atomisk. De tidligere tabeller findes fortsat i EF-modellen som kompatibilitetslag for eksisterende tests/legacy contracts, men bruges ikke af det nye UI-flow som source of truth.

## Slice 8b – Mine aktive køb og forbrug

Status: Færdig.

### Mål

Give brugeren et tydeligt overblik over aktive køb, anvendt quantity og resterende muligheder.

### UI

Tilføj `/my-purchases` og gerne et kort på dashboardet.

Faste produktoversigter skal være lette at aflæse:

```text
SoftReserve: 0 / 2
RollBonus: 30
```

Hvis brugeren ikke har et aktivt RollBonus-køb, vises `RollBonus: 0`.

Andre shop-items vises dynamisk med item-navn, aktuel quantity, maksimum og resterende quantity.

Eksempel:

```text
SoftReserve       0 / 2
RollBonus         30
Guild Token       2 / 5
Raid Entry        1 / 1
```

### Dataflow og kontrakter

```text
Blazor
  → Facade query contract
  → Infrastructure query
  → ShopPurchaseProjections + ShopItems
  → ActivePurchaseOverviewDto
```

Tilføj `IShopPurchaseQueries`, `ActivePurchaseOverviewDto` og `ActivePurchaseItemDto` i Facade. DTO’erne skal indeholde item key, navn, quantity, maksimum, resterende quantity, pris og eventuel RollBonusValue.

Queryen afgrænses til den authenticated bruger, ignorerer annullerede køb, summerer flere aktive køb af samme item og bruger event projections som source of truth. Blazor må ikke se domain entities, projections eller `DbContext`.

Den eksisterende købshistorik på DKP Shop bevares med dato, item, quantity, pris og status. Annullering sker fortsat gennem det eksisterende command-flow.

### Tests og acceptkriterier

- SoftReserve vises som `0 / 2`, `1 / 2` eller `2 / 2`.
- RollBonus vises som den aktive værdi eller `0`.
- Dynamiske items viser quantity og maksimum.
- Flere køb af samme item summeres korrekt.
- Annullerede køb tæller ikke som aktive.
- Brugere kan kun se egne aktive køb.
- Empty state og authorization håndteres korrekt.
- Queryen læser event projections og ikke legacy purchase-tabeller.

## Slice 9 – Shop catalog og shop-item administration

Status: Færdig.

Leveret:

- Officer-only `/admin/shop` med Radzen-katalog.
- Opret shop-item med key, navn, beskrivelse, pris og maksimum pr. bruger.
- Redigér navn, beskrivelse, pris og maksimum; key er stabil efter oprettelse.
- Aktivér/deaktivér items med confirmation dialog.
- Historiske køb forbliver bevaret, og items slettes ikke fysisk.
- Shop Catalog er tilgængelig under Administration-menuen og admin-overblikket.

Den generiske `ShopItem`-model, shop queries, commands og migration var allerede etableret og genbruges af UI’et.

Shop-items indeholder `Id`, `Key`, `Name`, `Description`, `Price`, `MaxPerUser`, `IsActive`, `CreatedAtUtc` og `UpdatedAtUtc`. Soft Reserve er item med key `soft-reserve`. Historiske køb beholder den oprindelige pris, og items med køb slettes ikke fysisk.

## Slice 10 – Admin shop-overview og køb for brugere

Status: Færdig.

Leveret:

- Officer-only `/admin/shop/purchases`.
- Oversigt over alle køb med bruger, main character, item, quantity, DKP, dato og status.
- Radzen sorting, filtering, search og pagination.
- Annullering af aktive køb med automatisk refundering.
- Multi-select af guild-medlemmer til køb på vegne af flere brugere.
- Validering og atomisk multi-user purchase-flow i Application/event-sourcing-laget.
- Officer authorization både i UI og query-flow.
- Almindelige brugere kan kun hente egne køb; admin-queryen kræver Officer serverside.

Multi-user køb valideres samlet og gennemføres atomisk. Hver bruger får sin egen purchase-record og DKP-event, og Officer gemmes som actor.

## Slice 11 – Blokering af medlemmer

Status: Planlagt.

Tilføj `IsBlocked`, `BlockedAtUtc`, `BlockedByUserId` og valgfri årsag. Blokerede brugere må ikke logge ind, købe, modtage admin-køb eller anvende DKP-presets. Bootstrap-Officers beskyttes, og eksisterende sessioner afvises ved næste cookie-validering. Alle regler håndhæves i Application.

## Slice 12 – DKP management presets

Status: Delvist implementeret.

`DkpAwardPreset` og preset commands/queries findes delvist. Slicen færdiggøres med Officer UI til CRUD, aktivering/deaktivering, preset-dropdown i DKP Management, usage-visning og livstidsgrænse pr. bruger.

Preset-apply opretter usage-record og DKP-event atomisk. Deaktiverede presets kan ikke anvendes, og eksisterende events ændres ikke ved redigering.

## Slice 13 – DKP acquisition overview

Status: Planlagt.

Tilføj `/my-dkp/sources` med aktive DKP-presets, beløb, årsag, anvendelser, maksimum og resterende muligheder. Presets, hvor brugeren har nået maksimum, skjules. Oversigten er informativ; kun Officers kan anvende presets.

## Slice 13a – Achievement-baserede DKP awards

Status: Planlagt.

Achievements registreres manuelt af Officers og knyttes til brugeren på guild-systemniveau, ikke til en character. En Officer kan oprette achievement definitions med key, navn, beskrivelse og DKP-belønning samt give eller fjerne et achievement for flere valgte brugere på én gang.

Tildeling opretter et positivt DKP-event. Fjernelse opretter et modgående DKP-event med det oprindelige beløb, så event store og saldo forbliver konsistente. Grant/revoke-historik bevares, og et revoked achievement kan gives igen. Members og blokerede brugere kan ikke anvende flowet.

Planlagte modeller:

- `AchievementDefinition`: `Id`, `Key`, `Name`, `Description`, `DkpAmount`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`.
- `UserAchievement`: bruger, achievement, grant-audit og revoke-audit med timestamps og Officer IDs.

Der skal være Officer-only UI, Facade-kontrakter, Application commands, Infrastructure persistence, migration og tests for authorization, multi-user atomicitet, duplicate grants, revoke/refund og re-grant.

## Slice 13b – Achievement-gated shop-items

Status: Planlagt.

Shop-items kan valgfrit kræve ét eller flere achievements. Hvis flere krav er sat, skal brugeren have alle achievements. Ingen krav betyder, at item’et fungerer som normalt.

Tilføj en relation mellem shop-items og achievement definitions. Shop administration skal kunne vælge krav, og shop query/UI skal vise eventuelle manglende krav. Købsvalideringen håndhæves altid serverside, også ved Officer-køb på vegne af brugere. Multi-user køb fejler atomisk, hvis én target-bruger mangler et krav.

Kravændringer påvirker kun fremtidige køb. Historiske køb og DKP-events ændres ikke. Der skal tilføjes Facade-kontrakter, Application-/Infrastructure-flow, EF migration og tests for AND-logik, manglende/deaktiverede achievements, multi-user atomicitet og historisk databevarelse.

## Slice 14 – Deployment og production hardening

Status: Planlagt.

Gør systemet deploymentklart med Dockerfile, Docker Compose for app/PostgreSQL/pgAdmin, production Discord redirect URI, environment-based configuration, database health checks, logging, kontrolleret fejlhåndtering, migration-/backup-dokumentation og kontrol af secrets.

Deployment-testen skal dække shop, refundering, blokering og DKP presets.

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
