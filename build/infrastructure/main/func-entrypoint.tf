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
module "func_entrypoint_messagearchive" {
  source                                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/function-app?ref=5.4.0"

  name                                      = "entrypoint"
  project_name                              = var.domain_name_short
  environment_short                         = var.environment_short
  environment_instance                      = var.environment_instance
  resource_group_name                       = azurerm_resource_group.this.name
  location                                  = azurerm_resource_group.this.location
  app_service_plan_id                       = data.azurerm_key_vault_secret.plan_shared_id.value
  application_insights_instrumentation_key  = data.azurerm_key_vault_secret.appi_instrumentation_key.value
  always_on                                 = true
  app_settings                              = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE           = true
    WEBSITE_RUN_FROM_PACKAGE                  = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE       = true
    FUNCTIONS_WORKER_RUNTIME                  = "dotnet-isolated"
    # Endregion
    STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING         = data.azurerm_key_vault_secret.st_market_operator_logs_primary_connection_string.value
    STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME            = data.azurerm_key_vault_secret.st_market_operator_logs_container_name.value
    STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME  = data.azurerm_key_vault_secret.st_market_operator_logs_archive_container_name.value
    COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING          = local.cosmos_db_connection_string
  }
  
  tags                                      = azurerm_resource_group.this.tags
}

module "kvs_app_message_archive_api_base_url" {
  source        = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/key-vault-secret?ref=5.4.0"

  name          = "app-message_archive-api-base-url"
  value         = "https://${data.func_entrypoint_messagearchive.default_hostname}/api/"
  key_vault_id  = data.azurerm_key_vault.kv_shared_resources.id

  tags          = azurerm_resource_group.this.tags
  depends_on = [
    module.func_entrypoint_messagearchive,
  ]
}