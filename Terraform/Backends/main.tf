data "azurerm_ssh_public_key" "ssh_vm" {
  # ----------------------------------------------------------------------------------------------------------
  # NOTE: The SSH key for the given environment must exist before provisioning!
  #
  #       If it doesn't: Create the SSH-key in the Azure Portal in the resource group mentioned below.
  #       (remember to upload the private key part (pem file) as a secret to the Azure KeyVault for safe keeping)
  # ----------------------------------------------------------------------------------------------------------
  name                = "ssh-vm-${local.environment}"
  resource_group_name = "ccscan-ssh-keys"
}

resource "azurerm_resource_group" "this" {
  name     = local.envs[local.environment].resource_group_name
  location = "northeurope"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_virtual_network" "this" {
  name                = "vnet"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  tags = {
    environment = local.environment
  }
}

resource "azurerm_subnet" "frontend" {
  name                 = "snet-frontend"
  resource_group_name  = azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = ["10.0.0.0/24"]
}

resource "azurerm_subnet" "backend" {
  name                 = "snet-backend"
  resource_group_name  = azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = ["10.0.1.0/24"]
}

data "azurerm_key_vault" "ccscan" {
  name                = "ccscan"
  resource_group_name = "ccscan-key-vault"
}
