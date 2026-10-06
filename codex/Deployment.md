# Lokal/prod-lignende Docker-opstart

## DNS

Peg følgende DNS-records til serverens offentlige IP:

- `wowforever.coffecottage.dk` → serverens IP
- `pgadmin.coffecottage.dk` → serverens IP, hvis pgAdmin skal være tilgængelig via Traefik

Port 80 og 443 skal være åbne udefra. Traefik henter TLS-certifikater via Let’s Encrypt.

## Konfiguration

Kopiér `.env.example` til `.env`, og indsæt rigtige passwords samt Discord Client ID/Secret. `.env` er ignoreret af Git og må ikke commit’es.

Discord OAuth redirect URI skal være:

`https://wowforever.coffecottage.dk/signin-discord`

## Start

```powershell
docker compose up -d --build
docker compose logs -f dkp
```

DKP findes på `https://wowforever.coffecottage.dk`. pgAdmin findes lokalt på `http://localhost:5050` og, hvis DNS er sat op, på `https://pgadmin.coffecottage.dk`.

Applikationen kører migrationer ved opstart. PostgreSQL-data, pgAdmin-data og Traefik-certifikater gemmes i Docker-volumes.

Watchtower opdaterer kun containere med Watchtower-label og bruger det angivne polling-interval.

## Lokal ngrok-test

Brug `docker-compose.ngrok.yml` som override sammen med hovedfilen:

```powershell
docker compose -f docker-compose.yml -f docker-compose.ngrok.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.ngrok.yml logs -f ngrok
```

Sæt `NGROK_URL` og `NGROK_AUTHTOKEN` i `.env`. Ngrok peger på DKP-containerens interne port 8080 og bruger derfor ikke Traefik i denne lokale test.

Tilføj derefter følgende Discord redirect URI for den valgte ngrok-host:

`https://<din-ngrok-host>/signin-discord`
