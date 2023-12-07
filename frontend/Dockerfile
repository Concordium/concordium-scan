ARG NODE_VERSION=17.0.0

FROM node:${NODE_VERSION}-slim as base

ARG ENVIRONMENT
ARG PORT=3000
ARG HOST=0.0.0.0

ENV ENVIRONMENT=$ENVIRONMENT

WORKDIR /app

# Build
FROM base as build

COPY --link package.json yarn.lock .
RUN yarn

COPY --link . .
RUN yarn build

# Run
FROM base

ENV PORT=$PORT
ENV HOST=$HOST

COPY --from=build /app/.output .

CMD ["node", "server/index.mjs"]
