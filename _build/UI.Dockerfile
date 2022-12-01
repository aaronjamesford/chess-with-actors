FROM nginx:stable-alpine

COPY _build/out/ui /usr/share/nginx/html