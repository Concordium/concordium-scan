# Updating Concordium node images in Azure container registry (ACR)
## Introduction
Concordium does not post images of the Concordium node to public docker repositories such as Docker Hub. That is why we keep these images in our own Azure container registry.
When a new version of the Concordium node is released the docker image must be uploaded to ACR and a new environment must be provisioned (remember to update the entrypoint file with the new version).

## Instructions
1. **Download docker image**

	The image must be downloaded from concordiums distribution site. Concordium usually do not announce an URL to these images. These are examples of previously used URLS:

		https://distribution.testnet.concordium.com/image/testnet-node-4.0.11-0.tar.gz
		https://distribution.mainnet.concordium.software/image/mainnet-node-3.0.2-0.tar.gz
		
		
2. **Load image to docker**

	Next the downloaded image must be loaded to a (local) docker installation. this works from ubuntu: 

		docker load < ${tarball_filename_mainnet}
		
3. **Tag image to upload to Azure container registry**

	Next the image loaded to a docker installation must be tagged correctly so that it can be pushed to ACR. Use this as template for constructing the right command:

		docker image tag concordium/mainnet-node:3.0.2-0 ccscan.azurecr.io/ccnode-mainnet:3.0.2-0
	
4. **Push image to Azure container registry**
	
	Finally the image must be pushed to ACR. To do this you need to be logged in to Azure. From the CLI issue this command to login:

		az acr login --name ccscan

	And use this command as a template to push the image:
	
		docker image push ccscan.azurecr.io/ccnode-mainnet:3.0.2-0

5. **Provision a new environment using the new image**

	Finally a new environment must be provisioned using the new image. Remember to update the entrypoint file with the new Concordium node image version.