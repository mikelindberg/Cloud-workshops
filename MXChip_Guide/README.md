
# Lab 2.1
This guide will help you setup an MXChip and send data to Azure IoT Hub, Stream Analytics, Blob storage and Power BI. You will also learn about bi-directional IoT Hub communication by sending a cloud-to-device message.

![](images/d2c_c2d.png )

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
2. Go to IoT Hub and click IoT Devices
3. On the IoT Devices page click **"Add"** and give the IoT device a name e.g. **mxchip**
5. After you created a new device click refresh on the IoT Device page until the new device appears.
6. Select the device and copy the Connection string you will need this to setup the MXChip

### Setup MXChip and verify data
Follow the guide <https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-arduino-iot-devkit-az3166-get-started> to setup and connect the MXChip
1. Under your storage account go to Blobs, select your container, drill down to the lowest level of the folder structure and verify that there is a payload
4. Stop the Stream Analytics job

### Setup a Power BI Connection

1. Create a new output to the Stream Analytics job where the output type is Power BI and sign in. It is necessary to define dataset and table name.
2. Add the following code to your existing Stream Analytics query
```sql
SELECT
    IoTHub.ConnectionDeviceId,
    EventEnqueuedUtcTime,
    CAST( digiahubinput.temperature AS float) temperature
INTO
    pbioutput
FROM
    digiahubinput
```

3. Start the Stream analytics job
4. Create a table chart after the dataset appears in Power BI.
5. Save the chart to a report
6. Select "Pin visual" and create a new Dashboard
7. Add a new tile and select "Streaming Data"
8. Set EventEnqueuedUtcTime as the axis, IoTHub.ConnectionDeviceId as the legend and temperature as value
9. Verify that data is streaming in

### Create an Azure Function App

Go back to your Resource group and add an Azure Function App.
 
1. In the settings for the Function App select North Europe as location 
2. Create a new function (select HTTP trigger)

![](images/Create_NewFunction.PNG)

3. In the Function App run.csx file copy/paste the following code instead of the default code

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
    var messageItem = JsonConvert.DeserializeObject<Sensor[]>(jsonContent);

    var connectionString = "<Connection String>";
    // create IoT Hub connection.
    var serviceClient = ServiceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.TransportType.Amqp);
    var methodInvocation = new CloudToDeviceMethod("fanSpeed") { ResponseTimeout = TimeSpan.FromSeconds(10) };

    log.Info($"Ready to send DM to device {messageItem[0].DeviceId}");

    //send DM
    var response = await serviceClient.InvokeDeviceMethodAsync(messageItem[0].DeviceId, methodInvocation);
}
catch(System.Exception ex) {
  log.Info(ex.Message);
  return new HttpResponseMessage(HttpStatusCode.InternalServerError);
}

return new HttpResponseMessage(HttpStatusCode.OK);
}

class Sensor
{
  public string DeviceId { get; set; }
}
```

4. The **connectionString** variable should get the value from your IoT Hub/ Shared access policies/ service / connection string primary key

5. Under your Function go to View Files and add a new file called project.json
6. Copy the content below to the project.json file

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

7. Restart the Function

### Update your Stream Analytics job

1. Add the new function as an Output to your Stream Analytics job

![](images/StreamAnalytics_SetupFunction.PNG)

2. Update your Stream Analytics query by adding the script below (change the temperature if necessary)

```sql
    SELECT 
    IoTHub.ConnectionDeviceId DeviceId
    INTO
    [functionOutput]
    FROM 
    [iothubinput]
    WHERE iothubinput.Temperature > 30.0
```


Start the Stream Analytics job
