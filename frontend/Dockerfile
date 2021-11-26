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
RUN apt-get install –y apache2 
RUN apt-get install –y apache2-utils 
RUN apt-get clean 
COPY .  /var/www

RUN http-serve -p 4220


EXPOSE 80 
 CMD [“apache2ctl”, “-D”, “FOREGROUND”]
