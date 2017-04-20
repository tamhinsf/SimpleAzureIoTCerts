using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SimpleAzureIoTCerts
{
    class Program
    {
        static RegistryManager registryManager;

        // Optionally, embed the connection string to your Azure IoT Hub instance 
        // into the variable azIotConnectionString (below).  To keep things simple,
        // we suggest you use the string associated with the automatically created
        // user named "iothubowner"

        // Alternatively, you can leave azIotConnectionString blank and supply it 
        // at run-time after you build and launch the app.

        // You can get the connection string for "iothubowner" by navigating to https://portal.azure.com 
        // your iot hub instance -> settings -> shared access policy -> iothubowner -> connection string - primary key
        // it will look something like this: 
        // HostName=your-iot-hub-name.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=AzureGeneratedGUID

        static string azIotConnectionString = "";

        // programatically derive this from azIotConnectionString

        static string azIotHubHostname = ""; 

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("**************************************************");
            Console.WriteLine("*             Simple Azure IoT Certs             *");
            Console.WriteLine("**************************************************");
            Console.WriteLine("");
            Console.WriteLine("This app demonstrates how to add a device to your Azure IoT Hub's Registry.");
            Console.WriteLine("Optionally, you can associate X509 certificates with your device's registry entry,");
            Console.WriteLine("which you can then use for subsequent operations requiring authentication");

            // you can supply your IoT Hub connection string as a command line argument (here)
            // or enter it interactively

            if (args.Length > 0)
            {
                azIotConnectionString = args[0];
            }

            // allow the user to interactively enter their Azure IoT Hub connection string

            if (azIotConnectionString == "")
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
                Console.WriteLine("You need to supply a connection string to your Azure Iot Hub instance!");
                Console.WriteLine("You can do this in Program.cs in the variable azIotConnectionString,");
                Console.WriteLine("supply it as a command line parameter (i.e. SimpleAzureIotCerts <connection string>),");
                Console.Write("or enter it here: ");
                azIotConnectionString = Console.ReadLine().Trim();
                if(azIotConnectionString == "")
                {
                    Console.WriteLine("You can get your Azure IoT Hub connection string from the Azure Portal");
                    Console.WriteLine("at https://portal.azure.com/ and then run this app again");
                    Environment.Exit(1);
                }
            }

            string[] connectionStringParts = azIotConnectionString.Split(';');
            azIotHubHostname = connectionStringParts[0].Split('=')[1];
            Console.WriteLine("********************************************************");
            Console.WriteLine(" IoT Hub Hostname is " + azIotHubHostname);
            Console.WriteLine("********************************************************");

            string commandString = string.Empty;

            while (!commandString.Equals("Exit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Enter command (add | delete (all) | exit ) > ");
                commandString = Console.ReadLine();

                switch (commandString.ToUpper())
                {
                    case "ADD":
                        AddDeviceAsync().Wait();
                        break;
                    case "DELETE":
                        DeleteAllDevicesAsync().Wait();
                        break;
                    case "EXIT":
                        Console.WriteLine("Bye!"); ;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command.");
                        break;
                }

            }
        }

        // delete first 1000 devices found in the IoT Hub registry
        
        private static async Task DeleteAllDevicesAsync()
        {
            Console.WriteLine("This will delete the first 1000 devices found in the IoT Hub registry.");
            Console.WriteLine("You will have to run this operation multiple times if you have more than 1000");
            Console.WriteLine("devices in your IoT Hub registry!");
            Console.Write("Enter y to confirm, anything else to abort> ");
            var confirmCommand = Console.ReadLine().ToLower();
            if (confirmCommand.StartsWith("y"))
            {
                registryManager = RegistryManager.CreateFromConnectionString(azIotConnectionString);
                IEnumerable<Device> allDevices = await registryManager.GetDevicesAsync(1000);
                try
                { 
                    await registryManager.RemoveDevices2Async(allDevices);
                    Console.WriteLine("Deletion completed");
                }
                catch
                {
                    Console.WriteLine("No devices to delete");
                }
            }
            else
            {
                Console.WriteLine("Aborting delete");
            }
        }


        // add a device into the Iot Hub registry

        private static async Task AddDeviceAsync()
        {
            Console.WriteLine("Add a new device");
            Console.Write("Enter your new device id or an existing device id to see its device key: ");
            string deviceId = Console.ReadLine();

            Console.Write("Would you like to associate X509 certificates with your device (y|n)? ");
            var useCert = Console.ReadLine().ToLower();

            registryManager = RegistryManager.CreateFromConnectionString(azIotConnectionString);
            Device device;

            if (!useCert.StartsWith("y"))
            {
                try
                {
                    device = await registryManager.AddDeviceAsync(new Device(deviceId));
                    Console.WriteLine("Device added " + device.Id);
                }
                catch (DeviceAlreadyExistsException)
                {
                    device = await registryManager.GetDeviceAsync(deviceId);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Device with this ID already exists " + device.Id);
                    if (device.Authentication.SymmetricKey.PrimaryKey == null)
                    {
                        Console.WriteLine("Device was previously registered using X509 certificates and not symmetric keys");
                        ShowX509Thumbprints(device);
                    }
                    else
                    {
                        ShowSymmetricalKeys(device);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }

                ShowSymmetricalKeys(device);

                // setup a device authentication config which you will pass to device client (below)
                var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(device.Id, device.Authentication.SymmetricKey.PrimaryKey);

                // you can use the secondary authentication key as well
               //var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(device.Id, device.Authentication.SymmetricKey.SecondaryKey);

                // create a device client for sending a message
                var deviceClient = DeviceClient.Create(azIotHubHostname, authMethod, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

                // TransportType supports different options
                //var deviceClient = DeviceClient.Create(azIotHubHostname, authMethod, Microsoft.Azure.Devices.Client.TransportType.Http1);

                // send the message
                SendTelemetryMessage(deviceClient, device);
            }
            else
            {

                // associate X509 certificates with your device

                // below are the two OpenSSL commands you can use to generate your own 
                // self-signed X509 certs.  accept the defaults when prompted
                // note: our code assumes that you don't supply a password for your pfx file 
                // when prompted when running the second "openssl" command

                // This command generates a "primary" cert with output of primary.crt and primary.pfx

                // openssl req -newkey rsa:2048 -nodes -keyout primary.key -x509 -days 365 -out primary.crt
                // openssl pkcs12 -export -out primary.pfx -inkey primary.key -in primary.crt

                // This command generates a "secondary" cert with output of secondary.crt and secondary.pfx

                // openssl req -newkey rsa:2048 -nodes -keyout secondary.key -x509 -days 365 -out secondary.crt
                // openssl pkcs12 -export -out secondary.pfx -inkey secondary.key -in secondary.crt

                // primary and secondary certs (crt and pfx files) have been setup as embedded build resources in the 
                // Resource folder.  You can choose to use them at runtime

                // we've also included primary and secondary certificate files that will be output
                // upon build to the same directory as the SimpleAzureIotCerts.exe executable
                // they are called primary.crt and primary.pfx, and secondary.crt and secondary.pfx
                // you can choose to use them at runtime by specifying their filename

                // of course, you also have the option of specifying your own generated crt and pfx files

                X509Certificate2 primaryCert;
                X509Certificate2 secondaryCert;
                X509Certificate2 primaryCertSecret;
                X509Certificate2 secondaryCertSecret;

                string primaryCrtFile = "";
                string primaryPfxFile = "";
                string secondaryCrtFile = "";
                string secondaryPfxFile = "";


                Console.WriteLine("We've embedded primary and secondary certificate files (crt and pfx) into this app");
                Console.WriteLine("to make this demo easy.  But you can specify your own crt and pfx files.");
                Console.Write("Use the embedded certificates (y|n)? ");
                var certChoice = Console.ReadLine().ToLowerInvariant();

                if (certChoice.StartsWith("y"))
                {
                    primaryCert = new X509Certificate2(Helpers.LoadEmbeddedFile("primary-embedded.crt"), "");
                    primaryCertSecret = new X509Certificate2(Helpers.LoadEmbeddedFile("primary-embedded.pfx"), "");
                    secondaryCert = new X509Certificate2(Helpers.LoadEmbeddedFile("secondary-embedded.crt"), "");
                    secondaryCertSecret = new X509Certificate2(Helpers.LoadEmbeddedFile("secondary-embedded.pfx"), "");
                }
                else
                {
                    // we've also included primary and secondary certificate files that will be output
                    // upon build to the same directory as the SimpleAzureIotCerts.exe executable

                    Console.Write("Primary certificate CRT filename (i.e. primary.crt): ");
                    primaryCrtFile = Console.ReadLine().Trim();
                    primaryCert = new X509Certificate2(primaryCrtFile, "");

                    Console.Write("Primary certificate PFX filename (i.e. primary.pfx): ");
                    primaryPfxFile = Console.ReadLine().Trim();
                    primaryCertSecret = new X509Certificate2(primaryPfxFile, "");

                    Console.Write("Want to provide a secondary certificate (y|n)? ");
                    var registerSecondaryCert = Console.ReadLine().ToLowerInvariant().Trim();
                    if (registerSecondaryCert.StartsWith("n"))
                    {
                        Console.WriteLine("OK.  We'll just make your secondary certificate the same as your primary");
                        secondaryCrtFile = primaryCrtFile;
                        secondaryCert = new X509Certificate2(secondaryCrtFile, "");
                        secondaryPfxFile = primaryPfxFile;
                        secondaryCertSecret = new X509Certificate2(secondaryPfxFile, "");
                    }
                    else
                    {
                        Console.Write("Secondary certificate CRT filename (i.e. secondary.crt): ");
                        secondaryCrtFile = Console.ReadLine().Trim();
                        secondaryCert = new X509Certificate2(secondaryCrtFile, "");
                        Console.Write("Secondary certificate PFX filename (i.e. secondary.pfx): ");
                        secondaryPfxFile = Console.ReadLine().Trim();
                        secondaryCertSecret = new X509Certificate2(primaryPfxFile, "");

                    }
                }
                Console.WriteLine("Locally read Primary X509 Thumbprint " + primaryCert.Thumbprint);
                Console.WriteLine("Locally read Secondary X509 Thumbprint " + secondaryCert.Thumbprint);

                try
                {
                    device = await registryManager.AddDeviceAsync(new Device(deviceId)
                    {
                        Authentication = new AuthenticationMechanism()
                        {
                            X509Thumbprint = new X509Thumbprint()
                            {
                                PrimaryThumbprint = primaryCert.Thumbprint,
                                SecondaryThumbprint = secondaryCert.Thumbprint,
                            }
                        }
                    });
                    Console.WriteLine("Device added " + device.Id);
                }
                catch (DeviceAlreadyExistsException)
                {
                    device = await registryManager.GetDeviceAsync(deviceId);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Device with this id exists " + device.Id);
                    if (device.Authentication.X509Thumbprint.PrimaryThumbprint == null)
                    { 
                        Console.WriteLine("Device was previously registered using symmetric keys and not X509 certificates");
                        ShowSymmetricalKeys(device);
                    }
                    else
                    {
                        ShowX509Thumbprints(device);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }

                ShowX509Thumbprints(device);

                // setup a device authentication config which you will pass to device client (below)
                var authWithPrimaryPfx = new DeviceAuthenticationWithX509Certificate(device.Id, primaryCertSecret);

                // you can use the secondary authentication key (certificate) as well
                //var authWithSecondaryPfx = new DeviceAuthenticationWithX509Certificate(device.Id, secondaryCertSecret);
            
                // create a device client for sending a message
                var deviceClient = DeviceClient.Create(azIotHubHostname, authWithPrimaryPfx, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

                // TransportType supports different options
                //var deviceClient = DeviceClient.Create(azIotHubHostname, authWithPrimaryPfx, Microsoft.Azure.Devices.Client.TransportType.Http1);

                // send the message
                SendTelemetryMessage(deviceClient, device);
            }

            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-identity-registry

            Console.WriteLine("Azure IoT Hub has associated this unique value (Generation ID) with your device: " + device.GenerationId);

        }

        private static void ShowX509Thumbprints(Device device)
        {
            Console.WriteLine("Your certificate thumbprints as retreived from Azure are: " + device.Authentication.X509Thumbprint.PrimaryThumbprint + " " + device.Authentication.X509Thumbprint.SecondaryThumbprint);
        }

        private static void ShowSymmetricalKeys(Device device)
        {
            Console.WriteLine("Your certificate thumbprints as retreived from Azure are: " + device.Authentication.SymmetricKey.PrimaryKey + " " + device.Authentication.SymmetricKey.SecondaryKey);
        }

        // send a simple message after device registration has completed to demonstrate
        // that registration and authentication was successful

        private static void SendTelemetryMessage(DeviceClient deviceClient, Device device)
        {
            var telemetryDataPoint = new
            {
                deviceId = device.Id,
                windSpeed = "100"
            };

            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

            Console.WriteLine("You've added a new device.  We'll now try to send a telemetry message");

            try
            {
                deviceClient.SendEventAsync(message).Wait();
                Console.WriteLine("Telemetry message sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception upon sending message is " + ex);
            }

        }

    }
}
