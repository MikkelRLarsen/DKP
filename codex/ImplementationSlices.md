# DKP – V2 ImplementationSlices

Dette dokument er den nye roadmap-version. Den tidligere roadmap er bevaret som [`ImplementationSlices.V1.md`](ImplementationSlices.V1.md) og indeholder historikken for den oprindelige Slice 1–14-indsats samt bot-slices.

V2 bygger videre på den eksisterende funktionalitet. De eksisterende login-, character-, DKP-, shop-, achievement-, LootReserve- og Discord-botflows behandles som baseline og implementeres ikke igen.

## Slice 1 – Bulletin Board core

Status: Implementeret (webflow; Discord-synkronisering og knapper hører fortsat til Slice 2/3).

Implementér et guild-bulletin board, hvor Officers kan publicere DKP-givende aktiviteter eller opgaver.

Funktionalitet:

- Officer kan oprette, redigere, aktivere og deaktivere bulletin items.
- Titel, beskrivelse, DKP-beløb, antal charges/pladser og valgfri deadline.
- Automatisk inaktiv efter deadline eller når charges er brugt.
- `UniquePerUser`, så hvert item kan begrænse brugeren til én claim eller tillade flere claims.
- Members kan se aktive items og oprette claims.
- Members kan se egne pending, approved, rejected og cancelled claims.
- Officers kan approve/reject claims.
- Approve opretter et positivt event-baseret DKP-event; reject opretter ikke et DKP-event.
- Blocked users kan ikke oprette eller modtage claims.

Forventede entities:

- `BulletinItem`: `Id`, `Title`, `Description`, `DkpAmount`, `MaxCompletions`, `DeadlineUtc`, `UniquePerUser`, `IsActive`, timestamps.
- `BulletinClaim`: `Id`, `BulletinItemId`, `UserId`, `Quantity`, `Status`, kommentarer, reviewer og eventreference.

Queries går Facade → Infrastructure. Commands går Facade → Application → Application persistence interfaces → Infrastructure. DKP-balance og historik kommer fortsat fra event-store replay.

Der skal genereres EF-migration. Tests skal dække charges, deadlines, `UniquePerUser`, ownership, authorization, duplicate claims, approval/rejection og atomisk stateændring.

## Slice 2 – Bulletin Board live Discord-opslag

Status: Implementeret.

Synkronisér aktive bulletin items til `DISCORD_MEDLEM_CHANNEL_ID` via den eksisterende Discord-bot og database-backed outbox.

- Ét aktivt bulletin item har én Discord-besked.
- Beskeden viser titel, beskrivelse, DKP, deadline og resterende charges.
- Beskeden opdateres ved claim, approval, cancellation eller anden ændring af charges.
- Beskeden slettes ved deaktivering, eller når deadline er overskredet.
- Ved `0` charges beholdes beskeden, hvis der stadig er pending claims; den slettes først, når quantity er `0` og alle pending claims er behandlet.
- Pending claims ændres ikke automatisk, selv om Discord-opslaget slettes efter deadline.
- Create/update/delete er retrybare og idempotente.
- Ingen brugere eller roller pinges; brug `AllowedMentions.None`.
- Discord-fejl må ikke rollbacke den underliggende bulletin-operation.
- Hvis channel-id mangler, fungerer webflowet fortsat uden Discord-synkronisering.

Der bruges stabil notification identity og message metadata, så beskeden kan redigeres eller genskabes. Existing `DiscordNotificationOutbox` genbruges, og der kræves ingen ny migration. Tests skal dække outbox-retry, slettet Discord-besked, duplicate events og korrekt livscyklus.

## Slice 3 – Discord interaction PoC

Status: Implementeret.

Lav en PoC, hvor begge Discord-modeller kan testes på et bulletin item. Begge flows bruger de samme bot-API-contracts og den eksisterende serverside claim-validering.

### 3.a Klikbar slash-command mention

Bulletin-opslag indeholder en Discord command mention til `/bulletin claim`, når botten har registreret commandens guild-ID. Commanden viser item-autocomplete og quantity-parameter.

Bulletin-beskeden viser en klikbar command mention:

```text
</bulletin claim:COMMAND_ID>
```

Discord udfylder commanden i chatfeltet, og brugeren trykker selv Enter. Commanden skal have en integer `quantity`-option, hvis klienten understøtter det:

```text
/bulletin claim quantity: 2
```

PoC’en skal dokumentere, at command mention ikke nødvendigvis kan pre-udfylde bulletin-ID eller quantity. Hvis brugeren skal vælge item og quantity bagefter, skal det fremgå tydeligt.

### 3.b Button-flow

Bulletin-opslag kan vise en `Claim`-knap. Knappen henter den aktuelle resterende quantity for den bruger, viser et ephemeral quantity-dropdown og kalder derefter samme `/api/bot/bulletin/claims`-flow som slash-commanden. Unique-per-user items viser kun quantity `1`. Der anvendes ikke modal.

## Slice 4 – Bulletin templates

Status: Implementeret.

Giv Officers mulighed for at genbruge standardopslag.

`BulletinTemplate` indeholder:

- `Id`, `Name`, `Title`, `Description`, `DkpAmount`.
- `MaxCompletions`, `UniquePerUser`, `DefaultDeadlineOffset`.
- `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`.

Officer kan oprette, redigere, aktivere, deaktivere og arkivere templates. Ved oprettelse af et bulletin item kan en template vælges som startværdier. Alle værdier kan ændres før publicering. Template-ændringer påvirker ikke allerede publicerede items, og templates publicerer aldrig automatisk et item.

Der skal være tydelig forskel på `Save template`, `Create draft` og `Publish bulletin item`. Tests dækker CRUD, kopiering, isolation og authorization. Der skal genereres EF-migration.

Implementeret i `Slice4BulletinTemplates`-migrationen. Templates er officer-beskyttede almindelige entities uden relation til bulletin items. Ved oprettelse valideres en valgt template som aktiv, mens de endelige formularværdier sendes og gemmes på det nye item. Template CRUD/statusændringer sender ingen Discord-notifikationer; kun det publicerede item bruger det eksisterende bulletin-outbox-flow.

## Slice 5 – Discord approve/deny af requests

Status: Implementeret.

Lad Officers behandle DKP requests, achievement requests og bulletin claims direkte fra Discord.

Nye request-notifikationer i `DISCORD_OFFICER_CHANNEL_ID` viser relevante request-data og knapperne `Approve` og `Deny`. Knappernes `custom_id` identificerer request og handling, men request-state genlæses altid serverside.

Approve-flow:

1. Officer klikker `Approve`.
2. Botten sender straks deferred/ephemeral response.
3. Botten sender request-id og Discord User ID til API’et med bot-service-authentication.
4. API/Application mapper brugeren til intern `User` og kræver database-rollen `Officer`.
5. Request, blocked state, limits, ownership og event-store state genvalideres under command-lock.
6. Godkendelse og eventuel DKP-tildeling gennemføres atomisk.
7. Request-beskeden slettes eller opdateres via outbox.
8. Officer får ephemeral success/error feedback.

Deny-flowet følger samme authorization og concurrency-regler, men opretter ikke DKP-event. En valgfri rejection reason kan indtastes i modal.

Tests dækker Member-afvisning, ukendt/blokeret bruger, korrekt actor, ét DKP-event, idempotens, stale requests, outbox-fejl og manglende pings.

## Slice 6 – Udvidede shop-grænser og delte item-regler

Status: Implementeret.

Udvid shop-items med valgfri:

- `DailyLimit`.
- `WeeklyLimit`.
- `MonthlyLimit`.
- `MaxTotalPerUser`.
- `MaxActivePerUser`.
- `LimitGroupKey`.

`ShopItem.Id` er teknisk identitet, `Key` identificerer itemtypen, og `LimitGroupKey` samler flere items, som skal dele regler. Items med samme `LimitGroupKey` deler quantity counters, periodiske limits og relevante active/total limits.

Indfør én fælles PurchaseLimitRules-service for webshop, My Purchases, admin-køb og Discord-bot. Availability og counters beregnes fra event-store replay. Daily/weekly/monthly beregnes i UTC. Cancelled tæller ikke. Soft Reserve og RollBonus bevarer deres særlige active-regler.

Alle commands genvaliderer regler under samme command-lock. Multi-user køb er atomiske. Der skal genereres EF-migration for katalogfelterne og tests for perioder, UTC-boundaries, shared groups, used/active, cancellation, concurrency og ens web/bot-resultater.

Implementeret i migrationerne `20261015124425_Slice6ShopPurchaseLimits` og `20261015134551_ShopPurchaseLimits`: kataloget understøtter nu genbrugelige `ShopPurchaseLimit`-konfigurationer, som shop-items valgfrit kan arve via en foreign key. Uden valgt konfiguration bruges itemets egne regler. Den fælles `ShopPurchaseLimitRules` anvendes af web- og bot-flowet, og DKP Shop administration kan oprette, redigere, aktivere/deaktivere og vælge limits. Concurrency- og PostgreSQL-integrationstests er fortsat en kendt begrænsning.

## Slice 7 – Dark Mode

Status: Implementeret.

Tilføj et gennemgående dark mode i Blazor/Radzen:

- Light, Dark og System preference.
- Theme-toggle i fælles layout/navigation.
- Lokal persistence via browserens `localStorage`.
- System preference som standard.
- Tidlig theme-loading for at undgå flash mellem temaer.
- Kontrasttilpasning af Radzen grids, dialogs, formularer, statusfarver og placeholders.
- Tilgængelig label og tooltip for toggle.

Der kræves ingen migration. Tests og manuel verifikation dækker reload, system preference, login/logout, mobile layout og kontrast.

Implementeret med global theme-toggle i layoutet, `light`/`dark`-valg, localStorage persistence, tidlig theme-applikation via `theme.js` og dark-mode styling for Radzen-layout, cards, grids, navigation og formularfelter. Der er ingen databaseændringer.

## Slice 8 – Discord Shop integration

Status: Implementeret.

Synkronisér aktive shop-items til `DISCORD_MEDLEM_CHANNEL_ID` via den eksisterende database-backed Discord notification outbox.

- Nye shop-items opretter et Discord-opslag med navn, beskrivelse, pris og maksimum.
- Ændringer i shop-itemets navn, beskrivelse, pris eller købsgrænser opdaterer opslaget.
- Deaktiverede eller skjulte shop-items får opslaget slettet.
- Opslaget viser en `Buy shop item`-knap.
- Knappen henter brugerens aktuelle availability via API’et og viser et ephemeral quantity-dropdown.
- Quantity begrænses til brugerens faktiske remaining quantity, og achievement-krav, DKP-saldo og purchase limits genvalideres af det eksisterende Application-flow.
- Et køb fra Discord går gennem samme `/api/bot/shop/purchases`-endpoint som øvrige bot-køb og sender ingen officer-notifikation.
- Medlemsopslag bruger `AllowedMentions.None`; Discord-fejl påvirker ikke shop-operationens database-transaktion.

Implementeret med `ShopItem`-outbox-synkronisering, `ShopItem`-notification rendering i botten samt `shop:buy`/`shop:quantity` component-flow. Der er ingen ny migration.

## Slice 9 – Discord member-kanaler som threads

Status: Implementeret.

Medlemsnotifikationer routes nu automatisk til stabile Discord public threads under `DISCORD_MEDLEM_CHANNEL_ID`:

- `Bulletin Board`
- `Achievements`
- `Repeatable DKP`
- `Shop`

Medlemsrouting kan konfigureres med `DISCORD_MEMBER_NOTIFICATION_MODE=text` eller `forum`.

- `text` bruger `DISCORD_MEDLEM_CHANNEL_ID` som parent og opretter/genbruger public threads med de fire faste kategorinavne.
- `forum` bruger manuelt oprettede forum-post/thread IDs fra `DISCORD_BULLETIN_THREAD_ID`, `DISCORD_ACHIEVEMENTS_THREAD_ID`, `DISCORD_DKP_SOURCES_THREAD_ID` og `DISCORD_SHOP_THREAD_ID`.
- Eksisterende outbox-flow bruges fortsat til create, update og delete, så beskeder opdateres eller fjernes i korrekt kanal/thread.
- Officer-notifikationer forbliver i `DISCORD_OFFICER_CHANNEL_ID`, og der sendes ingen pings.
- Der er ingen databaseændringer.

## Slice 10 – Automatisk Discord forum-sektioner og redigerbare initialbeskeder

Status: Implementeret.

Erstat den manuelle forum-thread-konfiguration med automatisk oprettelse og persistent registrering af medlemssektioner i ét Discord Forum Channel.

Forum-kanalen konfigureres med:

```env
DISCORD_MEMBER_NOTIFICATION_MODE=forum
DISCORD_MEMBER_FORUM_CHANNEL_ID=<forum-channel-id>
```

Botten opretter og vedligeholder følgende forum-opslag:

- `Bulletin Board` (`bulletin-board`).
- `Achievements` (`achievements`).
- `Repeatable DKP` (`dkp-sources`).
- `Shop` (`shop`).
- `Bot Help / Introduction` (`bot-help`).

### Startup og lifecycle

Ved gateway-start skal botten:

1. Validere, at den konfigurerede kanal er en Discord Forum Channel.
2. Hente de registrerede sektioner via DKP API’et.
3. Kontrollere, om hver registreret `ThreadId` stadig findes og tilhører det korrekte forum.
4. Oprette et manglende forum-opslag.
5. Sende eller opdatere den initiale besked.
6. Forsøge at fastgøre den initiale besked.
7. Gemme eller opdatere `ThreadId` og `InitialMessageId`.

Startup-synkronisering skal være idempotent og beskyttet mod dubletter ved samtidige bot-instanser. Hvis en thread eller initialbesked slettes manuelt, skal den opdages ved næste synkronisering og erstattes.

Alle eksisterende Bulletin-, Achievement-, DKP Source- og Shop-notifikationer routes derefter til den relevante registrerede section-thread. `Bot Help / Introduction` bruges som et stabilt, fastgjort opslag og modtager ikke item-notifikationer.

### Database og arkitektur

Tilføj en almindelig entity til persistent Discord-mapping:

```text
DiscordForumSection
- Id
- GuildId
- SectionKey
- ForumChannelId
- ThreadId
- InitialMessageId
- InitialMessageContent
- IsActive
- CreatedAtUtc
- UpdatedAtUtc
```

Botten må ikke læse databasen direkte. Den bruger API’et og Facade/Application-kontrakter til at hente og gemme mappingen. Discord-API-kald udføres fortsat i `DKP.DiscordBot`, mens section-state gemmes gennem den eksisterende backend-arkitektur.

Der skal genereres en EF-migration med unik constraint på eksempelvis `(GuildId, SectionKey)`.

### Bot Help / Introduction

Der oprettes en standardbesked, eksempelvis med:

- Hvad DKP-botten bruges til.
- Tilgængelige medlemscommands.
- Hvordan man køber shop-items.
- Hvordan man anmoder om DKP eller achievements.
- Hvordan man håndterer characters.
- Hvordan man kontakter en Officer.

Teksten gemmes som section-content og publiceres ikke automatisk som en almindelig notification. Den kan redigeres fra Blazor og gendannes til standardteksten.

### Blazor administration

Tilføj en Officer-only sektion til administrationen, hvor Officers kan:

- Se alle forum-sektioner og deres status.
- Redigere initialbeskeden for hver sektion.
- Se `ThreadId` og `InitialMessageId` som diagnostiske oplysninger.
- Gemme og straks synkronisere beskeden til Discord.
- Gendanne standardteksten for `Bot Help / Introduction`.
- Udløse manuel resynkronisering af alle sektioner.
- Se fejl, hvis forum-kanalen eller Discord-rettighederne mangler.

En ændring af initialbeskeden påvirker kun den registrerede Discord-besked og ændrer ikke de underliggende bulletin-, shop-, achievement- eller DKP-data.

### Discord-rettigheder

Botten skal mindst have følgende rettigheder på forum-kanalen:

- View Channel.
- Send Messages / Create Posts.
- Send Messages in Threads.
- Read Message History.
- Pin Messages.
- Manage Threads.
- Embed Links, hvis initialbeskederne bruger embeds.

`Manage Channels`, `Manage Server` og `Administrator` er ikke nødvendige.

Implementeret:

- `DiscordForumSection` med unik guild/section-mapping og migration `Slice10DiscordForumSections`.
- Bot API til registrering og læsning af section mappings.
- Idempotent bot-synkronisering af de fem forum-opslag, inklusive Bot Help, initialbesked og pin-forsøg.
- Periodisk synkronisering, så ændringer fra Blazor kan slå igennem efter kort tid, og slettede opslag kan genskabes.
- Officer-side på `/admin/discord-forum` til redigering af initialbeskeder.
- `DISCORD_MEMBER_FORUM_CHANNEL_ID` i compose og `.env.example`.

Kendt begrænsning: den første besked synkroniseres af botten via polling med cirka 30 sekunders interval efter startup. Den eksisterende manuelle thread-ID-konfiguration fungerer fortsat som fallback.

### Tests og acceptkriterier

Tests skal dække:

- Forum-kanalen valideres korrekt.
- Alle fem sektioner oprettes ved første startup.
- Restart genbruger eksisterende threads uden dubletter.
- `ThreadId` og `InitialMessageId` gemmes korrekt.
- Initialbeskeder sendes og fastgøres.
- En slettet thread genskabes og mappingen opdateres.
- Initialbeskeder kan redigeres fra Blazor.
- `Bot Help / Introduction` kan gendannes til standardtekst.
- Officer authorization håndhæves på UI og API.
- Manglende Discord-rettigheder giver en kontrolleret fejl.
- En fejl i én sektion stopper ikke synkronisering af de øvrige sektioner.
- Eksisterende notification outbox-flows fortsætter korrekt.

Der skal gennemføres build, tests og manuel smoke-test med tom mapping, restart, manuel sletning af thread og redigering af Bot Help-beskeden.

## Teststrategi for V2

Hver slice skal som minimum have:

- Application unit tests.
- Infrastructure/EF tests ved model- eller migrationændringer.
- Facade-kontrakt tests for nye DTO’er og interfaces.
- Authorization tests for Member/Officer/blocked-grænser.
- Discord-bot/API tests med fake HTTP og fake interactions.
- Build og komplet testsuite.
- Manuel smoke-test af den brugerrejse, slicen leverer.

## Versionering

- `ImplementationSlices.V1.md` er historisk roadmap og papirspor.
- Denne fil er V2-roadmapen og starter fra Slice 1.
- V2-slices genbruger eksisterende baseline-funktionalitet og skal ikke genimplementere V1-features.
