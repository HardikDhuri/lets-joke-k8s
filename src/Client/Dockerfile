FROM node:18 AS build

WORKDIR /app

COPY ./Client/package*json ./

RUN npm install

COPY ./Client .

RUN npm run build

FROM nginx:1-alpine-slim

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD [ "nginx", "-g", "daemon off;" ]