# Discord Bot-integration

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
app.MapDkpApi();
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

## Planlagte implementeringstrin

1. Opret `DKP.Api.csproj` og fælles API-authentication.
2. Opret `DKP.DiscordBot.csproj` og inkluder begge i `DKP.slnx`.
3. Implementér bot health/reconnect og guild command registration.
4. Implementér `/dkp` som første vertical slice.
5. Implementér characters, shop, purchases og achievements.
6. Tilføj bot-compose-service og secrets.
7. Tilføj API authorization og integration tests.
8. Tilføj atomisk multi-user request.
9. Tilføj Discord-kanalnotifikationer.

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
