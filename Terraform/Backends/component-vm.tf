resource "azurerm_public_ip" "vm" {
  name                = "vm-nic-pip"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  allocation_method   = "Static"
  domain_name_label   = "${local.environment}-ccdscan-vm"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_network_interface" "vm" {
  name                = "vm-nic"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  ip_configuration {
    name                          = "ipc-vm"
    subnet_id                     = azurerm_subnet.backend.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.vm.id
  }

  tags = {
    environment = local.environment
  }
}

resource "azurerm_network_security_group" "this" {
  name                = "vm-nic-nsg"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  tags = {
    environment = local.environment
  }

  security_rule {
    name                       = "SSH"
    priority                   = 1001
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "ccnode_mainnet_p2p"
    priority                   = 1002
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "8888"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "ccnode_testnet_p2p"
    priority                   = 1003
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "18888"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "ccnode_mainnet_grpc"
    priority                   = 1004
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "10000"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "ccnode_testnet_grpc"
    priority                   = 1005
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "10111"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "http-mainnet"
    priority                   = 1007
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5000"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "http-testnet"
    priority                   = 1008
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5001"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

resource "azurerm_network_interface_security_group_association" "vm_nic" {
  network_interface_id      = azurerm_network_interface.vm.id
  network_security_group_id = azurerm_network_security_group.this.id
}

resource "azurerm_linux_virtual_machine" "vm" {
  name                            = "vm"
  location                        = azurerm_resource_group.this.location
  resource_group_name             = azurerm_resource_group.this.name
  network_interface_ids           = [azurerm_network_interface.vm.id]
  size                            = local.envs[local.environment].azure_vm_size
  custom_data                     = base64encode(local.vm_entrypoint_script)
  computer_name                   = "vm-${local.environment}"
  admin_username                  = local.vm_admin_user
  disable_password_authentication = true

  os_disk {
    name                 = "vm_disk_os"
    caching              = "ReadWrite"
    storage_account_type = "Premium_LRS"
  }

  source_image_reference {
    publisher = "canonical"
    offer     = "0001-com-ubuntu-server-focal"
    sku       = "20_04-lts-gen2"
    version   = "latest"
  }

  admin_ssh_key {
    username   = local.vm_admin_user
    public_key = data.azurerm_ssh_public_key.ssh_vm.public_key
  }

  tags = {
    environment = local.environment
  }
}

# Changes to the disk resource that leads to replacement will NOT trigger a replacement of the VM
# However, the VM needs to be replaced when the data disk is replaced.
# This needs to be done manually by running apply with -replace:
# terraform apply -replace="azurerm_linux_virtual_machine.vm"
resource "azurerm_managed_disk" "vm_data" {
  name                 = "vm_disk_data"
  location             = azurerm_resource_group.this.location
  resource_group_name  = azurerm_resource_group.this.name
  storage_account_type = "Premium_LRS"
  create_option        = "Empty"
  disk_size_gb         = "256"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_virtual_machine_data_disk_attachment" "vm_data" {
  managed_disk_id    = azurerm_managed_disk.vm_data.id
  virtual_machine_id = azurerm_linux_virtual_machine.vm.id
  lun                = "1"
  caching            = "ReadWrite"

  # provisioner "local-exec" {
  #   command = "az (stop vm)"
  # }
}

data "azurerm_key_vault_secret" "ccnode-auth-token" {
  name         = "${local.environment}-ccnode-auth-token"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

data "azurerm_key_vault_secret" "postgres-password" {
  name         = "${local.environment}-postgres-password"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}
