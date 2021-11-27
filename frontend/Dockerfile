# Download base image ubuntu 20.04
FROM ubuntu:20.04

# LABEL about the custom image
LABEL maintainer="stefan@fintech.builders"
LABEL version="0.1"
LABEL description="This is custom Docker Image for \
CCScan frontend"

# Disable Prompt During Packages Installation
ARG DEBIAN_FRONTEND=noninteractive
RUN apt-get update 
RUN apt-get install apache2 -y
RUN apt-get install apache2-utils -y
RUN apt-get clean 
COPY .  /var/www




EXPOSE 80 
CMD [“apache2ctl”, “-D”, “FOREGROUND”]
