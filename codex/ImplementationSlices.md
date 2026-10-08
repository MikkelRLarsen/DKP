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
| 11 | Guild membership ved OAuth og blokering af medlemmer | Færdig |
| 12 | DKP management presets | Færdig |
| 12a | Legacy cleanup og event-konsolidering | Implementeret; event-store uden persisted projections |
| 13 | DKP acquisition overview | Færdig |
| 13a | Achievement-baserede DKP awards | Færdig |
| 13b | Achievement-gated shop-items | Planlagt |
| 13c | DKP-anmodninger fra Sources | Færdig |
| 13d | Consume LootReserve og revert seneste batch | Færdig |
| 13e | LootReserve modifiers | Færdig |
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

## Slice 11 – Guild membership ved OAuth og blokering af medlemmer

Status: Færdig.

Slice 11 implementeres uden Discord bot.

OAuth-flowet udvides med Discord `guilds`-scope. Ved login hentes brugerens guild-liste via `/users/@me/guilds`, og systemet kontrollerer den konfigurerede `Discord:GuildId`. Brugere, der ikke er medlem af guilden, afvises før user provisioning.

Tilføj `IsBlocked`, `BlockedAtUtc`, `BlockedByUserId` og valgfri årsag. Blokerede brugere må ikke logge ind, købe, modtage admin-køb eller anvende DKP-presets. Bootstrap-Officers beskyttes, og alle regler håndhæves i Application.

Uden bot kontrolleres guild-medlemskab ved OAuth-login. Kontinuerlig kontrol af, om en allerede aktiv bruger senere forlader guilden, hører til backlog-punktet for Discord bot security integration.

Leveret:

- OAuth `guilds`-scope og kontrol mod `Discord:GuildId` ved Discord callback.
- Brugere uden medlemskab af den konfigurerede guild provisioneres ikke.
- `IUserBlockCommands` og Application command service til block/unblock.
- Valgfri blokeringsårsag med maksimum 500 tegn.
- Bootstrap-Officers kan ikke blokeres.
- Blokerede brugere afvises ved login og ved cookie-validering.
- Blokerede brugere er fortsat afskåret fra shop- og DKP-handlinger gennem eksisterende serverside-validering.
- User Administration viser block-status, årsag og block/unblock-handlinger.
- Eksisterende User-felter og migration genbruges; ingen ny migration var nødvendig.

## Backlog – Discord guild security og OAuth callback

Status: Potentielt fremtidigt tiltag.

### Kendt problem: OAuth callback ved forkert bruger

Når en Discord-bruger, som ikke er medlem af den konfigurerede guild, trykker **Authorize**, kan Discord OAuth-flowet i lokal udvikling blive hængende på eller genindlæse OAuth 2.0-siden i stedet for stabilt at navigere til `/account/access-denied?reason=guild`. Cancel-flowet fungerer, men Authorize-flowet er ikke stabilt nok og skal undersøges særskilt.

Mulige årsager, der skal undersøges:

- ASP.NET Core correlation-cookie/state mellem Discord og localhost.
- Discords callback-adfærd ved `guilds`-scope og afvisning i `OnCreatingTicket`.
- Samspillet mellem HTTPS-dev-certifikat, flere localhost-redirects og browserens cookie-politik.
- Om guild-valideringen bør flyttes til et separat callback-/ticket-trin.

Acceptkriteriet for en fremtidig rettelse er, at en forkert Discord-bruger altid lander på en tydelig 401/access-denied-side uden exception, redirect-loop eller genindlæsning af Discord OAuth-siden.

### Planlagt Discord bot-integration

Hvis der senere er behov for løbende kontrol af guild-medlemskab, kan systemet udvides med en Discord bot integration. Botten skal være medlem af guilden og bruge konfiguration for `Discord:BotToken` og `Discord:GuildId`.

Botten kan bruges til serverside medlemskontrol ved login og cookie-validering via Discords guild-member endpoint. Brugere, der forlader guilden, kan dermed miste adgang ved næste cookie-validering. Manglende eller ugyldig bot-konfiguration skal give en tydelig konfigurationsfejl.

Bot-token må kun komme fra User Secrets eller deployment environment variables og må aldrig commit’es.

Mulige tests:

- Guild-member success/not-found.
- Discord API-fejl.
- Cookie-validering.
- Manglende eller ugyldig bot-konfiguration.
- Forsøg på at omgå guild-kontrollen.

## Slice 12 – DKP management presets

Status: Færdig.

`DkpAwardPreset` og preset commands/queries er færdigimplementeret med Officer UI til CRUD, aktivering/deaktivering, preset-dropdown i DKP Management, usage-visning og livstidsgrænse pr. bruger.

Preset-apply opretter usage-record og DKP-event atomisk. Deaktiverede presets kan ikke anvendes, og eksisterende events ændres ikke ved redigering.

Leveret:

- Officer-only `/admin/dkp-presets` med opret, redigér og aktivér/deaktivér.
- Validering af navn, positivt beløb, årsag og maksimum på 500 tegn.
- Preset-dropdown i `/admin/dkp` med mulighed for fortsat custom amount/reason.
- Presets kan kun bruges til Add DKP; Remove DKP forbliver custom.
- Usage-query viser anvendelser, maksimum og resterende anvendelser for den valgte bruger.
- Lifetime-limit håndhæves serverside og atomisk sammen med DKP-event og usage-record.
- Deaktiverede presets vises ikke som valgmulighed til nye awards.
- Officer authorization håndhæves både på UI, query og commands.

## Slice 12a – Legacy cleanup og event-konsolidering

Status: Implementeret og automatiseret verificeret 2026-10-06. Baseline: funktionalitet til og med Slice 12. DKP-, shop-, preset- og aktivitetsstate beregnes nu ved load fra `DkpEvents`; der gemmes ingen persisted projections. Manuel browser-/Discord-gennemgang er fortsat et afleveringscheck; se begrænsninger nedenfor.

Godkendt plan:

- Brugeren nulstiller selv databasen. Erstat migrationskæden med én initial migration til tom PostgreSQL; ingen automatisk sletning af den eksisterende database.
- Fjern legacy transaction/purchase entities, repositories, SoftReserve commands/queries, konfiguration, DI og fallback-læsninger. Tidligere slice-beskrivelser ovenfor er historisk papirspor og erstattes teknisk af denne slice.
- Typed, versionsmærkede events er eneste kilde til balance, køb, refundering og preset-forbrug. Ret event-id/correlation-id, og forbind preset-projections med faktiske events.
- Samme projector anvendes live og ved atomisk replay. Commands bruger friske contexts, fælles transaktionsgrænse og brugerlåse; event-store er append-only.
- Shop Catalog styrer pris/maksimum, også Soft Reserve (seed 10 DKP / 2). Seed RollBonus 10/20/30/40 med pris 10/30/60/120 og DefaultReserveLimit 0. Historiske køb snapshotter pris, navn og bonus.
- Facade bliver rent kontraktlag uden Domain-reference. Actor kommer fra authenticated current-user-kontrakt; server-side rolle, blokering og ejerskab håndhæves.
- Ret dashboard, Guild Members, Player Details, My DKP, shop, aktive køb og LootReserve til fælles event-baserede projections. Bevar multiselect, main character og preset-forbrug pr. spiller.
- Tilføj /activity for aktive medlemmer med spiller-, handlings- og datofiltre, server-pagination og én række pr. handling/spiller.
- Fjern skabelonsider/ubrugte dependencies. Omskriv legacy-tests og tilføj PostgreSQL-tests for rollback, concurrency, replay, migrations og authorization samt arkitekturkontrol.
- Aflever først efter build og relevante tests; dokumentér smoke-test og konkrete begrænsninger. OAuth-loop og bot forbliver backlog; Slice 13+ er ikke del af oprydningen.

Fund: Guild/Player queries læser legacy; blandede fallback-kilder; forkert event-id i bulk; preset uden event-reference; ikke-atomisk replay; forskellige SoftReserve-priskilder; Facade afhænger af Domain; nuværende 32 tests dækker primært legacy/InMemory.

### Leveret

- De ovenstående fund er rettet. Legacy-entities, repositories, SoftReserve-konfiguration og fallback-queries er fjernet. Historiske beskrivelser af `DkpTransaction`, `SoftReservePurchase`, `ShopPurchase`, `User.RollBonus` og gamle migrationer beskriver ikke længere den aktuelle løsning.
- `DkpEvents` bruger version 1 med typed payloads: `DkpPosted`, `PurchasePlaced` og `PurchaseCancelled`. Køb/debitering er én hændelse; annullering/refundering er én hændelse. Preset-id gemmes i DKP-eventet.
- Balance, purchase-state, lifetime preset-usage og historik/aktivitet er genopbyggelige projections. Faktiske event-id'er anvendes overalt; bulk-operationer deler et operations-/correlation-id.
- `LedgerProjector` anvendes ved både append og replay. Replay sorterer efter aggregate/sequence og afbryder atomisk ved ugyldige events. Ingen nye events skrives under replay.
- `CommandUnitOfWork` opretter en frisk context/transaction pr. operation. Alle writes og replay koordineres med én PostgreSQL advisory transaction lock samt sorterede target-brugerlåse. Rollback efterlader ingen context, som næste command genbruger. Der foretages ikke automatisk genforsøg efter fejl eller usikkert commit-resultat.
- EF-afvisning af event-ændringer/-sletninger suppleres af en PostgreSQL-trigger, så set-baserede UPDATE/DELETE heller ikke kan omskrive ledgeren.
- Facade er uden Domain/EF-reference. `ICurrentUser` får identitet fra serverens `AuthenticationStateProvider`; alle commands og queries kontrollerer aktiv bruger i databasen. Officer-rolle, blokering og karakter-/købsejerskab kontrolleres serverside.
- Alle saldo-/historik-/købsoversigter anvender samme projections. Shop Catalog er eneste kilde til aktuelle priser og item-id-baserede maksimum. Historiske købsnavne/priser/bonus kommer fra events.
- `/activity` har server-pagination, stabil sortering, spiller-/handlings-/UTC-datofiltre, links til spiller og loading/empty/error states.
- DKP Management bevarer multiselect, Discord/main-character-navn og preset-forbrug pr. valgt spiller. Mutationer har dobbeltklik-guard; fejl ved refresh efter et gemt command vises som refresh-advarsel, ikke som fejlet lagring.
- Counter/Weather, ubrugte package-versioner og EF Design-reference i Blazor er fjernet. `AddMigration.ps1` anvender nu Infrastructure som både target og design-time startup; webhost og Discord-secrets kræves ikke til generering.

### Database og opstart

Den eneste migration er nu `20261006164533_InitialEventLedger`. Den indeholder alle tabeller/projections og deterministiske seeds: Soft Reserve 10 DKP / maksimum 2, RollBonus 10/20/30/40 til 10/30/60/120 DKP, DefaultReserveLimit 0.

**Breaking change:** Denne baseline kræver en tom database. Der findes ingen datakonvertering fra tidligere slices. Tag backup af ønskede data, og nulstil selv databasen eller peg `ConnectionStrings:DefaultConnection` på en ny tom database. Implementeringen har ikke nulstillet den eksisterende applikationsdatabase. Cold-start-migrering bevares og giver en tydelig fejl, hvis gamle migrationer registreres.

Fremtidige migrationer:

```powershell
.\AddMigration.ps1 -m NavnPåÆndring
dotnet ef migrations has-pending-model-changes --project src/DKP.Infrastructure --startup-project src/DKP.Infrastructure
```

De slettede legacy-filer/migrationer kan genfindes i Git-historikken.

### Verifikation

- `dotnet build DKP.slnx -p:UseAppHost=false`: solution verificeres sammen med test-build.
- `dotnet test DKP.slnx -p:UseAppHost=false`: **43 bestået, 0 fejlet, 0 sprunget over**.
- PostgreSQL-tests migrerer en ny isoleret `dkp_slice12a_<guid>`-database pr. testcase og sletter kun denne igen. Standard er den lokale Compose PostgreSQL; alternativ admin-forbindelse kan angives i miljøvariablen `DKP_TEST_ADMIN_CONNECTION` (kræver CREATE DATABASE). Tests må ikke erstattes med EF InMemory til concurrency-/rollback-kontrol.
- Dækning: konsistente saldoer/historik, event-referencer, negative korrektioner, bulk-deduplikering/rollback, lifetime presets, samtidig køb/preset, katalogpris/maksimum, RollBonus, original refundering én gang, gentaget/fejlende replay og replay samtidig med køb.
- Authorization: Member/Officer/anonymous/blocked, bootstrap-beskyttelse, eksisterende cookie efter blokering, provisioning, karakterejerskab og main-skift.
- Activity: én række pr. køb/refundering, filtre og stabil pagination. Arkitekturkontrol verificerer lagafhængigheder og kontrakter uden actor-argumenter.
- Automatisk host-smoke med fake authentication renderer dashboard, members, player details, My DKP, DKP Shop, Mine køb, Guild Activity og alle berørte administrationssider. Member får 403 til Officer-routes; anonym får 401 til activity. Test-loginhandleren findes kun i testprojektet.
- Initial migration anvendt mod tom PostgreSQL; `has-pending-model-changes` viser ingen ændringer. `AddMigration.ps1` er brugt til at generere baselinen.

### Kendte begrænsninger og resterende manuelle checks

- Host-smoke tester server-rendering og authorization, ikke en browsers JavaScript/SignalR-interaktion. Gennemgå efter eget database-reset: login, DKP multiselect/confirmation, køb/annullering, aktivitetens paging/filtre og clipboard. Rigtigt Discord-login og addon-paste er ikke udført i denne verifikation.
- Den fælles advisory lock serialiserer writes for guilden; det er et bevidst korrekthedsvalg. Ved større trafik kan låsegranularitet optimeres med tilsvarende PostgreSQL-concurrency-tests.
- Replay er en intern Application persistence-kontrakt, ikke et offentligt endpoint eller en ny administrationsside. Den læser eventlisten i hukommelsen; streaming/batching er et senere skaleringsbehov.
- `DkpBalanceProjection`, `ShopPurchaseProjection`, `LedgerEntryProjection` og `DkpAwardPresetApplication` er fjernet fra den aktive model. `RemovePersistedProjections` fjerner de tidligere tabeller; event-store er den eneste autoritative kilde.
- Discord OAuth-loopet og bot-sikkerhedsintegrationen forbliver de kendte backlog-punkter. Slice 13, 13a, 13b og 14 er ikke implementeret her.

## Slice 13 – DKP acquisition overview

Status: Færdig.

Leveret:

- `/my-dkp/sources` med navnet **Ways to Earn DKP** for authenticated brugere.
- Viser aktive DKP-presets med beløb, årsag, anvendelser, maksimum og resterende muligheder.
- Presets med opbrugt lifetime-limit eller som er deaktiverede vises ikke.
- Usage beregnes fra event-store replay via den eksisterende preset-query; der gemmes ingen ny projection.
- Siden er informativ, og Members kan ikke selv anvende eller claime DKP.
- Tilføjet navigation under My DKP samt host smoke-test og query-test for filtrering og remaining count.

## Slice 13a – Achievement-baserede DKP awards

Status: Færdig.

Achievements registreres manuelt af Officers og knyttes til brugeren på guild-systemniveau, ikke til en character. En Officer kan oprette achievement definitions med key, navn, beskrivelse og DKP-belønning samt give eller fjerne et achievement for flere valgte brugere på én gang.

Tildeling opretter et positivt DKP-event. Fjernelse opretter et modgående DKP-event med det oprindelige beløb, så event store og saldo forbliver konsistente. Grant/revoke-historik bevares, og et revoked achievement kan gives igen. Members og blokerede brugere kan ikke anvende flowet.

Planlagte modeller:

- `AchievementDefinition`: `Id`, `Key`, `Name`, `Description`, `DkpAmount`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`.
- `UserAchievement`: bruger, achievement, grant-audit og revoke-audit med timestamps og Officer IDs.

Leveret:

- Officer-only `/admin/achievements` med definition CRUD, aktivering/deaktivering, multi-select grant og revoke.
- Authenticated members kan se egne aktive achievements og achievement-historik på `/my-achievements`, og navigationen indeholder links til både member- og Officer-siden.
- Achievement-definitioner og brugergrant gemmes som almindelige entities med grant/revoke-audit og eventreferencer.
- Grant opretter et positivt `DkpPosted`-event med `AchievementId`; revoke opretter et modgående negativt event.
- Members kan ikke oprette, tildele eller revoke achievements; blokering og target-validering håndhæves i Application.
- En aktiv achievement kan ikke tildeles samme bruger to gange. Efter revoke kan den tildeles igen.
- Facade-kontrakter, Application commands, Infrastructure repository/query og IoC-registrering er tilføjet.
- Migrationen `Slice13aAchievements` er genereret.

Kendte begrænsninger:

- Achievement-events er ikke selv en del af DKP acquisition sources endnu; det hører til en senere udvidelse.
- Grant/revoke-history vises på Officer-siden, mens almindelige brugeroversigter fortsat fokuserer på DKP-balance og historik.

## Slice 13b – Achievement-gated shop-items

Status: Implementeret.

Shop-items kan valgfrit kræve ét eller flere achievements. Hvis flere krav er sat, skal brugeren have alle achievements. Ingen krav betyder, at item’et fungerer som normalt.

Tilføj en relation mellem shop-items og achievement definitions. Shop administration skal kunne vælge krav, og shop query/UI skal vise eventuelle manglende krav. Købsvalideringen håndhæves altid serverside, også ved Officer-køb på vegne af brugere. Multi-user køb fejler atomisk, hvis én target-bruger mangler et krav.

Kravændringer påvirker kun fremtidige køb. Historiske køb og DKP-events ændres ikke. Shop-administrationen kan vælge flere krav, shoppen viser manglende krav, og Application-valideringen håndhæver AND-logik for både medlemskøb og atomiske multi-user officer-køb. Migrationen `Slice13bAchievementGatedShopItems` opretter relationstabellen.

## Slice 13c – DKP-anmodninger fra Sources

Status: Færdig.

Leveret:

- Members kan oprette én pending DKP-anmodning pr. aktivt preset fra `/my-dkp/sources`.
- Members kan vælge quantity; quantity bruger samme antal lifetime-applications, og godkendelse opretter ét DKP-event pr. anvendelse.
- Members kan tilføje valgfri kommentar, se egne request-statusser og annullere egne pending requests.
- Officers kan se alle requests på `/admin/dkp-requests` og godkende eller afvise dem.
- Godkendelse genvaliderer preset, blokering og lifetime-limit under command-locken og opretter ét `DkpPosted`-event med Officer som actor.
- Rejected og cancelled requests ændrer ikke saldo eller preset usage.
- Pending-request uniqueness, ejerskab, authorization og kommentargrænser håndhæves serverside.
- Request-state gemmes som workflow-data; DKP-balance og usage kommer fortsat fra event-store replay.
- Migrationerne `20261006182056_Slice13cDkpAwardRequests` og `20261006182802_Slice13cRequestQuantity` tilføjer request-tabellen, quantity, event-reference-data, relationer og unique pending-index.
- Tests dækker create/cancel, approve/reject, authorization, duplicate requests, limit recheck, usage og host-routes.

## Slice 13d – Consume LootReserve og revert seneste batch

Status: Implementeret.

Leveret:

- Officer kan efter `Generate text` previewe og consume aktive SoftReserves og RollBonus for alle ikke-strikede, export-klare spillere.
- Consume bruger event-store state og ignorerer midlertidige ReserveLimit/RollBonus-overrides fra export-editoren.
- Nye immutable `LootReserveConsumed`-events deler en `ConsumeBatchId` og indeholder de konkrete purchase IDs.
- `LootReserveConsumptionReverted` opretter modgående state uden at ændre eller slette historiske events.
- Kun seneste ikke-reverterede batch kan reverteres; dobbelt-revert og ugyldige purchase references afvises under command-locken.
- Replay beregner consumed/restored purchase-state i memory. Persisted projections bruges ikke.
- LootReserve-, shop- og aktive purchase-queries udelader consumed state, mens revert genskaber den oprindelige aktive state.
- UI viser preview/confirmation, loading/error states, seneste batch og `Revert latest consumption`.

Der er ikke oprettet en migration: Slice 13d udvider kun JSON-payloads i den eksisterende event-store og ændrer ikke EF-schemaet.

Verifikation:

- Solution build passerer med `dotnet build DKP.slnx -p:UseAppHost=false --no-restore`.
- Eksisterende tests bevares; der bør suppleres med PostgreSQL-tests for consume/revert og replay, hvis der senere opstår en separat test-fixture til LootReserve-flowet.

Kendte begrænsninger:

- “Striket” og “export-ready” er UI-state; backend modtager kun de valgte bruger-ID’er og genvaliderer den faktiske event-state.
- Consume bruger hele den aktive SoftReserve-purchase quantity og hele den aktive RollBonus-enhed pr. bruger.
- Consume/revert er Officer-only og påvirker ikke DKP-balance eller historiske køb.

## Slice 13e – LootReserve modifiers

Status: Implementeret.

Formålet er at give Officers mulighed for at tildele midlertidige negative modifiers til SoftReserve eller RollBonus i et begrænset antal LootReserve-consume-batches. Modifiers ændrer ikke køb, DKP-balance eller historiske events.

Plan:

- Officer kan tildele `-SoftReserve` eller `-RollBonus` til én eller flere brugere med årsag og antal resterende exports/raids.
- Modifiers gemmes som immutable events og replayes i memory. Der oprettes ingen persisted projection.
- En modifier påvirker den faktiske LootReserve-export og forbruges kun ved en succesfuld consume-batch.
- Effective-værdier beregnes med minimum 0. Midlertidige CSV-overrides ændrer ikke modifier-state.
- Consume/revert bruger samme `ConsumeBatchId`, så et revert også genskaber modifierens resterende anvendelser.
- UI viser aktive modifiers, resterende anvendelser og effective SoftReserve/RollBonus på `/admin/loot-reserve`.
- Multi-user assignment er atomisk, Officer-only og beskyttet mod blokerede brugere, ugyldige beløb og dobbeltklik.

Forventede events:

- `LootReserveModifierGranted`.
- `LootReserveModifierConsumed`.
- `LootReserveModifierRevoked`.

Verificeret:

- Immutable modifier-events og event-store replay er implementeret.
- SoftReserve/RollBonus penalties trækkes fra effective LootReserve-værdier og bliver aldrig negative.
- Modifier-forbrug kobles til `ConsumeBatchId`, og revert genskaber resterende anvendelser.
- Officer-only grant/revoke, multi-user assignment, confirmation og active modifier overview er implementeret.
- Solution build og testsuite passerer; replay-tests dækker grant, consume, revert og dobbeltforbrug.

Kendte begrænsninger:

- En modifier forbruges ved en succesfuld LootReserve consume-batch, ikke ved blot at generere tekst.
- Der kræves ingen migration, fordi modifiers gemmes som JSON events i den eksisterende event-store.

## Slice 13g – Achievement requests fra medlemslisten

Status: Implementeret.

Medlemmer kan nu anmode om aktive achievements fra `/my-achievements`, både når de er Available og Previously revoked. Der oprettes højst én pending request pr. bruger og achievement, og medlemmet kan annullere sin egen pending request. Officers kan se, godkende og afvise achievement requests direkte på `/admin/achievements`.

Ved godkendelse oprettes et positivt `DkpPosted`-event med achievement-reference samt den tilhørende `UserAchievement`. Officer-authorization, aktivt achievement, eksisterende achievement og request-status valideres serverside. Preset requests fortsætter med at bruge det samme request-flow.

Migration: `20261006201039_Slice13gAchievementRequests` tilføjer achievement-reference, relation og unique pending-index til `DkpAwardRequests`.

## Slice 14 – Deployment og production hardening

Status: Påbegyndt.

Dockerfile, Docker Compose med PostgreSQL, pgAdmin, Traefik, Watchtower og DKP samt reverse-proxy-konfiguration for `wowforever.coffecottage.dk` er tilføjet. Se `codex/Deployment.md` for DNS, secrets og opstart. Health checks, fuld production-hardening og deployment-test er fortsat resterende arbejde.

En fremtidig Discord Bot/API-integration er dokumenteret i `codex/DiscordBotIntegration.md`. Den er ikke implementeret endnu. Planen bruger `DKP.DiscordBot.csproj` og `DKP.Api.csproj` i den eksisterende solution, hvor API’et hostes under `/api` på samme URL som Blazor.

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
