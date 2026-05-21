FROM nginx:1.27-alpine

RUN apk add --no-cache openssl

COPY docker/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY docker/nginx/docker-entrypoint.d/10-bootstrap-cert.sh /docker-entrypoint.d/10-bootstrap-cert.sh

RUN chmod +x /docker-entrypoint.d/10-bootstrap-cert.sh
