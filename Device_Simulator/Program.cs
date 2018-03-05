using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace simulatornet
{
    ///Sample IoT Hub Client simulator app
    class Program
    {
        static int Interval { get; set; } = 3000;

        static DeviceClient deviceClient;
        static string iotDeviceConnectionString = "Insert IoT Device connection string";

        static bool isRunning = true;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {

                Console.WriteLine("Argument index 0 not found: the Device IoT Hub connection string. Press any key to exit.");
                Console.ReadLine();
                return;
            }
            else
            {
                iotDeviceConnectionString = args[0].ToString();
            }

            Console.WriteLine("Simulated device\n");

            InitDeviceClient().Wait();
            SendDeviceToCloudMessagesAsync();

            Console.ReadLine();
        }

        static public async Task InitDeviceClient()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // DEV only! bypass certs validation
            mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            ITransportSettings[] settings = { mqttSetting };

            deviceClient = DeviceClient.CreateFromConnectionString(iotDeviceConnectionString, settings);
            await deviceClient.OpenAsync();

            Console.WriteLine($"Connected to IoT Hub with connection string [{iotDeviceConnectionString}]");

            //read twin  setting upon first load
            var twin = await deviceClient.GetTwinAsync();
            await onDesiredPropertiesUpdate(twin.Properties.Desired, deviceClient);

            //register for Twin desiredProperties changes
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, null);

            //callback for generic direct method calls
            //todo change the callback to actual method name and finalize callback implementation
            deviceClient.SetMethodHandlerAsync("Off", SwitchOff, null).Wait();
        }

        static Task onDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties.Count > 0)
                {
                    if (desiredProperties["Interval"] != null)
                        Interval = desiredProperties["Interval"];
                }

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }


        private static async void SendDeviceToCloudMessagesAsync()
        {
            double temperature = 25.00;
            int messageId = 1;

            while (true)
            {
                if (!isRunning)
                {
                    await Task.Delay(10000);
                    isRunning = true;
                    temperature = 25;
                }
                else
                    temperature += 0.20;

                MessageBody messageBody = new MessageBody();
                SensorInfo sensor = new SensorInfo();

                sensor.Temperature = temperature;
                messageBody.MessageId = messageId++;
                messageBody.Sensor = sensor;

                var messageString = JsonConvert.SerializeObject(messageBody);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(Interval);
            }
        }

        static Task<MethodResponse> SwitchOff(MethodRequest methodRequest, object userContext)
        {
            string result = "'DM call success'";
            if (isRunning)
            {
                Console.WriteLine($"Direct Method ({methodRequest.Name}) invoked...  ");
                Console.WriteLine("Returning response for method {0}", methodRequest.Name);
                isRunning = false;
            }
            return Task.FromResult(new MethodResponse(System.Text.Encoding.UTF8.GetBytes(result), 200));
        }
    }

    class MessageBody
    {
        public SensorInfo Sensor { get; set; }
        public DateTime TimeCreated { get; set; } = DateTime.Now;
        public int MessageId { get; set; }
    }

    class SensorInfo
    {
        public double Temperature { get; set; }
    }
}