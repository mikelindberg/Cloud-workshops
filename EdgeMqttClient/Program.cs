namespace EdgeMqttClient
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System.Collections.Generic;     // for KeyValuePair<>
    using Microsoft.Azure.Devices.Shared; // for TwinCollection
    using Newtonsoft.Json;                // for JsonConvert
    using uPLibrary.Networking.M2Mqtt;
    using uPLibrary.Networking.M2Mqtt.Exceptions;
    using uPLibrary.Networking.M2Mqtt.Messages;
    using uPLibrary.Networking.M2Mqtt.Session;
    using uPLibrary.Networking.M2Mqtt.Utility;
    using uPLibrary.Networking.M2Mqtt.Internal;

    class Program
    {
        //Connection string for IoT Hub - This will be read from the Edge hub
        static string connectionString = "Insert IoT Device connection string";

        //Client to write to IoT Hub
        static DeviceClient ioTHubModuleClient;

        //MQTTClient
        static MqttClient mqttClient;

        //MQTT Server - This setting will be read from the Module Twin Properties
        static string mqttServer = "";

        //MQTT topics - This setting will be read from the Module Twin Properties
        static string topic;

        static void Main(string[] args)
        {
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
            connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!bypassCertVerification) InstallCert();
            Init(connectionString, bypassCertVerification).Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }


        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            //Set up MQTT client to IoT Hub
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            if (bypassCertVerification)
            {
                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub MQTT module client initialized.");
            
            //Set up MQTT client for reading from queue
            //Read module twin properties
            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            var moduleTwinCollection = moduleTwin.Properties.Desired;

            mqttServer = moduleTwinCollection.Contains("MQTTServer") ? moduleTwinCollection["MQTTServer"] : "";
            Console.WriteLine("MQTT Server: " + mqttServer);

            topic = moduleTwinCollection.Contains("Topic") ? moduleTwinCollection["Topic"] : "";
            Console.WriteLine("Topic: " + topic);

            //Try and initialize MQTT client
            bool clientInitialized = InitializeMqttClient();
            if (clientInitialized == false)
            {
                Console.WriteLine("Unable to initialize MQTT client");
            }
        }

        //Initialize the MQTT Client
        static bool InitializeMqttClient()
        {
            bool clientInitialized = false;

            if (String.IsNullOrEmpty(mqttServer))
            {
                return clientInitialized;
            }

            try
            {
                // create client instance 
                mqttClient = new MqttClient(mqttServer);

                // register to message received 
                mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                string clientId = Guid.NewGuid().ToString();
                mqttClient.Connect(clientId);

                // subscribe to the topic with QoS 0
                mqttClient.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

                clientInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return clientInitialized;
        }

        static async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (mqttClient != null && mqttClient.IsConnected == true)
            {
                var str = System.Text.Encoding.Default.GetString(e.Message);
                MessageBody msgBody = new MessageBody();

                msgBody.topic = e.Topic;
                msgBody.payload = str;
                msgBody.timeCreated = DateTime.Now;
                var messageString = JsonConvert.SerializeObject(msgBody);

                if (!string.IsNullOrEmpty(messageString))
                {
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    await ioTHubModuleClient.SendEventAsync("mqttclient", message);
                    Console.WriteLine("Sending message: " + messageString);
                }
            }
            else
            {
                bool clientInitialized = InitializeMqttClient();
                if (clientInitialized == false)
                {
                    Console.WriteLine("Unable to initialize MQTT client");
                }
            }
        }
    }

    class MessageBody
    {
        public string topic { get; set; }
        public string payload { get; set; }
        public DateTime timeCreated { get; set; }
    }
}
