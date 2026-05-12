FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM alpine:3.21
RUN apk add --no-cache nginx nginx-mod-http-headers-more gettext \
    && rm -f /etc/nginx/http.d/default.conf
COPY --from=build /app/dist/lifehub-frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf.template
EXPOSE 80 443
CMD ["sh", "-c", "envsubst '${DOMAIN}' < /etc/nginx/nginx.conf.template > /etc/nginx/http.d/default.conf && nginx -g 'daemon off;'"]
