variable "azure_deployment_location" {
  description = "Specifies the deployment location for the resource group"
  default ="northeurope"
}

variable "environment_name" {
  description = "The name of the environment is used when naming resource"
  type = string
}
