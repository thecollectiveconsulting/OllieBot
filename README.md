# Ollie

## Deployment Guide

### Prerequisites

To begin we will need:

- An Azure subscription with the correct permissions, where you can create the following resources:
  - App Service
  - App Service Plan
  - Bot channels registration
- A Copy of the Ollie github repo

### Register Azure AD application

Register two Azure AD Applications in your tenant's directory.

1. Log in to the Azure Portal for your subscription, and go to “App registrations”. You can use the following link: https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps
2. Click on "New registration", and create an Azure AD application.

   1. **Name**: The name of your Teams app - if you are following the template for a default deployment, we recommend "Ollie".
![image](https://user-images.githubusercontent.com/83395176/135573524-3964d067-4a61-4871-bb6b-0754f9238320.png)
   2. **Supported account types**: Select "Accounts in any organizational directory"
![image](https://user-images.githubusercontent.com/83395176/135573558-cf28ed43-a650-48b5-a34f-c63c5ed11749.png)
   3. Leave the "Redirect URI" blank

3. Click on the **Register** button
4. When the app is registered, you'll be taken to the app's **Overview** page. Copy the Application (client) ID; we will need it later. 
5. Navigate to the **Certificates & secrets** section, this can be found in the sidebar in the Manage section. Click on **+ New client secret**. Add a description for the secret and select an expiry time. Click **Add**.
6. When the secret is generated, copy the value and keep it safe. We will need it later.
7. Go back to “App registrations”, then repeat steps 2-6 to create another Azure AD application for the configuration app.

   1. Name: The name of the app config that can connect to the Log Analytics API and the Graph API, for example, “Ollie Configuration”.
   2. Supported account types: Select "Account in this organizational directory only"
   3. Leave the "Redirect URI" field blank for now.   
8. Add Api Permissions to Configuration app

   1. navigate to the configuration app
   2. Select API Permissions under the Manage blade
   3. Add the following permissions

      1. Log Analytics API

         1. Data.Read

      2. Microsoft Graph

         1. ThreatIndicators.ReadWrite.OwnedBy

   4. Click **Grant admin consent for ...**

9. grant Access to the Azure Sentinel resource.
  
   1. Navigate to the Azure Sentinel Log Analytics workspace
   2. Select **Access Control(IAM)**
   3. Add the configuration app, with the **Azure Sentinel Contributer** role

### Deploy ARM to your Azure subscription

1. Click on the **Deploy to Azure** button, this will help you deploy all the needed resources.

   [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fthecollectiveconsulting%2FOllie%2Fmain%2FARMTemplates%2FazureDeploy.json)

2. Select a subscription and resource group, we recommend creating a new resource group.
3. Fill in the required fields:

  1. **Serviceplan Name**: the name of the serviceplan
  2. **Key Vault Name**: the name of the keyvault, where we will store the secrets
  3. **Bot Name**: The name of the bot.
  4. **Msa App Id**: The application (client) ID of the first app registration that you created before
  5. **Msa App Secret**: The client secret of the first app registration that you created before

> To create the App Service, we use the name of the Bot. So the name of the Bot must be available.

4. Click **Review and Create** and review all the provided information
5. Click **Create**

### Deploy Bot

Now that we've created all the necessary resources, it's time to deploy the bot. The following steps describe what to do.

1. Start by downloading this repository.
2. Navigate to the **appsettings.json** file, which can be found in Solution/Ollie
3. Fill in all the required information.

   - **MicrosoftAppId**: The application (client) ID of the first app registration that you created before
   - **MicrosoftAppPassword**: The client secret of the first app registration that you created before
   - **Tenant**: TenantID
   - **ClientId**: Client ID of the Configuration App
   - **ClientSecret**: Secret of the Configuration App
   - **WorkspaceId**: Id of the Azure Sentinel Log Analytics workspace
   - **SubscriptionId**: Azure subscription ID
   - **ResourcegroupName**: Resourcegroupname of where de Azure sentinel workspace is located.
   - **WorkspaceName**: Name of the Azure Sentinel workspace

5. Next navigate to the **manifest.json** file, which can be found in Solution/Ollie/Manifest
6. Fill in the id parameter.
7. Deploy the bot to the **App Service** we've previously created.

To deploy the bot you can use Visualstudio or follow the instruction from the [documentation](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-deploy-az-cli?view=azure-bot-service-4.0&tabs=csharp#deploy-the-bot-to-azure)

### Create the Teams app package

You have to create a single Teams app package in order to add Ollie to Teams.

1. Open the Manifest\manifest.json file in a text editor.
2. Change the <MicrosoftAppId> placeholder to your Azure AD application's ID from above. This is the same GUID that you entered in the template under "Msa App Id".
3. Create a ZIP package with the manifest.json,color.png, and outline.png. Make sure there are no nested folders inside the ZIP Package.


### Add Ollie to teams

Follow these [instruction](https://docs.microsoft.com/en-us/MicrosoftTeams/manage-apps?toc=%2Fmicrosoftteams%2Fplatform%2Ftoc.json&bc=%2Fmicrosoftteams%2Fplatform%2Fbreadcrumb%2Ftoc.json#upload-an-app-package)
