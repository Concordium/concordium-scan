# Download base image ubuntu 20.04
FROM ubuntu:20.04

# LABEL about the custom image
LABEL maintainer="stefan@fintech.builders"
LABEL version="0.1"
LABEL description="This is custom Docker Image for \
CCScan frontend"

# Disable Prompt During Packages Installation
ARG DEBIAN_FRONTEND=noninteractive

# Update Ubuntu Software repository
RUN apt update
RUN apt install curl -y

## Set up node and NPM
RUN curl -sL https://deb.nodesource.com/setup_16.x |  bash - && \
    apt -y install nodejs

COPY .output /app

# Expose Port for the Application
EXPOSE 3000
##Todo : also expose http/api ports

CMD [ "node", "app/server/index.mjs" ]
