#!/bin/sh
set -eu

if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

domain="${SERVER_NAME:-webinventory.duckdns.org}"
admin_email="${ADMIN_EMAILS:-}"
email="${LETSENCRYPT_EMAIL:-${admin_email%%,*}}"

if [ -z "${email}" ]; then
    echo "Set LETSENCRYPT_EMAIL in .env before issuing a certificate." >&2
    exit 1
fi

docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path /var/www/certbot \
    --email "${email}" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d "${domain}"

docker compose exec nginx nginx -s reload
