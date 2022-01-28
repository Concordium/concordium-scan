# Terraforms for concordium scanner

[![Powered by - Terraform](https://img.shields.io/badge/Powered_by-Terraform-2ea44f?logo=terraform)](https://terraform.io)

## Requirements

- terraform
- azure cli

## Initial setup

- Sign in to azure via `az login`
- Run `terraform init` to initialize the project.
- Make sure to check variables in variables.tf.
- Create the file "secret.tf" (template in secret.txt) and provide user/pass for the postgres instance used for transaction logging.

Run `terraform apply` to build the node and all needed infrastructure.

##SSH access
The terraform sets up a local public/private key bar for SSH access to the server. Note that if you `terraform destroy` the platform and reapply it, you will invalidate existing keys in existence. Check with your colleagues.
When finished, you can SSH to the azure VM :
`ssh -i ./ssh.pem concNodeVMuser@<ip-address-of-vm>` (on linux - you may need to first run : `chmod 400 ssh.pem` )

## Terraform details

<!-- BEGIN_TF_DOCS -->
## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_azurerm"></a> [azurerm](#requirement\_azurerm) | =2.46.0 |
| <a name="requirement_random"></a> [random](#requirement\_random) | 3.0.0 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_azurerm"></a> [azurerm](#provider\_azurerm) | 2.46.0 |

## Modules

| Name | Source | Version |
|------|--------|---------|
| <a name="module_dev"></a> [dev](#module\_dev) | ./modules/dev | n/a |
| <a name="module_prod"></a> [prod](#module\_prod) | ./modules/prod | n/a |
| <a name="module_test"></a> [test](#module\_test) | ./modules/test | n/a |

## Resources

| Name | Type |
|------|------|
| [azurerm_container_registry.registry](https://registry.terraform.io/providers/hashicorp/azurerm/2.46.0/docs/resources/container_registry) | resource |
| [azurerm_resource_group.registry](https://registry.terraform.io/providers/hashicorp/azurerm/2.46.0/docs/resources/resource_group) | resource |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_deployment_location"></a> [deployment\_location](#input\_deployment\_location) | Specifies the deployment location for the resource group | `string` | `"northeurope"` | no |
| <a name="input_deployment_name"></a> [deployment\_name](#input\_deployment\_name) | Specifies the deployment name | `string` | `"concNodeVM"` | no |
| <a name="input_docker_registry_server"></a> [docker\_registry\_server](#input\_docker\_registry\_server) | Specifies the login server for the AZ Docker registry | `string` | `"ccscan.azurecr.io"` | no |
| <a name="input_http_port"></a> [http\_port](#input\_http\_port) | Specifies the internal port of the frontend server | `number` | `4220` | no |
| <a name="input_node_name"></a> [node\_name](#input\_node\_name) | Specifies the name the node reports to the network | `string` | `"FTB.Node"` | no |
| <a name="input_postgres_database_name"></a> [postgres\_database\_name](#input\_postgres\_database\_name) | Specifies the database name in use | `string` | `"concordium"` | no |
| <a name="input_postgres_port"></a> [postgres\_port](#input\_postgres\_port) | Speci1fies the port of the postgres db | `number` | `5432` | no |
| <a name="input_vm_size"></a> [vm\_size](#input\_vm\_size) | Specifies the size of the virtual machine. | `string` | `"Standard_B4MS"` | no |

## Outputs

No outputs.
<!-- END_TF_DOCS -->
