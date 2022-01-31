terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = "=2.94.0"
    }
  }
}

provider "azurerm" {
  features {}
}

