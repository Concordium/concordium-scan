ARG NODE_VERSION=17.0.0

FROM node:${NODE_VERSION} as build

ARG ENVIRONMENT

ENV ENVIRONMENT=$ENVIRONMENT

WORKDIR /app

COPY package.json yarn.lock ./
RUN yarn

COPY . .
RUN yarn build

FROM node:${NODE_VERSION}-slim

ENV PORT=3000
ENV HOST=0.0.0.0

EXPOSE 3000

COPY --from=build /app/.output .

CMD ["node", "server/index.mjs"]
