# Lab 2.1
For this lab we will use a device simulator to send "Temperature" measurements to an IoT Hub, save the messages to blob storage and report back to the device via an Azure Function if the “Temperature becomes too hot.

![](images/Architecture.png )

You can create the architecture in 2 ways
By following a step-by-step guide
By clicking this button (insert button)

## Step-by-step guide
Sign in to <http://portal.azure.com>

### Create Resource group
On the left pane choose Resource Groups
Click the "+ Add" button to create a new Resource group
Give the Resource group a name and choose North Europe as region

![](images/Create_resourcegroup.PNG)

### Create IoT Hub
You might need to the refresh icon in Azure to see your new Resource group
Select your new Resource group and then click the "+ Add" button to add an IoT hub
Search for IoT Hub and then click “Create”

![](images/Search_IotHub.PNG "Search IoT Hub")

IoT Hub setting
Give the IoT Hub a name 
Select the F1 pricing tier
Use the Resource group you just created
Location North Europe

![](images/Create_IotHub.PNG)
 
Go back to your Resource group and verify that the IoT Hub there

### Create Blob storage
Add a Storage account to your Resource group
 
Storage account settings 
* Give the Storage account a name
* Select your Resource group
* Location North Europe
* Leave everything else with default settings

![](images/Create_StorageAccount.PNG)

### Create Stream Analytics Job

Add a Stream Analytics job to your Resource group
Stream Analytics Job setting
* Give the Stream Analytics job a name
* Use your Resource group
* Location North Europe
* Hosting environment (Cloud)
 
Go back to your Resource group and select the Stream Analytics job
Click Input to create a new input for the Stream Analytics job
 
Click the “Add Stream Input” button, select IoT Hub and setup it up to receive messages from your IoT Hub

![](images/StreamAnalytics_CreateInput.PNG)

Give the Stream Analytics job an output and select Blob storage as the output type

![](images/StreamAnalytics_SetupBlobStorage.PNG)
 
Edit the Stream Analytics Query by change the input to your IoT Hub name and the output to your Blob Storage name

![](images/StreamAnalytics_Query1.PNG)
 
1. Start the Stream Analytics job from the Stream Analytics Overview page
2. Go and [download the Git repo from](https://github.com/mikelindberg/Cloud-workshops)
3. Go to IoT Hub and click IoT Devices
4. On the IoT Devices page click   and give the IoT device a name e.g. Simulator
5. After you created a new device click refresh on the IoT Device page until the new device appears.
6. Select the device and copy the Connection string and add it to the iotDeviceConnectionString in the Simulator app
7. Start the solution and verify that the simulator is sending messages to:
8. IoT Hub – Look on the IoT Hub Overview page and see “Usage”
9. Stream Analytics Job – Look on the Stream Analytics Overview page and see “Monitoring” there should be events coming in
10. Under your storage account go to Blobs ? select your container ? drill down to the lowest level of the folder structure and verify that there is a payload
11. Stop the Stream Analytics job
12. Add Function App to your Resource group.
 
In the settings for the Function App select North Europe as location
 
Create a new function (select HTTP trigger) under the Function App and copy paste the following code instead of the default code

```csharp
#r "Newtonsoft.Json"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
  try {   
    var content = req.Content;
    string jsonContent = content.ReadAsStringAsync().Result;
    log.Info($"Payload: {jsonContent}");
    var messageItem = JsonConvert.DeserializeObject<MessageBody[]>(jsonContent);

    var connectionString = "Insert IoT Hub Connection string";
    // create IoT Hub connection.
    var serviceClient = ServiceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.TransportType.Amqp);
    var methodInvocation = new CloudToDeviceMethod("Off") { ResponseTimeout = TimeSpan.FromSeconds(10) };

    log.Info($"Ready to send DM to device {messageItem[0].Sensor.DeviceId}");

    //send DM
    var response = await serviceClient.InvokeDeviceMethodAsync(messageItem[0].Sensor.DeviceId, methodInvocation);
}
catch(System.Exception ex) {
  log.Info(ex.Message);
  return new HttpResponseMessage(HttpStatusCode.InternalServerError);
}

return new HttpResponseMessage(HttpStatusCode.OK);
}

class MessageBody
{
	public SensorInfo Sensor { get; set; }
	public DateTime TimeCreated { get; set; } = DateTime.Now;
	public int MessageId { get; set; }
}

class SensorInfo
{
  public string DeviceId { get; set; }
  public double Temperature { get; set; }
}
```

Under your Function go to View Files and add a new file called project.json

Make sure that the content of the file is like below

```json
{
  "frameworks": {
  "net46":{
    "dependencies": {
      "Microsoft.Azure.Devices": "1.4.1",
      "Microsoft.Azure.Amqp": "2.0.0"
      }
    }
  }
}
```

Start the Function

Start the Stream Analytics job (wait with next step until it is started)
Run the Device Simulator
Verify that the “Temperature” reaches 30+ degrees, then stops for 10 seconds and then restarts at 25 degrees
