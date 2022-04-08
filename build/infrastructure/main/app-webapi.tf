# Copyright 2020 Energinet DataHub A/S
#
# Licensed under the Apache License, Version 2.0 (the "License2");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
resource "azurerm_app_service" "webapi" {
  name                = "app-webapi-${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  app_service_plan_id = module.plan_webapi.id

  site_config {
    linux_fx_version = "DOTNETCORE|5.0"
    dotnet_framework_version = "v5.0"
    cors {
      allowed_origins = ["*"]
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"                    = "${data.azurerm_key_vault_secret.appi_instrumentation_key.value}"
    "FRONTEND_OPEN_ID_URL"                              = "${data.azurerm_key_vault_secret.frontend_open_id_url.value}"
    "FRONTEND_SERVICE_APP_ID"                           = "${data.azurerm_key_vault_secret.frontend_service_app_id.value}"
    "STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING"         = "${data.azurerm_key_vault_secret.st_market_operator_logs_primary_connection_string.value}"
    "STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME"            = "${data.azurerm_key_vault_secret.st_market_operator_logs_container_name.value}"
    "STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME"  = "${data.azurerm_key_vault_secret.st_market_operator_logs_archive_container_name.value}"
    "COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING"          = local.cosmos_db_connection_string
  }

  tags              = azurerm_resource_group.this.tags

  lifecycle {
    ignore_changes = [
      # Ignore changes to tags, e.g. because a management agent
      # updates these based on some ruleset managed elsewhere.
      tags,
    ]
  }
}

module "plan_webapi" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/app-service-plan?ref=5.1.0"

  name                  = "webapi"
  project_name          = var.domain_name_short
  environment_short     = var.environment_short
  environment_instance  = var.environment_instance
  resource_group_name   = azurerm_resource_group.this.name
  location              = azurerm_resource_group.this.location
  kind                  = "Linux"
  reserved              = true
  sku                   = {
    tier  = "Basic"
    size  = "B1"
  }

  tags                = azurerm_resource_group.this.tags
}

module "kvs_app_message_archive_api_base_url" {
  source        = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/key-vault-secret?ref=5.4.0"

  name          = "app-message-archive-api-base-url"
  value         = "https://${module.func_entrypoint_messagearchive.default_hostname}"
  key_vault_id  = data.azurerm_key_vault.kv_shared_resources.id

  tags          = azurerm_resource_group.this.tags
}