## Dotnet core sample: Device simulator for IoT Hub Client

How to run this sample:

1. Make sure you have an IoT Hub with a device created, copy the connection string for the device and replace below `[yourdeviceconnectionstring]`.
1. Open a console and navigate to this folder.
1. Type `dotnet restore` to restore the packages.
1. Type `dotnet run [yourdeviceconnectionstring]` to build and run the console app.
1. You will see messages sent to IoT Hub.
1. There is a Device Twin, Desired Property used: Interval. You can set a new interval to send telemetry by updating the Desired Property like this on your favorite tool (the portal, Device Explorer, etc):
````
"properties": {
    "desired": {
      "Interval" : 3000,
      "$metadata": {
        "$lastUpdated": "2018-02-08T09:57:34.1979575Z"
      },
      "$version": 1
    },
    "reported": {
      "$metadata": {
        "$lastUpdated": "2018-02-08T09:57:34.1979575Z"
      },
      "$version": 1
    }
  }
````

Optionally implement a direct method.

> Note: this is for dev only sample. 



