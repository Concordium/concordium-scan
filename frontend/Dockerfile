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
RUN curl -sL https://deb.nodesource.com/setup_14.x |  bash - && \
    apt -y install nodejs
RUN  npm install http-serve -g -y

COPY . /app
RUN cd /app
CMD http-serve -p 4220 


# Expose Port for the Application 
EXPOSE 4220 
##Todo : also expose http/api ports
