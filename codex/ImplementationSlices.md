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
