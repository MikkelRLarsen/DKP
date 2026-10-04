# Guild DKP System

## Projektidé

Projektet er en simpel hjemmeside til administration af DKP (**Dragon Kill Points**) for en World of Warcraft Classic guild.

Formålet er at give guildens medlemmer ét centralt sted, hvor de kan:

- Se deres aktuelle DKP.
- Se hvordan deres DKP er blevet optjent og brugt.
- Se andre guildmedlemmers DKP.
- Senere bruge DKP til at købe ekstra Soft Reserves.

Projektet skal holdes så simpelt som muligt både funktionelt og teknisk.

Derfor udvikles systemet som en **C# ASP.NET Core Blazor monolit**, hvor frontend og backend ligger i samme applikation.

Der anvendes **Radzen Blazor Components** til så stor en del af brugergrænsefladen som muligt.

Den første version fokuserer udelukkende på:

- Discord login.
- Guildmedlemmer.
- WoW-characters.
- DKP-saldo.
- DKP-transaktioner.
- Officer-administration.

LootReserve-integration og køb af Soft Reserves udvikles først, når basissystemet fungerer stabilt.

---

# 1. Teknologistak

Projektet anvender følgende teknologier:

```text
Language:
C#

Framework:
ASP.NET Core

Frontend:
Blazor

UI Components:
Radzen Blazor Components

ORM:
Entity Framework Core

Database:
PostgreSQL

Authentication:
Discord OAuth2

Deployment:
Docker

Architecture:
Monolith
```

Applikationen består derfor ikke af en separat frontend og backend.

Arkitekturen er:

```text
Browser
   │
   ▼
┌───────────────────────────────┐
│ ASP.NET Core / Blazor         │
│                               │
│ Radzen UI Components          │
│          │                    │
│          ▼                    │
│ Blazor Pages / Components     │
│          │                    │
│          ▼                    │
│ Application Services          │
│          │                    │
│          ▼                    │
│ Entity Framework Core         │
└──────────┬────────────────────┘
           │
           ▼
      PostgreSQL
```

Frontend kan dermed kalde application services direkte.

Der skal ikke bygges et separat REST API alene for at kommunikere mellem frontend og backend.

---

# 2. Hvorfor monolit?

Projektet skal være forholdsvis lille.

Der er derfor ikke behov for:

- Microservices.
- Separat frontend-applikation.
- Separat backend-applikation.
- API Gateway.
- Message brokers.
- Event-driven architecture.
- Kubernetes.

Et simpelt request-flow kan eksempelvis være:

```text
Blazor Page
     │
     ▼
DkpService
     │
     ▼
Entity Framework Core
     │
     ▼
PostgreSQL
```

Det holder både udvikling, debugging og deployment simpelt.

---

# 3. Radzen som UI-framework

Radzen Blazor Components skal bruges som det primære UI-framework.

Målet er at anvende Radzen-komponenter i stedet for selv at implementere almindelige UI-elementer.

Det gælder eksempelvis:

- Navigation.
- Layout.
- Tabeller.
- Formularer.
- Inputs.
- Dropdowns.
- Buttons.
- Dialoger.
- Notifications.
- Cards.
- Badges.
- Progress indicators.
- Menus.
- Data grids.
- Pagination.
- Confirmation dialogs.

Custom HTML og CSS skal kun anvendes, når Radzen ikke allerede har en passende komponent.

---

# 4. Overordnet UI-layout

Applikationen kan bygges omkring Radzens layout-komponenter.

Eksempel:

```text
┌─────────────────────────────────────────────┐
│ RadzenHeader                                │
├───────────────┬─────────────────────────────┤
│               │                             │
│ RadzenSidebar │       Main Content          │
│               │                             │
│ Dashboard     │                             │
│ Members       │                             │
│ My DKP        │                             │
│ Administration│                             │
│               │                             │
├───────────────┴─────────────────────────────┤
│ Footer                                      │
└─────────────────────────────────────────────┘
```

Der kan anvendes:

- `RadzenLayout`
- `RadzenHeader`
- `RadzenSidebar`
- `RadzenBody`
- `RadzenPanelMenu`

Navigationen kan eksempelvis være:

```text
Dashboard

My DKP

Guild Members

Administration
    DKP Management
    Users
```

Administration vises kun for brugere med Officer-rollen.

---

# 5. Login

Brugerne logger ind med **Discord OAuth2**.

Der skal ikke oprettes separate brugernavne eller passwords på hjemmesiden.

Ved første login gemmes eksempelvis:

- Discord User ID.
- Discord display name.
- Discord avatar.
- Oprettelsesdato.

Discord ID bruges som brugerens permanente identifikation.

Efter login kan brugerens Discord-information vises i UI'en.

Eksempel:

```text
┌─────────────────────────┐
│ [Avatar] Shock          │
│                         │
│ Character: Shockadin    │
│ DKP: 350                │
└─────────────────────────┘
```

Denne information kan eksempelvis vises med:

- `RadzenCard`
- `RadzenAvatar`
- `RadzenText`
- `RadzenBadge`

---

# 6. Characters

En Discord-bruger kan tilknyttes et World of Warcraft-character.

Eksempel:

```text
Discord:
Shock

Character:
Shockadin

Realm:
Firemaw
```

Formularen kan bygges med:

- `RadzenTemplateForm`
- `RadzenTextBox`
- `RadzenDropDown`
- `RadzenButton`
- Radzen validation components

Eksempel:

```text
Character Name
[ Shockadin                ]

Realm
[ Firemaw              ▼ ]

                     [Save]
```

---

# 7. Roller

Systemet har som minimum to roller.

## Member

Et almindeligt guildmedlem kan:

- Se sin DKP.
- Se sin DKP-historik.
- Se guildens DKP-liste.
- Se andre spilleres DKP.

I en senere version kan medlemmer også bruge deres DKP til at købe ekstra Soft Reserves.

---

## Officer

En officer kan desuden:

- Tilføje DKP.
- Fratrække DKP.
- Angive årsagen til en DKP-transaktion.
- Se alle spilleres DKP-historik.
- Foretage manuelle rettelser.
- Administrere brugere.

Officer-funktionerne skal beskyttes via ASP.NET Core authorization.

De skal derfor ikke kun skjules i UI'en.

Serveren skal også kontrollere, at brugeren faktisk har Officer-rollen.

---

# 8. DKP-system

DKP fungerer som en simpel virtuel valuta.

Hver spiller har en saldo.

Eksempel:

| Handling | DKP |
|---|---:|
| Dungeon med guilden | +20 |
| Raid deltagelse | +50 |
| Profession level 300 | +50 |
| Guild event | +25 |
| Manuel bonus | +10 |
| Senere: køb af Soft Reserve | -100 |

Pointværdierne skal kunne ændres senere uden større ændringer i systemet.

---

# 9. DKP som transaktioner

DKP skal ikke blot gemmes som et enkelt tal på brugeren.

Alle ændringer gemmes som individuelle transaktioner.

Eksempel:

```text
+50 DKP    Molten Core
+20 DKP    Stratholme Guild Run
+50 DKP    Alchemy 300
-20 DKP    Officer correction
```

Saldoen er:

```text
SUM(DkpTransaction.Amount)
```

Det giver:

```text
50 + 20 + 50 - 20 = 100 DKP
```

Fordelen er, at man altid kan se:

- Hvor pointene kom fra.
- Hvornår ændringen skete.
- Hvem der foretog ændringen.
- Hvor mange point der blev tilføjet eller fjernet.

Man ændrer derfor ikke brugerens saldo direkte.

Der oprettes altid en ny transaktion.

---

# 10. Dashboard

Når en bruger logger ind, vises et simpelt dashboard.

Dashboardet kan eksempelvis indeholde tre Radzen Cards:

```text
┌──────────────────┐
│ Current DKP      │
│                  │
│       350        │
└──────────────────┘

┌──────────────────┐
│ Character        │
│                  │
│ Shockadin        │
└──────────────────┘

┌──────────────────┐
│ Guild Rank       │
│                  │
│      #4          │
└──────────────────┘
```

Der kan anvendes:

- `RadzenCard`
- `RadzenRow`
- `RadzenColumn`
- `RadzenStack`
- `RadzenText`
- `RadzenIcon`
- `RadzenBadge`

Under disse kan brugerens seneste DKP-transaktioner vises.

Eksempel:

| Date | DKP | Reason |
|---|---:|---|
| 04/10 | +50 | Molten Core |
| 03/10 | +20 | Stratholme |
| 02/10 | +50 | Alchemy 300 |

Denne visning implementeres med `RadzenDataGrid`.

---

# 11. My DKP

Brugeren skal have en dedikeret side til sin DKP-historik.

Øverst vises saldoen:

```text
Current DKP

350
```

Denne kan vises i et `RadzenCard`.

Herefter vises hele transaktionshistorikken med `RadzenDataGrid`.

Kolonner kan være:

- Date.
- Amount.
- Reason.
- Created By.

Eksempel:

| Date | Amount | Reason | Officer |
|---|---:|---|---|
| 04/10 | +50 | Molten Core | OfficerOne |
| 03/10 | +20 | Stratholme | OfficerTwo |
| 02/10 | -100 | Correction | OfficerOne |

Positive og negative værdier kan tydeliggøres med eksempelvis `RadzenBadge`.

---

# 12. Guild Members

Alle guildmedlemmer kan se en oversigt over medlemmerne.

Dette bygges med `RadzenDataGrid`.

Eksempel:

| Character | Discord | DKP | |
|---|---|---:|---|
| Shockadin | Shock | 450 | View |
| Tankman | Tank | 390 | View |
| Magebolt | Mage | 310 | View |
| Healbot | Heal | 280 | View |

DataGrid skal så vidt muligt bruge Radzens indbyggede funktioner til:

- Sorting.
- Filtering.
- Pagination.
- Search.
- Responsive layout.

Der skal derfor ikke bygges custom JavaScript til disse funktioner.

---

# 13. Player Details

Hvis man vælger en spiller fra Guild Members-listen, åbnes en detaljeret visning.

Den kan enten være:

- En separat Blazor Page.
- En `RadzenDialog`.

Eksempel:

```text
┌──────────────────────────────────┐
│ Shockadin                        │
│                                  │
│ Discord: Shock                   │
│ Realm: Firemaw                   │
│ Current DKP: 450                 │
│                                  │
│ Transaction History              │
│                                  │
│ +50 Molten Core                  │
│ +20 Stratholme                   │
│ -100 Correction                  │
└──────────────────────────────────┘
```

Der kan anvendes:

- `RadzenDialog`
- `RadzenCard`
- `RadzenDataGrid`
- `RadzenBadge`
- `RadzenText`

---

# 14. Officer Dashboard

Officererne får adgang til et separat administrationsområde.

Det kan bestå af:

```text
Administration
│
├── DKP Management
│
└── Users
```

Det kan vises via `RadzenPanelMenu`.

---

# 15. DKP Management

Officerer skal kunne finde en spiller og tilføje eller fjerne DKP.

Dette kan bygges som en kombination af:

- `RadzenDropDown`
- `RadzenNumeric`
- `RadzenTextBox`
- `RadzenButton`
- `RadzenTemplateForm`

Eksempel:

```text
Player

[ Shockadin                     ▼ ]

Amount

[ 50 ]

Reason

[ Molten Core                   ]

[ Add DKP ]    [ Remove DKP ]
```

Når officeren sender formularen, vises først en confirmation dialog.

Eksempel:

```text
Add 50 DKP to Shockadin?

Reason:
Molten Core

[Cancel]    [Confirm]
```

Dette implementeres med Radzens `DialogService`.

Efter en succesfuld transaktion vises en notification:

```text
50 DKP added to Shockadin
```

via `NotificationService`.

---

# 16. Add / Remove DKP

Både tilføjelse og fjernelse af DKP opretter en transaktion.

Eksempel på addition:

```text
Player:
Shockadin

Amount:
+50

Reason:
Molten Core

Created By:
OfficerOne
```

Eksempel på fratrækning:

```text
Player:
Shockadin

Amount:
-20

Reason:
Manual correction

Created By:
OfficerOne
```

Der må ikke være en funktion som direkte ændrer:

```text
Player.Dkp = 500
```

Alle ændringer går gennem `DkpTransaction`.

---

# 17. DKP uden for raids

DKP skal også kunne gives for aktiviteter uden for officielle raids.

Det kan eksempelvis være:

- Guild dungeon runs.
- Profession milestones.
- Materialer til guildbanken.
- Guild events.
- Hjælp til guildmedlemmer.
- Andre aktiviteter bestemt af guildens officers.

I basisversionen håndteres dette manuelt.

Eksempel:

```text
Player:
Shockadin

Amount:
20

Reason:
Scholomance Guild Run
```

Officer trykker derefter:

```text
Add DKP
```

---

# 18. Notifications

Radzens notification-system anvendes til feedback til brugeren.

Eksempelvis:

```text
Success
DKP transaction created.
```

```text
Error
Unable to create transaction.
```

```text
Warning
Player does not have enough DKP.
```

Der anvendes `NotificationService` frem for custom alert-bokse.

---

# 19. Dialoger

Confirmation og simple popup-vinduer implementeres gennem Radzens dialog-system.

Eksempler:

- Bekræft DKP-transaktion.
- Vis spiller.
- Bekræft brugerændring.
- Senere: bekræft køb af Soft Reserve.

Der anvendes `DialogService` frem for custom modal-komponenter.

---

# 20. Formularer og validering

Formularer bygges primært med:

- `RadzenTemplateForm`
- `RadzenTextBox`
- `RadzenNumeric`
- `RadzenDropDown`
- `RadzenButton`

Validering skal så vidt muligt vises direkte i Radzen-formularen.

Eksempel:

```text
Amount
[             ]

Amount is required.
```

Server-side validation skal stadig udføres.

UI-validation må ikke være den eneste validering af data.

---

# 21. Loading states

Når data indlæses, skal Radzen-komponenter bruges til feedback.

Eksempelvis:

- `RadzenProgressBar`
- `RadzenProgressBarCircular`
- Disabled Radzen buttons.

Eksempel:

```text
Loading players...

◯
```

Dette bruges eksempelvis ved:

- Login.
- Loading af medlemmer.
- Oprettelse af DKP-transaktion.
- Senere generering af LootReserve-data.

---

# 22. Database

Der anvendes PostgreSQL.

Entity Framework Core bruges til databasekommunikation.

De vigtigste entities i basisprogrammet er:

```text
User
Character
DkpTransaction
```

---

# 23. User

Eksempel på data:

```text
User

Id
DiscordId
DiscordName
AvatarUrl
Role
CreatedAt
```

En bruger identificeres eksternt gennem sit Discord ID.

---

# 24. Character

```text
Character

Id
UserId
Name
Realm
```

Relation:

```text
User
 │
 └── Character
```

I første version kan systemet eventuelt begrænses til ét primary character per bruger for at holde projektet simpelt.

---

# 25. DkpTransaction

```text
DkpTransaction

Id
UserId
Amount
Reason
CreatedByUserId
CreatedAt
```

Eksempel:

```text
UserId:
15

Amount:
50

Reason:
Molten Core

CreatedByUserId:
2

CreatedAt:
2026-10-04 19:30
```

Brugerens DKP beregnes som:

```text
SUM(Amount)
```

---

# 26. Application Services

Blazor-komponenterne skal ikke selv indeholde al business logic.

Logikken placeres i simple services.

Eksempel:

```text
Services/

DkpService
UserService
CharacterService
```

`DkpService` kan eksempelvis håndtere:

```text
GetBalanceAsync()

GetTransactionsAsync()

AddTransactionAsync()

RemoveDkpAsync()
```

Flow:

```text
Radzen / Blazor Page
       │
       ▼
   DkpService
       │
       ▼
    DbContext
       │
       ▼
   PostgreSQL
```

---

# 27. Projektstruktur

Projektet kan holdes i ét ASP.NET Core-projekt.

Eksempel:

```text
GuildDkp/
│
├── Components/
│   │
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   │
│   ├── Pages/
│   │   ├── Dashboard.razor
│   │   ├── MyDkp.razor
│   │   ├── Members.razor
│   │   └── Player.razor
│   │
│   └── Admin/
│       ├── DkpManagement.razor
│       └── Users.razor
│
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
│
├── Models/
│   ├── User.cs
│   ├── Character.cs
│   └── DkpTransaction.cs
│
├── Services/
│   ├── DkpService.cs
│   ├── UserService.cs
│   └── CharacterService.cs
│
└── Program.cs
```

Der er ikke behov for flere separate solution projects i den første version.

---

# 28. Basisversion – MVP

Basisprogrammet skal indeholde følgende.

## Authentication

- Discord login.
- Logout.

## User

- Discord-information.
- Member/Officer rolle.

## Character

- Tilknyt character.
- Character name.
- Realm.

## DKP

- Aktuel saldo.
- DKP-transaktioner.
- DKP-historik.

## Officer Administration

- Find spiller.
- Tilføj DKP.
- Fjern DKP.
- Angiv årsag.
- Se spillerhistorik.

## Guild Overview

- Liste over medlemmer.
- DKP-saldo.
- Spillerprofil.
- Transaktionshistorik.

## UI

UI skal primært implementeres gennem Radzen.

Der anvendes så vidt muligt:

- `RadzenLayout`
- `RadzenHeader`
- `RadzenSidebar`
- `RadzenPanelMenu`
- `RadzenDataGrid`
- `RadzenCard`
- `RadzenButton`
- `RadzenDropDown`
- `RadzenNumeric`
- `RadzenTextBox`
- `RadzenTemplateForm`
- `RadzenBadge`
- `RadzenAvatar`
- `RadzenDialog`
- `RadzenNotification`
- `RadzenProgressBar`
- `RadzenRow`
- `RadzenColumn`
- `RadzenStack`
- `RadzenText`

Når disse funktioner fungerer stabilt, betragtes basisprogrammet som færdigt.

---

# 29. Tilføjelse 1 – LootReserve integration

LootReserve-integrationen er **ikke en del af basisprogrammet**.

Den udvikles først efter det almindelige DKP-system fungerer.

Formålet er at gøre det lettere for officererne at overføre information fra hjemmesiden til LootReserve.

Der skal ikke bygges direkte kommunikation mellem hjemmesiden og World of Warcraft.

Hjemmesiden genererer i stedet tekst, som officeren kan kopiere.

---

# 30. Generate LootReserve Data

"Generate LootReserve Data" betyder, at hjemmesiden genererer en kopiérbar tekstliste.

Eksempel:

```text
Player,ReserveLimit
Shockadin,1
Magebolt,2
Tankman,3
Healbot,1
```

Officerens flow bliver:

```text
Website
   │
   ▼
Generate LootReserve Data
   │
   ▼
CSV / Text
   │
   ▼
Copy to Clipboard
   │
   ▼
Paste into LootReserve
```

Hjemmesiden kommunikerer altså ikke direkte med addonet.

---

# 31. LootReserve UI

LootReserve-siden kan fortsat bygges udelukkende med Radzen.

Eksempelvis:

`RadzenDataGrid`:

| Player | Reserve Limit |
|---|---:|
| Shockadin | 1 |
| Magebolt | 2 |
| Tankman | 3 |

Derefter:

```text
[Generate Export]
```

med `RadzenButton`.

Output kan vises i en:

```text
RadzenTextArea
```

Eksempel:

```text
Player,ReserveLimit
Shockadin,1
Magebolt,2
Tankman,3
```

Derefter:

```text
[Copy]
```

---

# 32. Tilføjelse 2 – Køb Soft Reserves

Denne funktion implementeres efter:

1. Basisprogrammet.
2. LootReserve-export.

Spillere kan derefter selv bruge DKP til at købe ekstra Soft Reserves.

Eksempel:

```text
Current DKP

320
```

vises i et `RadzenCard`.

Derefter kan der vises:

```text
Extra Soft Reserve

Cost:
100 DKP

[Buy]
```

---

# 33. Købsflow

Når spilleren trykker `Buy`, åbnes en Radzen confirmation dialog:

```text
Buy an additional Soft Reserve?

Cost:
100 DKP

Current balance:
320 DKP

Balance after purchase:
220 DKP

[Cancel] [Confirm]
```

Efter confirmation:

1. Serveren validerer brugerens saldo.
2. Købet registreres.
3. Der oprettes en DKP-transaktion på `-100`.
4. UI opdateres.
5. Radzen Notification vises.

Eksempel:

```text
Success

Soft Reserve purchased.
```

---

# 34. Købsregler

Serveren skal kontrollere:

- At brugeren har nok DKP.
- At brugeren ikke har nået maksimum antal Soft Reserves.
- At samme køb ikke gennemføres flere gange.
- At DKP bliver trukket samtidig med købet.
- At transaktionen bliver registreret.

DKP-fratrækning og køb skal udføres atomisk.

Man må ikke kunne ende med:

```text
Soft Reserve købt
```

uden:

```text
DKP trukket
```

eller omvendt.

---

# 35. Soft Reserve Purchase Entity

Når denne funktion implementeres, kan følgende entity tilføjes:

```text
SoftReservePurchase

Id
UserId
ReserveNumber
DkpCost
CreatedAt
```

Eksempel:

```text
UserId:
15

ReserveNumber:
2

DkpCost:
100
```

Dette betyder:

```text
Brugeren købte sin anden Soft Reserve
for 100 DKP.
```

---

# 36. Senere LootReserve-export

Efter købssystemet kan Reserve Limit automatisk beregnes.

Eksempel:

```text
Standard:
1

Purchased:
2

Total:
3
```

LootReserve-output:

```text
Player,ReserveLimit
Shockadin,1
Magebolt,2
Tankman,3
```

---

# 37. Deployment

Systemet skal kunne køres gennem Docker.

En simpel deployment kan være:

```text
Docker Compose
│
├── guild-dkp
│   │
│   └── ASP.NET Core
│       + Blazor
│       + Radzen
│
└── postgres
```

Der er kun én application container.

Blazor UI og backend deployes samlet.

---

# 38. Udviklingsrækkefølge

## Fase 1 – Project Setup

- ASP.NET Core.
- Blazor.
- Radzen Blazor Components.
- PostgreSQL.
- Entity Framework Core.

## Fase 2 – Discord Authentication

- Discord OAuth2.
- Automatisk brugeroprettelse.
- Login/logout.

## Fase 3 – Layout

Byg grundlayout med Radzen:

- Header.
- Sidebar.
- Navigation.
- User menu.

## Fase 4 – Characters

- Tilknyt character.
- Vis character.
- Rediger character.

## Fase 5 – DKP

- DkpTransaction.
- Beregn saldo.
- Transaktionshistorik.

## Fase 6 – Officer Administration

- Player dropdown.
- Amount.
- Reason.
- Add DKP.
- Remove DKP.
- Confirmation dialog.
- Notifications.

## Fase 7 – Guild Overview

- RadzenDataGrid med medlemmer.
- Filtering.
- Sorting.
- Player details.

På dette tidspunkt er **basisprogrammet færdigt**.

---

# 39. Udvidelser efter basisprogrammet

Når basisprogrammet fungerer:

## Udvidelse 1

LootReserve Export.

```text
RadzenDataGrid
      │
      ▼
Generate Export
      │
      ▼
RadzenTextArea
      │
      ▼
Copy
```

## Udvidelse 2

Spillere kan købe ekstra Soft Reserves.

## Udvidelse 3

LootReserve-export beregnes automatisk ud fra købte reserves.

---

# 40. Designprincipper

Projektet skal følge nogle få simple principper.

## Hold arkitekturen simpel

Brug:

```text
Blazor Component
      ↓
Service
      ↓
Entity Framework
      ↓
PostgreSQL
```

Tilføj kun flere lag, hvis der opstår et konkret behov.

---

## Brug Radzen før custom UI

Før der laves en custom UI-komponent, skal det undersøges, om Radzen allerede tilbyder den nødvendige funktionalitet.

Prioriteten er:

```text
1. Radzen component

2. Kombination af Radzen components

3. Simpel HTML/CSS

4. Custom JavaScript
```

Custom JavaScript skal derfor være sidste løsning.

---

## Hold business logic ude af UI

Blazor Pages skal primært håndtere:

- Visning.
- User interaction.
- Loading states.
- Formularer.

Business logic placeres i services.

Eksempel:

```text
DkpManagement.razor
        │
        ▼
    DkpService
        │
        ▼
    AppDbContext
```

---

## Brug server-side authorization

Det er ikke nok at skjule en Radzen button.

Eksempel:

```text
[Add DKP]
```

må gerne være skjult for normale medlemmer.

Men serveren skal samtidig forhindre Member-brugere i at kalde den bagvedliggende funktion.

---

# Målet med projektet

Slutresultatet skal være et enkelt og transparent guild-system bygget som én Blazor-monolit.

Systemet skal have:

- C# som eneste primære programmeringssprog.
- ASP.NET Core som framework.
- Blazor til frontend.
- Radzen Blazor Components som primært UI-framework.
- Entity Framework Core til databaseadgang.
- PostgreSQL som database.
- Discord OAuth2 til login.
- Docker til deployment.

Brugerne skal kunne:

- Logge ind med Discord.
- Tilknytte deres WoW-character.
- Se deres DKP.
- Se deres DKP-historik.
- Se guildens andre medlemmer.

Officerer skal kunne:

- Tilføje DKP.
- Fjerne DKP.
- Angive årsag.
- Se spillerhistorik.
- Administrere brugere.

Når dette fungerer, kan systemet udvides med:

1. LootReserve-export.
2. Køb af ekstra Soft Reserves.
3. Automatisk beregning af LootReserve-data.

Den overordnede tekniske regel er:

> **Hold systemet som én simpel C# Blazor-monolit, og anvend Radzen-komponenter til så meget af brugergrænsefladen som muligt.**