#!/bin/bash

echo Starting entrypoint script...

## house keeping and initial installs and packages
sudo apt-get update
sudo mkdir /home/setup
cd /home/setup

## Mount data drive
## TODO - partition drive (if not partitioned) 
##      - read partition device name (not hard code)
##        (https://docs.microsoft.com/en-us/azure/virtual-machines/linux/attach-disk-portal)
sudo mkdir /data
sudo mount /dev/sdc1 /data

## Set up docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo groupadd docker
sudo usermod -aG docker ${vm_user}
sudo systemctl daemon-reload
sudo systemctl enable docker.service
sudo systemctl start docker.service
sudo chmod 666 /var/run/docker.sock

## set up Azure cloud credentials (connection to container registry):
sudo apt install azure-cli -y
echo Login to azure container registry for root
az acr login --name ccscan --username ccscan --password "${docker_registry_password}"
echo Login to azure container registry for ${vm_user} 
sudo -u ${vm_user} az acr login --name ccscan --username ccscan --password "${docker_registry_password}"

# set up discrete docker networks for main and testnet
docker network create -d bridge mainnetDocker
docker network create -d bridge testnetDocker

# Set up Concordium node containers
docker run -td \
 -p 10000:10000 \
 -p 8888:8888 \
 --network=mainnetDocker  \
 --name ccnode-mainnet  \
 --hostname ccnode \
 -e CONCORDIUM_NODE_COLLECTOR_NODE_NAME=${node_name} \
 -e CONCORDIUM_NODE_RPC_SERVER_TOKEN=${grpc_token}  \
 -e CONCORDIUM_NODE_COLLECTOR_GRPC_AUTHENTICATION_TOKEN=${grpc_token}  \
 -e CONCORDIUM_NODE_LISTEN_PORT=8888 \
 ccscan.azurecr.io/ccnode-mainnet:3.0.1-0

# -v /data/concordium.mainnet:/var/lib/concordium/data \

docker run -td  \
 -p 10111:10000 \
 -p 18888:18888 \
 --network=testnetDocker \
 --name ccnode-testnet \
 --hostname ccnode \
 -e CONCORDIUM_NODE_COLLECTOR_NODE_NAME=${node_name} \
 -e CONCORDIUM_NODE_RPC_SERVER_TOKEN=${grpc_token} \
 -e CONCORDIUM_NODE_COLLECTOR_GRPC_AUTHENTICATION_TOKEN=${grpc_token} \
 -e CONCORDIUM_NODE_LISTEN_PORT=18888 \
 ccscan.azurecr.io/ccnode-testnet:3.0.1-0

#-v /data/concordium.testnet:/var/lib/concordium/data \

# Set up postgres containers
docker run -td \
 --network=mainnetDocker \
 --name postgres-mainnet \
 --hostname postgres \
 -e POSTGRES_PASSWORD=passwordFTB2021 \
 -e POSTGRES_USER=postgres \
 -e POSTGRES_DB=ConcordiumScan \
 postgres

docker run -td \
 --network=testnetDocker \
 --name postgres-testnet \
 --hostname postgres \
 -e POSTGRES_PASSWORD=passwordFTB2021 \
 -e POSTGRES_USER=postgres \
 -e POSTGRES_DB=ConcordiumScan \
 postgres

# Set up backend containers
docker run -td \
 -p 5000:5000 \
 --network=mainnetDocker \
 --restart=on-failure:3 \
 --name backend-mainnet \
 -v /data/backend-logs.mainnet:/app/logs \
 ${docker_registry_backend_container}:latest 

docker run -td \
-p 5001:5000 \
--network=testnetDocker \
--restart=on-failure:3 \
--name backend-testnet \
-v /data/backend-logs.testnet:/app/logs \
${docker_registry_backend_container}:latest 

# Set up watchtower container 
docker run -d \
  --name watchtower \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v /home/concNodeVMuser/.docker/config.json:/config.json \
  -e WATCHTOWER_INCLUDE_STOPPED=true \
  -e WATCHTOWER_REVIVE_STOPPED=true \
  -e WATCHTOWER_POLL_INTERVAL=60 \
  -e WATCHTOWER_NOTIFICATIONS=slack \
  -e WATCHTOWER_NOTIFICATION_SLACK_HOOK_URL="https://hooks.slack.com/services/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" \
  -e WATCHTOWER_NOTIFICATION_SLACK_IDENTIFIER=watchtower-server-${node_name}.testnet \
  -e WATCHTOWER_NOTIFICATION_SLACK_CHANNEL=#ccscan-servicebots \
  containrrr/watchtower \
  backend-testnet backend-mainnet

echo Entrypoint script finished...