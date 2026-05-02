FROM node:20-alpine AS development-dependencies-env
COPY . /app
WORKDIR /app
RUN npm ci

FROM node:20-alpine AS production-dependencies-env
COPY ./package.json package-lock.json /app/
WORKDIR /app
RUN npm ci --omit=dev

FROM node:20-alpine AS build-env
ARG KeycloakUrl
ARG KeycloakRealm
ARG KeycloakClientId
ARG HostUrl
ARG ApiUrl
ARG LOGO_URL
ARG BOARD_PRIMARY_LIGHT
ARG BOARD_PRIMARY
ARG BOARD_PRIMARY_DARK
ENV KeycloakUrl=$KeycloakUrl
ENV KeycloakRealm=$KeycloakRealm
ENV KeycloakClientId=$KeycloakClientId
ENV HostUrl=$HostUrl
ENV ApiUrl=$ApiUrl
ENV LOGO_URL=$LOGO_URL
ENV BOARD_PRIMARY_LIGHT=$BOARD_PRIMARY_LIGHT
ENV BOARD_PRIMARY=$BOARD_PRIMARY
ENV BOARD_PRIMARY_DARK=$BOARD_PRIMARY_DARK
COPY . /app/
COPY --from=development-dependencies-env /app/node_modules /app/node_modules
WORKDIR /app
RUN npm run build

FROM node:20-alpine
COPY ./package.json package-lock.json /app/
COPY --from=production-dependencies-env /app/node_modules /app/node_modules
COPY --from=build-env /app/build /app/build
WORKDIR /app
CMD ["npm", "run", "start"]