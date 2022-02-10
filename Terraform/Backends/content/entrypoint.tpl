#!/bin/bash

echo Starting entrypoint script...

set -e
trap 'last_command=$current_command; current_command=$BASH_COMMAND' DEBUG
trap 'echo "\"$${last_command}\" command filed with exit code $?."' EXIT

## house keeping and initial installs and packages
sudo apt-get update
sudo mkdir /home/setup
cd /home/setup

## Mount data drive
## TODO: Partition device name is hard-coded, maybe it should be read dynamically?
##       Should probably check if drive is already partitioned/formatted before 
##       (https://docs.microsoft.com/en-us/azure/virtual-machines/linux/attach-disk-portal)
until [ -e /dev/disk/azure/scsi1/lun1 ]; do echo waiting on data disk to be attached...; sleep 1; done

parted /dev/sdc --script mklabel gpt mkpart primary ext4 0% 100%
mkfs -t ext4 /dev/sdc1
partprobe /dev/sdc1

echo 
echo "#========= FDISK after partitioning ======(start)#"
fdisk -l
echo "#========= FDISK after partitioning ========(end)#"
echo 

sudo mkdir /data
sudo mount /dev/sdc1 /data


## Set up docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker ${vm_user}
sudo systemctl daemon-reload
sudo systemctl enable docker.service
sudo systemctl start docker.service
sudo chmod 666 /var/run/docker.sock


## set up Azure cloud credentials (connection to container registry):
sudo apt install azure-cli -y
echo Login to azure container registry for root
az acr login --name ccscan --username "${container_registry_username}" --password "${container_registry_password}"
echo Login to azure container registry for ${vm_user} 
sudo -u ${vm_user} az acr login --name ccscan --username "${container_registry_username}" --password "${container_registry_password}"


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
 --log-driver local \
 -e CONCORDIUM_NODE_COLLECTOR_NODE_NAME=FTB-${environment_name} \
 -e CONCORDIUM_NODE_RPC_SERVER_TOKEN=${cc_node_auth_token}  \
 -e CONCORDIUM_NODE_COLLECTOR_GRPC_AUTHENTICATION_TOKEN=${cc_node_auth_token}  \
 -e CONCORDIUM_NODE_LISTEN_PORT=8888 \
 -v /data/concordium.mainnet:/var/lib/concordium/data \
 ccscan.azurecr.io/ccnode-mainnet:3.0.1-0

docker run -td  \
 -p 10111:10000 \
 -p 18888:18888 \
 --network=testnetDocker \
 --name ccnode-testnet \
 --hostname ccnode \
 --log-driver local \
 -e CONCORDIUM_NODE_COLLECTOR_NODE_NAME=FTB-${environment_name} \
 -e CONCORDIUM_NODE_RPC_SERVER_TOKEN=${cc_node_auth_token} \
 -e CONCORDIUM_NODE_COLLECTOR_GRPC_AUTHENTICATION_TOKEN=${cc_node_auth_token} \
 -e CONCORDIUM_NODE_LISTEN_PORT=18888 \
 -v /data/concordium.testnet:/var/lib/concordium/data \
 ccscan.azurecr.io/ccnode-testnet:3.0.1-0


# Set up backend containers
docker run -td \
  -p 5000:5000 \
  --network=mainnetDocker \
  --restart=on-failure:3 \
  --name backend-mainnet \
  --log-driver none \
  -e PostgresDatabase:ConnectionString="Host=${postgres_hostname}.postgres.database.azure.com;Port=5432;Database=ccscan-mainnet;User ID=${postgres_user}@${postgres_hostname};password=${postgres_password};SSL Mode=Require;Trust Server Certificate=true" \
  -e ConcordiumNodeGrpc:AuthenticationToken="${cc_node_auth_token}" \
  -v /data/backend-logs.mainnet:/app/logs \
  ccscan.azurecr.io/${container_repository_backend}:latest 

docker run -td \
  -p 5001:5000 \
  --network=testnetDocker \
  --restart=on-failure:3 \
  --name backend-testnet \
  --log-driver none \
  -e PostgresDatabase:ConnectionString="Host=${postgres_hostname}.postgres.database.azure.com;Port=5432;Database=ccscan-testnet;User ID=${postgres_user}@${postgres_hostname};password=${postgres_password};SSL Mode=Require;Trust Server Certificate=true" \
  -e ConcordiumNodeGrpc:AuthenticationToken="${cc_node_auth_token}" \
  -v /data/backend-logs.testnet:/app/logs \
  ccscan.azurecr.io/${container_repository_backend}:latest 


# Set up watchtower container 
docker run -d \
  --name watchtower \
  --log-driver local \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v /home/${vm_user}/.docker/config.json:/config.json \
  -e WATCHTOWER_INCLUDE_STOPPED=true \
  -e WATCHTOWER_REVIVE_STOPPED=true \
  -e WATCHTOWER_POLL_INTERVAL=60 \
  -e WATCHTOWER_NOTIFICATIONS=slack \
  -e WATCHTOWER_NOTIFICATION_SLACK_HOOK_URL="https://hooks.slack.com/services/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" \
  -e WATCHTOWER_NOTIFICATION_SLACK_IDENTIFIER=watchtower-${environment_name} \
  -e WATCHTOWER_NOTIFICATION_SLACK_CHANNEL=#ccscan-servicebots \
  containrrr/watchtower \
  backend-testnet backend-mainnet

echo Entrypoint script finished...