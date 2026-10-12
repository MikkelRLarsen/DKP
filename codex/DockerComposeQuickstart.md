# Docker Compose quickstart

## 1. Opret `.env`

Kopiér eksempelkonfigurationen:

```powershell
Copy-Item .env.example .env
```

Udfyld derefter `.env` med rigtige værdier:

```env
POSTGRES_DB=dkp
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<stærkt-postgres-password>

PGADMIN_DEFAULT_EMAIL=admin@coffecottage.dk
PGADMIN_DEFAULT_PASSWORD=<stærkt-pgadmin-password>

DISCORD_CLIENT_ID=<discord-client-id>
DISCORD_CLIENT_SECRET=<discord-client-secret>
DISCORD_GUILD_ID=1505886353131311136

DKP_HOST=wowforever.coffecottage.dk
PGADMIN_HOST=pgadmin.coffecottage.dk
LETSENCRYPT_EMAIL=admin@coffecottage.dk
```

Commit aldrig `.env` eller secrets til Git.

## 2. Production-lignende opstart med Traefik

DNS skal pege `wowforever.coffecottage.dk` til serverens offentlige IP, og Discord redirect URI skal være:

```text
https://wowforever.coffecottage.dk/signin-discord
```

Start services:

```powershell
docker compose up -d --build
docker compose logs -f dkp
```

Stop services:

```powershell
docker compose down
```

## 3. Lokal test med ngrok

Tilføj også til `.env`:

```env
NGROK_URL=https://little-stylishly-blot.ngrok-free.dev
NGROK_AUTHTOKEN=<ngrok-authtoken>
```

Start med ngrok-override:

```powershell
docker compose -f docker-compose.yml -f docker-compose.ngrok.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.ngrok.yml logs -f ngrok
```

Discord redirect URI bliver:

```text
https://little-stylishly-blot.ngrok-free.dev/signin-discord
```

Stop begge Compose-filer:

```powershell
docker compose -f docker-compose.yml -f docker-compose.ngrok.yml down
```
