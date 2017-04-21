# Use X.509 Certificates to Authenticate with Azure IoT Hub

SimpleAzureIoTCerts is a C#-based command-line application that demonstrates how to authenticate with Azure IoT Hub using self-generated and self-signed X.509 certificates.

You can learn more about Azure IoT Hub authentication on the [Control access to IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#supported-x509-certificates) page located on the Azure documentation website.

## What's included?

SimpleAzureIoTCerts implements two key features:
* Add Device
  * Registers a new device with Azure IoT Hub
  * Gives you the option of associating X.509 certificates (primary and secondary) with a device at time of registration in three different ways. Our goal is to give you patterns that you can clone into your own implementation. 
    * Embedded: The build process embeds X.509 certificates into the SimpleAzureIoTCerts binary.  The "Resources" folders contains the CRT and PFX files that will be included.
    * Bundled: The build process copies standalone X.509 certificate files we've pre-generated into the same directory the SimpleAzureIoTCerts binary is output into.  You can then specify these files by name during runtime when prompted for certificate filenames: primary.crt, primary.pfx, secondary.crt, secondary.pfx.
    * Bring Your Own: Instead of using the pre-generated certificates files (CRT, PFX) we've described above, you use use OpenSSL to generate your own.  
* Delete (all) Devices - Deletes the first 1000 devices in an IoT Hub Registry.  This is particularly useful if you need to quickly delete all the devices in your registry.

## Setup a development environment

* Clone this GitHub repository.
* Install Visual Studio 2015.  Don't have it?  Download the free [Visual Studio Community Edition](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx)
* Optionally, install OpenSSL.  Don't want to?  No worries.  We've included self-generated and self-signed X.509 certificates that you can use to get started.
  * NOTE: Don't use any of these certifcates in a production or quasi-production (i.e. QA) environment!  They're here so you can focus on understanding the authentication flow between your client app / device, the Azure IoT NuGet packages, and IoT Hub - and not the particulars of OpenSSL.  

## Create an Azure IoT Hub 

* Sign in to the Azure Portal at https://portal.azure.com
* Create a new IoT Hub 
* Wait for the deployment of your new IoT Hub to complete.  
* Navigate to your IoT Hub Instance -> Settings / Shared access policies -> iothubowner
* Copy the value associated with "Connection string--primary key". It should come in this format:
  * HostName=your-iot-hub-name.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=AzureGeneratedGUID

## Build and Run SimpleAzureIoTCerts

* Open the cloned code from this repository in Visual Studio
* Update the variable azIotConnectionString in Program.cs with the value of "Connection string--primary key", which you copied from the Azure Portal
  * Or, leave this value empty and provide it at the command line like so: 
    * SimpleAzureIoTCerts <connection string>
* Optionally, you can supply the "Connection string--primary key" value when prompted by SimpleAzureIoTCerts
* Build and run the app: SimpleAzureIoTCerts
* Follow the prompts to add a new device and select which X.509 certificates you want to use.
  * Want to use your own certificates?  Follow the instructions in the section below.

## (Optional) Self-Generate and Self-Sign X.509 Certificates with OpenSSL 

It's easy to generate, sign, and supply your own X.509 certs to SimpleAzureIoTCerts.
* This command generates a "primary" cert with output of primary.crt and primary.pfx
  * openssl req -newkey rsa:2048 -nodes -keyout primary.key -x509 -days 365 -out primary.crt
  * openssl pkcs12 -export -out primary.pfx -inkey primary.key -in primary.crt 
* This command generates a "secondary" cert with output of secondary.crt and secondary.pfx
  * openssl req -newkey rsa:2048 -nodes -keyout secondary.key -x509 -days 365 -out secondary.crt
  * openssl pkcs12 -export -out secondary.pfx -inkey secondary.key -in secondary.crt 

## Questions and comments

We'd love to get your feedback about this sample. You can send your questions and suggestions to us in the Issues section of this repository.

Questions about Azure IoT development in general should be posted to [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub). Make sure that your questions or comments are tagged with [azure-iot-hub] or [azure-iot-sdk].

## Additional resources

* [IoT Hub Documentation](https://docs.microsoft.com/en-us/azure/iot-hub/)
* [Connect your simulated device to your IoT hub using .NET](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted)
* [Control Access to IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security)
* [Secure your IoT deployment](https://docs.microsoft.com/en-us/azure/iot-suite/iot-suite-security-deployment)
* [Microsoft Azure IoT SDK for .NET](https://github.com/azure/azure-iot-sdk-csharp)

## Copyright

Copyright (c) 2017 Tam Huynh. All rights reserved. 

### Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**
