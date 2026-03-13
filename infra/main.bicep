@description('Name of the web app. Must be globally unique.')
param webAppName string = 'mockstar'

@description('Name of the parser API app. Must be globally unique.')
param parserAppName string = 'heat-parser'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('App Service Plan SKU.')
@allowed(['F1', 'B1', 'B2', 'B3'])
param appServicePlanSku string = 'B1'

@description('SQLite connection string for the heat database (e.g. Data Source=/home/data/mockstar.db).')
@secure()
param heatDbConnectionString string = 'Data Source=/home/data/mockstar.db'

var planName = '${webAppName}-plan'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource parserApp 'Microsoft.Web/sites@2023-01-01' = {
  name: parserAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
    httpsOnly: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'ParserApi__BaseUrl'
          value: 'https://${parserApp.properties.defaultHostName}/'
        }
      ]
      connectionStrings: [
        {
          name: 'HeatDb'
          connectionString: heatDbConnectionString
          type: 'Custom'
        }
      ]
    }
    httpsOnly: true
  }
}

@description('Hostname of the web app.')
output webAppHostname string = webApp.properties.defaultHostName

@description('Hostname of the parser API.')
output parserAppHostname string = parserApp.properties.defaultHostName
