#r "Newtonsoft.Json"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    try
    {
        var content = req.Content;
        string jsonContent = content.ReadAsStringAsync().Result;
        log.Info($"Payload: {jsonContent}");
        var messageItem = JsonConvert.DeserializeObject<Sensor[]>(jsonContent);

        var connectionString = GetEnvironmentVariable("Azure_IoT_ConnectionString");
        // create IoT Hub connection.
        var serviceClient = ServiceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.TransportType.Amqp);
        var methodInvocation = new CloudToDeviceMethod("Off") { ResponseTimeout = TimeSpan.FromSeconds(10) };

        log.Info($"Ready to send DM to device {messageItem[0].DeviceId}");

        //send DM
        var response = await serviceClient.InvokeDeviceMethodAsync(messageItem[0].DeviceId, methodInvocation);

    }
    catch (System.Exception ex)
    {
        log.Info(ex.Message);
        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
    }
    
    return new HttpResponseMessage(HttpStatusCode.OK);
}

class Sensor
{
  public string DeviceId { get; set; }
}
public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}
