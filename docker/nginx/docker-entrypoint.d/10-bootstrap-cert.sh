#!/bin/sh
set -eu

create_certificate() {
    server_name="$1"
    cert_dir="/etc/letsencrypt/live/${server_name}"

    if [ -f "${cert_dir}/fullchain.pem" ] && [ -f "${cert_dir}/privkey.pem" ]; then
        return
    fi

    mkdir -p "${cert_dir}"
    openssl req \
        -x509 \
        -nodes \
        -newkey rsa:2048 \
        -days 1 \
        -keyout "${cert_dir}/privkey.pem" \
        -out "${cert_dir}/fullchain.pem" \
        -subj "/CN=${server_name}" >/dev/null 2>&1
}

create_certificate "${SERVER_NAME:-webinventory.duckdns.org}"
create_certificate "${ODOO_SERVER_NAME:-odoo.webinventory.duckdns.org}"
