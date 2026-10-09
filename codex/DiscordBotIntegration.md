# Discord Bot-integration

## Slice B1 – API-grundlag og Discord-bot connection

Status: Implementeret.

B1 leverer det tekniske fundament uden DKP-medlemsfunktioner:

- `DKP.Api.csproj` er tilføjet til den eksisterende solution og hostes af Blazor under `/api`.
- API’et bruger almindelige `[ApiController]`-REST controllers; Blazor registrerer services via `AddDkpApi()` og mapper controllers direkte med `app.MapControllers()`.
- `GET /api/bot/health` kræver `X-DKP-Bot-Secret` og returnerer en neutral health-status.
- `DKP.DiscordBot.csproj` er et separat `net10.0` executable-projekt uden adgang til Domain, DbContext eller Infrastructure.
- Botten bruger Discord Gateway, `Discord.Net` og guild-specific registrering af `/dkp-ping`.
- `/dkp-ping` kalder API'et via typed `HttpClient`, timeout og correlation-id.
- Gateway-disconnects logges, commands registreres idempotent ved reconnect, og botten lukker kontrolleret ved shutdown.
- Der er ikke tilføjet database-tabeller, events eller migrations.

Konfigurationen kommer fra environment variables eller tilsvarende secret configuration:

```env
DISCORD_BOT_TOKEN=<bot-token>
DISCORD_APPLICATION_ID=<application-id>
DISCORD_GUILD_ID=1505886353131311136
DKP_BOT_API_URL=http://dkp:8080
DKP_BOT_API_SECRET=<lang-random-secret>
```

`DKP_BOT_API_SECRET` skal sættes ens i webappens `DkpBot:ApiSecret` og i bot-processens `DKP_BOT_API_SECRET`. Secrets er ikke hardcodet i source eller tracked `.env`.

B1 har ingen production bot-container endnu; det hører til B8.

## Formål

Dette dokument beskriver, hvordan DKP senere kan udvides med en Discord-bot, så medlemmer kan bruge udvalgte DKP-funktioner direkte fra Discord.

Botten skal i første version kun understøtte member-features. Officer-funktioner eksponeres ikke gennem botten. Serverside-authorization bevares stadig i Application-laget, så en fejl i botten ikke kan give adgang til officer-commands.

Guild Members og Guild Activity er ikke en del af bot-flowet i første version.

## Hvad er en Discord-bot?

En Discord-bot er et program, der logger ind på Discord med en Bot Token og reagerer på events eller slash commands.

Eksempel:

```text
Member skriver /dkp
  → Discord sender interaction til botten
  → Botten identificerer Discord User ID
  → Botten kalder DKP API
  → API/Application henter saldo fra event-store
  → Botten svarer i Discord
```

Botten er ikke en indstilling inde i Discord. Det er et separat program, der skal køre et sted, ligesom DKP-webappen og PostgreSQL kører som services.

## Hvor lever botten?

Botten skal køre konstant for at kunne modtage Discord-events. Den kan køre:

- På samme VPS/server som DKP-webappen.
- Som en separat Docker-container.
- På en cloud-host.
- Lokalt under udvikling.

I production anbefales en separat Compose-service:

```text
postgres
dkp
discord-bot
traefik
watchtower
pgadmin
```

Botten behøver ikke selv være offentligt tilgængelig, hvis den bruger Discord Gateway. Den opretter selv en udgående forbindelse til Discord. API-kaldet til DKP kan gå internt over Docker-netværket.

## Gateway eller webhook

### Gateway – anbefalet til første version

Botten holder en udgående, persistent forbindelse til Discord Gateway. Discord sender slash commands og events over denne forbindelse.

Fordele:

- Ingen offentlig bot-URL er nødvendig.
- Passer godt til en bot, der kører som Docker-service.
- Simpel lokal udvikling.

Ulempe:

- Bot-processen skal køre hele tiden.

### HTTP interactions/webhook

Discord sender interactions til en offentlig HTTPS-URL.

Fordele:

- Kan skaleres som HTTP-service.
- Passer til serverless-hosting.

Ulemper:

- Kræver offentlig endpoint gennem Traefik.
- Discord-signaturer skal valideres korrekt.
- Mere kompleks deployment til første version.

Første bot-version bør bruge Gateway.

## Projektstruktur

Der oprettes ikke en ny solution. Begge projekter tilføjes til den eksisterende `DKP.slnx` som almindelige `.csproj`-projekter.

### DKP.DiscordBot

Et executable project, der indeholder:

- Discord connection og slash-command registration.
- Command handlers.
- Discord embeds, buttons, select menus og modals.
- API client til `DKP.Api`.
- Mapping fra API DTO’er til Discord responses.
- Bot logging og reconnect-håndtering.

Botten må ikke læse `DbContext` eller event-store direkte.

### DKP.Api

Et API-/endpoint-project, der refereres af Blazor-host’en og eksponerer endpoints under samme host:

```text
https://wowforever.coffecottage.dk/api/...
```

Det kan implementeres som et almindeligt `.csproj`-projekt med endpoint extensions, som registreres fra Blazor:

```csharp
app.MapControllers();
```

Blazor forbliver host for webapp og API, så der kræves ikke en separat webadresse eller ekstra reverse-proxy-route.

## Foreslået flow

```text
Discord
  → DKP.DiscordBot
  → DKP.Api (/api)
  → Facade contracts
  → Application commands/queries
  → Infrastructure/event-store
```

Botten bruger samme Facade-kontrakter og Application-business logic som Blazor. Der må ikke implementeres en separat bot-version af DKP-reglerne.

## Identitet og API-sikkerhed

Botten behøver ikke kende eller vise Officer/Member-roller i første version, fordi officerfunktioner ikke eksponeres.

API’et skal dog stadig:

- Autentificere, at requestet kommer fra den kendte bot-service.
- Modtage Discord User ID fra den validerede Discord interaction.
- Slå den interne User op via `DiscordId`.
- Afvise anonyme, ukendte eller blokerede brugere.
- Håndhæve ejerskab og member-regler i Application-laget.
- Afvise officer-commands, selv hvis et endpoint ved en fejl senere bliver kaldt fra botten.

Botten og API’et kan starte med en intern service-secret:

```env
DKP_BOT_API_SECRET=<lang-random-secret>
```

Secret’en ligger kun i bot- og DKP-containerens environment. Discord User ID er ikke i sig selv en API-authentication.

Botten skal hente User ID fra Discords validerede interaction-context og sende den videre til API’et sammen med service-authentication.

## Første member-features

Første version kan indeholde:

- `/dkp`
  - Aktuel DKP-saldo.
- `/dkp-history`
  - Seneste DKP-historik.
- `/characters`
  - Se egne characters.
  - Oprette, redigere og slette egne characters.
  - Vælge main character.
- `/shop`
  - Se aktive shop-items.
  - Se pris, quantity-limit og achievement-krav.
- `/shop buy`
  - Købe shop-item.
  - Quantity-validering, saldo, achievement-krav, max-per-user og blokering.
- `/purchases`
  - Se aktive og historiske køb.
  - Annullere egne aktive køb.
- `/achievements`
  - Se opnåede, revoked og tilgængelige achievements.
- `/achievement request`
  - Oprette en DKP-anmodning for et achievement.
  - Annullere egen pending request.
- `/dkp sources`
  - Se aktive DKP-sources og resterende anvendelser.

## Fremtidig multi-user request

En senere udvidelse kan give mulighed for, at én bruger opretter én samlet request for sig selv og taggede medlemmer:

```text
/achievement request achievement: Attendance users: @UserA @UserB @UserC
```

Flowet skal være:

1. Botten modtager initiatorens Discord User ID.
2. Botten udtrækker initiator og alle taggede Discord User IDs.
3. API’et deduplikerer brugerne.
4. Alle brugere valideres som eksisterende, ikke-blokerede og eligible.
5. Der oprettes requests atomisk.
6. Hvis én bruger fejler, oprettes ingen requests.
7. Initiatoren får svar med samlet resultat eller præcis fejl.

Det skal være en særskilt Application-command med en atomisk batch-grænse. Botten må ikke oprette records én efter én uden en samlet server-side validering.

## Fremtidige Discord-notifikationer

En senere udvidelse kan sende en besked til en konfigureret Discord-kanal, når der oprettes:

- DKP request.
- Achievement request.

Det bør ikke ske direkte fra Blazor eller API-controlleren. Et bedre flow er:

```text
Request oprettes
  → Application/event-flow registrerer notification event
  → Botten læser eller modtager notification
  → Botten sender embed til konfigureret kanal
```

Første version kan dog bruge en intern notification endpoint eller en simpel outbox, hvis event-baseret notification infrastructure ikke er etableret endnu.

Notifikationen bør indeholde:

- Discord display name.
- Main character, hvis den findes.
- Request-type.
- Achievement/preset/item.
- Tidspunkt.
- Link eller reference til officer-siden.

Personlige kommentarer og følsomme oplysninger bør begrænses i offentlige guild-kanaler.

## Discord Application-opsætning

Discord Application skal have:

- Bot user.
- Bot token.
- `bot` scope.
- `applications.commands` scope.
- Slash commands registreret globalt eller til den specifikke guild.
- Kun nødvendige intents.

Botten skal inviteres til guilden med den mindst mulige permission-pakke. Første version behøver normalt ikke administrator-rettigheder.

Typiske secrets:

```env
DISCORD_BOT_TOKEN=<bot-token>
DISCORD_APPLICATION_ID=<application-id>
DISCORD_GUILD_ID=1505886353131311136
DKP_BOT_API_URL=http://dkp:8080
DKP_BOT_API_SECRET=<lang-random-secret>
```

Bot-token og API-secret må ikke commit’es.

## Deployment med Docker Compose

Botten tilføjes senere til Compose:

```yaml
  discord-bot:
    build:
      context: .
      dockerfile: src/DKP.DiscordBot/Dockerfile
    restart: unless-stopped
    depends_on:
      - dkp
    environment:
      DISCORD_BOT_TOKEN: ${DISCORD_BOT_TOKEN}
      DISCORD_APPLICATION_ID: ${DISCORD_APPLICATION_ID}
      DISCORD_GUILD_ID: ${DISCORD_GUILD_ID}
      DKP_BOT_API_URL: http://dkp:8080
      DKP_BOT_API_SECRET: ${DKP_BOT_API_SECRET}
```

Botten skal ikke have en offentlig port, hvis Gateway anvendes.

## Bot-slices

### Slice B1 – API-grundlag og bot connection

Opret `DKP.Api.csproj` og `DKP.DiscordBot.csproj` i den eksisterende solution. API’et hostes af Blazor under `/api`, mens botten kører som separat executable.

- Intern bot-authentication mellem bot og API.
- Discord Gateway connection.
- Guild-specific slash-command registration.
- Reconnect, logging og graceful shutdown.
- `/api/health` for bot/API connectivity.
- Ingen member-business features endnu.

Acceptkriterier: Botten kan installeres i guilden, starter stabilt, registrerer en test-command og kan kalde API’et uden databaseadgang.

### Slice B2 – DKP-balance og historik

Status: Implementeret.

Tilføj første funktionelle member-flow:

- `/dkp balance` viser brugerens aktuelle saldo.
- `/dkp history` viser de seneste 10 historikposter.
- Discord User ID mappes til intern User.
- Ukendte, anonyme og blokerede brugere afvises.

Botten sender kun den validerede Discord User ID og service-secret til API’et. API’et slår brugeren op via `DiscordId`, afviser ukendte/blokerede brugere og returnerer kun den pågældende brugers replay-baserede saldo/historik. Acceptkriteriet er dermed opfyldt uden databaseadgang fra botten.

### Slice B3 – Characters

Status: Implementeret.

Tilføj member-management af egne characters:

- `/characters list`.
- Opret character via modal.
- Redigér og slet egne characters.
- Vælg main character.

API’et eksponerer character-flowet under `/api/bot/characters`. Botten kan vise egne characters, oprette og redigere via modals, slette med eksplicit confirmation og sætte main character. Ownership, main-character-regler og validering håndhæves i Application/Infrastructure.

Acceptkriterier: Ownership, main-character-regler og validering er identiske med Blazor-flowet.

### Slice B4 – Shop og purchases

Status: Implementeret.

Tilføj read og self-service purchase-flow:

- `/shop` viser aktive items, priser, limits og achievement-krav.
- `/shop buy` med quantity.
- `/purchases` viser egne køb.
- Annullering af egne aktive køb.

Køb går gennem bot-kontrakterne til Application og event-store. Botten implementerer ingen egen balance-, limit- eller achievement-logik. API’et eksponerer aktive items, egne køb, køb med quantity og annullering/refundering af egne aktive køb.

Acceptkriterier: Saldo, max-per-user, RollBonus, SoftReserve, refunds og achievement-gates håndhæves på samme måde som i webappen.

### Slice B5 – Achievements og DKP requests

Status: Implementeret.

Tilføjet:

- `/account create` til idempotent initial account-provisioning.
- `/achievements` med obtained, revoked og available.
- `/achievement request`.
- `/achievement requests`.
- Annullering af egne pending requests.

Botten bruger API’et under `/api/bot/achievements`, og requesten genbruger den eksisterende Application-validering. Request-status og event-baseret DKP matcher `/my-achievements` og `/my-dkp/sources`.

### Slice B6 – Atomiske multi-user requests

Status: Implementeret.

Giv en bruger mulighed for at tagge flere medlemmer i én achievement request:

```text
/achievement request achievement: Attendance users: @UserA @UserB
```

- Initiator og taggede brugere deduplikeres.
- Alle targets valideres før oprettelse.
- Hele operationen er atomisk.
- Én fejl betyder, at ingen requests oprettes.
- Initiator får et tydeligt samlet resultat.

API’et modtager initiatorens Discord ID og de taggede Discord IDs. Application deduplikerer initiator og targets, validerer alle brugere, aktive achievements, eksisterende awards og pending requests før der oprettes nogen records. Ingen database-migration er nødvendig.

Acceptkriterier: Ingen partial requests ved fejl, og member kan ikke tildele sig selv DKP direkte; alle requests forbliver pending, indtil en Officer behandler dem.

### Slice B6a – Preset-baserede DKP requests for flere spillere

Status: Implementeret.

Udvid botten med DKP requests baseret på de aktive DKP-presets fra `/my-dkp/sources`.

Planlagte commands:

- `/dkp sources` viser aktive presets, beløb, årsag og resterende anvendelser.
- `/dkp request` opretter en request for initiatoren.
- `/dkp request-many` opretter requests for initiatoren og flere taggede spillere.
- `/dkp requests` viser egne pending, approved, rejected og cancelled requests.
- `/dkp cancel` annullerer egne pending requests.

Multi-player-flowet skal deduplikere initiator og taggede Discord IDs, validere alle brugere, kontrollere aktivt preset, pending duplicates, quantity og lifetime-limit før nogen request gemmes. Alle requests oprettes atomisk, så én fejl giver rollback for hele gruppen. Botten må ikke uddele DKP direkte; Officer-godkendelse genvaliderer preset usage og event-store.

Implementeret med API-flow, preset-listing, én request, multi-user requests, deduplikering, blocked/unknown users, pending duplicates, limit-validering og rollback uden partial requests.

### Slice B7 – Discord request-notifikationer

Send besked til et konfigureret Discord-channel, når der oprettes:

- DKP request.
- Achievement request.

Notifikationer skal komme fra en reliable outbox/event-notification mekanisme, så database-operationen ikke fejler, hvis Discord midlertidigt er utilgængelig.

Acceptkriterier: Request gemmes selv om Discord-kanalen er utilgængelig, beskeden kan retries, og der sendes ikke dubletter.

### Slice B7a – Private Discord-beskeder ved behandlinger

Status: Planlagt.

Send en privat Discord-besked til den berørte bruger, når en Officer behandler en DKP- eller achievement-relateret handling:

- DKP request accepteres eller afvises.
- Achievement request accepteres eller afvises.
- Et shop-køb refunderes eller annulleres af en Officer.

Beskeden indeholder handling, beløb eller achievement/item, eventuel årsag eller Officer-kommentar samt tidspunkt.

Flowet er:

```text
Application command → committed event/database operation → outbox notification → Discord bot → private message
```

Databaseoperationen må ikke fejle, hvis DM ikke kan leveres. Notifikationer sendes først efter commit og skal have retry- og idempotency-beskyttelse ved bot-restart. Brugere med lukkede DMs håndteres som en kontrolleret warning i loggen. Der kræves ingen public webhook; Discord Gateway bruges til DM-leveringen.

Tests skal dække successful DM, DM-fejl uden rollback, retry, idempotency og korrekt tekst for approve, reject og refund.

### Slice B8 – Bot production hardening

- Dockerfile til botten.
- Compose-service og secrets.
- Health checks.
- Watchtower-label.
- Rate limiting og Discord API backoff.
- Idempotency ved crash efter command-commit.
- Structured logging og metrics.
- Deployment- og recovery-dokumentation.

Acceptkriterier: Botten genstarter automatisk, reconnecter, mister ikke committed requests og kan testes med en isoleret Discord/API-konfiguration.

## Samlet implementeringsrækkefølge

1. B1 – API-grundlag og bot connection
2. B2 – DKP-balance og historik
3. B3 – Characters
4. B4 – Shop og purchases
5. B5 – Achievements og DKP requests
6. B6 – Atomiske multi-user requests
7. B6a – Preset-baserede DKP requests for flere spillere
8. B7 – Discord request-notifikationer
9. B7a – Private Discord-beskeder ved behandlinger
10. B8 – Bot production hardening

## Ikke en del af første bot-version

- Officer DKP management.
- Officer shop administration.
- Officer achievement administration.
- Guild Members overview.
- Guild Activity overview.
- Rolleadministration.
- Direkte databaseadgang fra botten.

## Acceptkriterier

- Botten kører som et separat `.csproj` i samme solution.
- API’et kører på samme host under `/api`.
- Botten kan svare på `/dkp` for et authenticated Discord-medlem.
- Botten kan ikke tilgå officer-features.
- Blokerede og ukendte brugere afvises serverside.
- Botten kan ikke omgå event-store, ownership eller purchase-validering.
- Multi-user requests er atomiske, når de senere implementeres.
- Bot-notifikationer kan aktiveres til en konfigureret kanal uden at eksponere secrets.
