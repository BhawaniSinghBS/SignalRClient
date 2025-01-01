# SignalRClient
Nuget package for SignalR Client library


1) the connection can be made with signalr hub using following function
  await SignalRClient.ConnectToHub(signalRHubURL: URLorIP,
                                                    appName: <appname as mentioned>, 
                                                    jwtToken: <jwt token>,
                                                    tagsToSubsCribe: new List<string> { SignalRClient.GetDebugTag() }
                                                    functionToHandleSpatsAtClientApplication: HandleMessageReceived // this function will receive data from hub on the subcribed tags
                                                    );

        private async void HandleMessageReceived(string tag, string jsonData)
        {//this function will receive the data from signal hub , this will be triggred from the hub auomatically
            LastTimeDataReceived = DateTime.UtcNow;
            if (string.IsNullOrEmpty(jsonData) || jsonData.Length < 3 || tag =="ping")
            {
                return;
            }
            if (string.IsNullOrEmpty(jsonData) || jsonData.Length < 3)
            {
                return;
            }
            SignalrClientDataModal dataReceived = SignalRClient.GetDataObject<SignalrClientDataModal>(jsonData) ?? new SignalrClientDataModal();
        }
     

3) To reccive data we need to subscribe the respective tag of data the TAG CAN BE GENERATED AND SUBSCRIBED by using functions in SignalrClent.cs
    to generate tag use this function:
    string respectivedataTag = SignalRClient.GetEncryptedTag(Enums.SignalRReceiveType receiveType, string etityId, out string nonEncruptedTagOnlyToDebug, string encruptionKey = ClientSettings.ClientSettings.TagEncruptionKey, int timeInterValForRecivingData_inMS = (int)Enums.SignalRDataReciveTimeInterval.Milliseconds200, bool getRadisTagWithStar = false);
    
4) after generating tag send this tag to hub for subscription using following funciton :
    List<string> tagsToSubsCribeInHub = new (){respectivedataTag}
    await SignalRClient.SubscribeToTagsInHub(tagsToSubsCribeInHub);

5) To send data via signalr hub following funcion can be used:  // sending via hub requries a hosted hub on server
   SignalRClient.SendMessage(string hubCompleteurl, string appName, List<string> tagsOnWhichToSend, object nonSerialezedDataToSend, string jwtToken, List<string> specificConnectionIds_ToSend = null)

   Example:
            DataModel DataModel = new DataModel();//actual data model
            SignalrClientDataModal dataToSend = new SignalrClientDataModal(); 
            dataToSend.DeviceId = DataModel.Id;
            dataToSend.ModalTypeName = DataModel.GetType().Name;
            dataToSend.DataModal = DataModel;

            // generating tag with data deviceId
            string expectedTagToBroadCastDataAsModal = SignalRClient.SignalRClient.GetEncryptedTag(
                    receiveType: SignalRClient.Enums.SignalRReceiveType.DataModel,
                    etityId: deviceId.ToString(),
                    out string nonEncruptedTagOnlyToDebugForModal,
                    encruptionKey: SignalRClient.ClientSettings.ClientSettings.TagEncruptionKey,
                    timeInterValForRecivingData_inMS: (int)SignalRClient.Enums.SignalRDataReciveTimeInterval.Milliseconds200
                    ).Trim();

            // usually every data is send on 2 tags one with device id task and another with 0 device id tag so that if any client subscribe 0 tag the tag will receive all data for the respective data type
            string tagOnWhichSendAllDevicesModal = SignalRClient.SignalRClient.GetEncryptedTag( 
                    receiveType: SignalRClient.Enums.SignalRReceiveType.DataModel,
                    etityId: 0.ToString(),
                    out string nonEncruptedTag,
                    encruptionKey: SignalRClient.ClientSettings.ClientSettings.TagEncruptionKey,
                    timeInterValForRecivingData_inMS: (int)SignalRClient.Enums.SignalRDataReciveTimeInterval.Milliseconds200
                    ).Trim();

            _ = HubTimers.SendProcessedMessgesToSignalRClientsWithRetry(dataToSend, expectedTagToBroadCastDataAsModal, tagOnWhichSendAllSpecificReciveTypeModal)

6) 6) to write singnalr logging in the project set following lines in program.cs (folder name and file names can be set accordingly) 

 SignalRClient.ClientSettings.ClientSettings.IsSignalRLoggingOn = true;               
 SignalRClient.ClientSettings.ClientSettings.AppContentRootPath = app.Environment.ContentRootPath;
 SignalRClient.ClientSettings.ClientSettings.AppWebRootPath = app.Environment.WebRootPath;
 SignalRClient.ClientSettings.ClientSettings.LogDirectoryPath = Path.Combine(app.Environment.ContentRootPath, @"Logs");
 SignalRClient.ClientSettings.ClientSettings.SignalRLogFilePath = Path.Combine(Path.Combine(app.Environment.ContentRootPath, @"Logs"), $"SignalRLogs-{DateTime.UtcNow:dd-MM-yyyy HH-mm-ss}.txt");


                
    



