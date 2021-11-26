# Download base image ubuntu 20.04
FROM ubuntu:20.04

# LABEL about the custom image
LABEL maintainer="stefan@fintech.builders"
LABEL version="0.1"
LABEL description="This is custom Docker Image for \
CCScan Backend"

# Disable Prompt During Packages Installation
ARG DEBIAN_FRONTEND=noninteractive

# Update Ubuntu Software repository
RUN apt update
RUN apt-get install wget   -y
RUN apt install curl -y

## Set up node and NPM
RUN curl -sL https://deb.nodesource.com/setup_14.x |  bash - && \
    apt -y install nodejs
RUN  npm install http-serve -g -y
RUN  npm install pm2 -g

# Install nginx, php-fpm and supervisord from ubuntu repository
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb &&\
    rm packages-microsoft-prod.deb

RUN  apt-get update;  apt-get install -y apt-transport-https &&    apt-get update &&    apt-get install -y dotnet-sdk-6.0
RUN  apt-get update;  apt-get install -y apt-transport-https &&    apt-get update &&    apt-get install -y aspnetcore-runtime-6.0


COPY . /app

RUN cd /app  && \
    dotnet publish -c Release -o /srv/backend/ &&\
    cd /srv/backend/ && chmod 777 ./Application &&\
    ./Application migrate-db && pm2 start ./Application

# Expose Port for the Application 
EXPOSE 5551 
##Todo : also expose http/api ports
