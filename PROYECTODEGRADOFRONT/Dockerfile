# Frontend Angular servido por nginx en el puerto 4200 (coincide con CORS del backend)
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/RR_Nueva_Naturaleza_Frontend_Angular/browser /usr/share/nginx/html
EXPOSE 4200
