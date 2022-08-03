FROM node:18
WORKDIR /src
RUN yarn \
    && yarn global add firebase-tools \
    && yarn gql-codegen
ENTRYPOINT [ "sleep", "infinity" ]