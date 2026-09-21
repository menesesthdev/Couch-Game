FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# "docker" = build otimizado sem trocar o environment: /api continua relativo e o nginx faz o proxy.
RUN npx ng build --configuration docker

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/valorant-coach/browser /usr/share/nginx/html
EXPOSE 80
