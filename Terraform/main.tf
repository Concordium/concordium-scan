resource "azurerm_resource_group" "rg" {
  name     = "ccscan-${var.environment_name}"
  location = var.azure_deployment_location
}
