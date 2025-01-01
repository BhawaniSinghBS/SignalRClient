using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System.Reflection;

namespace SignalRClient
{
    public static class SignalRClient
    {
        private static HubConnection? hubConnection;
        public static string SignalRConnectionID { get; set; }
        private static List<string> _tagsSubscribed = new List<string>();//// needs to put in local storage
        private static List<string> tagsSubscribed
        {
            get => _tagsSubscribed;
            set
            {
                _tagsSubscribed = value ?? new();
                TagsSubscribed = TagsSubscribed;// invoke state has changed to notify frontend to check values in the property
            }
        }
        public static List<string> TagsSubscribed
        {
            get => tagsSubscribed;
            set {/* for updating fron end invoke chage has happen but no set because get is from private property of this*/ }
        }  // needs to put in local storage
        public static bool IsConnected => hubConnection?.State == HubConnectionState.Connected;
        public static bool IsConnecting => hubConnection?.State == HubConnectionState.Connecting;
        public static bool IsDisconnected => hubConnection?.State == HubConnectionState.Disconnected;
        public static event Action<string, string> MessageReceived;
        private static void HandleMessageReceived(string tag, string jsonData)
        {
            return;//to avoid exception
        }
        public static async Task<bool> ConnectToHub(string signalRHubURL, string appName, string jwtToken, List<string> tagsToSubsCribe = null, Action<string, string> functionToHandleSpatsAtClientApplication = null)
        {

            if (SignalRClient.IsConnected && !SignalRClient.IsConnecting)
            {
                return true;
            }
            if (SignalRClient.IsConnecting)
            {
                return false;
            }
            MessageReceived = HandleMessageReceived;
            if (functionToHandleSpatsAtClientApplication != null)
            {
                MessageReceived += functionToHandleSpatsAtClientApplication;
            }
            List<string> tagsToSubsCribeInHub = tagsToSubsCribe?.ToList() ?? new();
            if (tagsToSubsCribeInHub == null) // adding default tags to subscribe
            {
                tagsToSubsCribeInHub = new() { "defaulttag", "SendSubscribedTagsByThisConnectionToClient", GetPingTag() };
            }

            if (!tagsToSubsCribeInHub.Contains("SendSubscribedTagsByThisConnectionToClient"))// it will get the subcribed tag by my app from server in this connection
            {
                tagsToSubsCribeInHub.Add("SendSubscribedTagsByThisConnectionToClient");// add if not present
            }

            if (!tagsToSubsCribeInHub.Contains("defaulttag"))
            {
                tagsToSubsCribeInHub.Add("defaulttag");//add
            }

            if (!tagsToSubsCribeInHub.Contains(GetPingTag()))
            {
                tagsToSubsCribeInHub.Add(GetPingTag());//add
            }

            try
            {
                hubConnection = new HubConnectionBuilder()
                .WithUrl(signalRHubURL, options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {jwtToken}");
                    options.Headers.Add("AppName", $"{appName}");
                    options.CloseTimeout = TimeSpan.FromMinutes(8);
                })
                .WithAutomaticReconnect()
                .Build();
                hubConnection.ServerTimeout = TimeSpan.FromMinutes(8);
                //}

                hubConnection.Closed += async (error) =>
                {
                    List<string> linesToLog = new List<string>()
                    {
                    $"------------------ {DateTime.Now:dd-MM-yyyy HH-mm-ss} Automatic----------------",
                    $"Controller hub Connection closed.Trying to automatic reconnect connectionId =  {SignalRConnectionID}",

                    };

                    // Add a delay before attempting to reconnect (optional)
                    await Task.Delay(1000);

                    // Try to reconnect
                    await hubConnection.StartAsync();

                    if (ClientSettings.ClientSettings.IsSignalRLoggingOn)
                    {
                        lock (ClientSettings.ClientSettings.SignalRLoggingFileLockObject)
                        {
                            linesToLog.Add($"------------------ /{DateTime.Now:dd-MM-yyyy HH-mm-ss} Automatic----------------");
                            System.IO.File.WriteAllLines(ClientSettings.ClientSettings.SignalRLogFilePath, linesToLog);
                        }
                    }
                };

                hubConnection.Reconnecting += async (exception) =>
                {
                    List<string> linesToLog = new List<string>()
                    {
                        $"------------------ {DateTime.Now:dd-MM-yyyy HH-mm-ss} Automatic----------------",
                        $"Controller hub trying to automatic reconnect .",
                        $" Exception :",
                        $"",
                        $"------------------ {SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(exception)}----------------",
                        $"",
                        $"Controller hub Connection closed. Trying to reconnect connectionId = {SignalRConnectionID}"
                    };

                    if (ClientSettings.ClientSettings.IsSignalRLoggingOn)
                    {
                        lock (ClientSettings.ClientSettings.SignalRLoggingFileLockObject)
                        {
                            linesToLog.Add($"------------------ /{DateTime.Now:dd-MM-yyyy HH-mm-ss} Automatic----------------");
                            System.IO.File.WriteAllLines(ClientSettings.ClientSettings.SignalRLogFilePath, linesToLog);
                        }
                    }
                };
                hubConnection.Reconnected += async (connectionId) =>
                {
                    await SubscribeToTagsIfHubIsAlreadyConnected(tagsSubscribed);
                    List<string> linesToLog = new List<string>()
                    {
                        $"------------------ {DateTime.Now:dd-MM-yyyy HH-mm-ss}  Automatic----------------",
                        $"Controller hub when Connection closed connectionId = {SignalRConnectionID}",
                        $"Subcribed to all older tags again  .",

                    };
                    SignalRClient.SignalRConnectionID = connectionId;
                    linesToLog.Add($"Controller automatic reconnected with connection id : {connectionId}.");
                    linesToLog.Add($"");

                    if (ClientSettings.ClientSettings.IsSignalRLoggingOn)
                    {
                        lock (ClientSettings.ClientSettings.SignalRLoggingFileLockObject)
                        {
                            linesToLog.Add($"------------------ /{DateTime.Now:dd-MM-yyyy HH-mm-ss} Automatic----------------");
                            System.IO.File.WriteAllLines(ClientSettings.ClientSettings.SignalRLogFilePath, linesToLog);
                        }
                    }
                };

                if (!IsConnected && !IsConnecting && IsDisconnected)
                {
                    await hubConnection.StartAsync();
                }
            }
            catch (Exception ex)
            {
                await RetryAsync();
            }
            var isconnected = false;

            try
            {
                isconnected = IsConnected && !string.IsNullOrEmpty(hubConnection?.ConnectionId);
            }
            catch (Exception ex)
            {
                isconnected = false;
            }

            SignalRConnectionID = IsConnected && !string.IsNullOrEmpty(hubConnection?.ConnectionId) ? hubConnection.ConnectionId : "";
            await SubscribeToTagsIfHubIsAlreadyConnected(tagsToSubsCribeInHub);
            return isconnected;
        }
        public async static Task RetryAsync(int maxAttempts = 3, int delayMilliseconds = 1000)
        {
            int attempt = 0;
            while (attempt < maxAttempts)
            {
                try
                {
                    attempt++;
                    if (!IsConnected && !IsConnecting && IsDisconnected)
                    {
                        await hubConnection.StartAsync();
                    }
                }
                catch
                {
                    if (attempt >= maxAttempts) throw; // Rethrow if max attempts reached
                    await Task.Delay(delayMilliseconds); // Wait before retrying
                }
            }
        }
        public static async Task<bool> UnSubscribeSelectedTagsIfConnected(List<string> tagstoUnSubCribe)
        {
            try
            {
                if (IsConnected)
                {
                    await hubConnection?.InvokeAsync("UnSubscribeFromTags", tagstoUnSubCribe);

                    if (tagsSubscribed?.Count() > 0)
                    {
                        tagsSubscribed.RemoveAll(tag => tagstoUnSubCribe.Contains(tag));
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return false;
            }
        }
        private static async Task<bool> SubscribeToTagsInHub(List<string> tagsToSubsCribe)
        {

            try
            {
                if (IsConnected)
                {
                    await hubConnection?.InvokeAsync("SubscribeToTagsInHub", tagsToSubsCribe);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return false;
            }
        }
        public static async Task<bool> SubscribeToTagsIfHubIsAlreadyConnected(List<string> tagsToSubsCribe)
        {
            if (IsConnected && tagsToSubsCribe?.Count > 0)
            {
                try
                {

                    // register handler first then tell hub it will imediately end registered tags to subcribed tag by client
                    List<string> tagsToSubsCribeInHub = tagsToSubsCribe;
                    foreach (var tag in tagsToSubsCribe)
                    {
                        hubConnection?.On<string, string>(tag, (tag, jsonData) =>
                        {
                            #region handle SendSubscribedTagsByThisConnectionToClient if implemented in future in some other way
                            if (!string.IsNullOrEmpty(tag) && tag == "SendSubscribedTagsByThisConnectionToClient")
                            {
                                // tags subscribed by client will we received on sub and unsub if server sends,currently server SendSubscribedTagsByThisConnectionToClient function call is commented
                                List<string> sendSubscribedTagsByThisConnectionToClient = GetDataObject<List<string>>(jsonData) ?? new List<string>();
                                tagsSubscribed = new List<string>() { "SendSubscribedTagsByThisConnectionToClient" };
                                #endregion handle SendSubscribedTagsByThisConnectionToClient if implemented in future in some other way
                            }
                            else
                            {
                                SignalRClient.MessageReceived.Invoke(tag, jsonData);
                            }
                        });
                    }
                    await SubscribeToTagsInHub(tagsToSubsCribeInHub);
                    return true;
                }
                catch (Exception ex)
                {
                    string tag = SignalRClient.GetEncruptedErrorLogTag();

                    string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                    string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                    string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                    string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                    _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                                 appName: "SignalRHub",
                                                 tagsOnWhichToSend: new List<string>() { tag },
                                                 nonSerialezedDataToSend: excetpion,
                                                 jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                    return false;
                }
            }
            return IsConnected;
        }
        //  calls SignalR Hub function named as send message to send signalR message
        public async static Task<string> SendMessage(string hubCompleteurl, string appName, List<string> tagsOnWhichToSend, object nonSerialezedDataToSend, string jwtToken, List<string> specificConnectionIdsToSend = null)
        {

            try
            {
                if (nonSerialezedDataToSend != null && tagsOnWhichToSend != null && tagsOnWhichToSend.Count() > 0)
                {
                    foreach (var tagOnWhichToSend in tagsOnWhichToSend)
                    {
                        string serializedStringObjectData = Newtonsoft.Json.JsonConvert.SerializeObject(nonSerialezedDataToSend);

                        if (hubConnection != null && !string.IsNullOrEmpty(hubConnection.ConnectionId))
                        {
                            await SendMessageToGivenConnectionIdsIfSubscribedGivenTags(specificConnectionIdsToSend, tagOnWhichToSend, serializedStringObjectData);
                        }
                        else
                        {
                            if (await SignalRClient.ConnectToHub(signalRHubURL: hubCompleteurl,
                                                                 appName: appName,
                                                                 jwtToken: jwtToken,
                                                                 tagsToSubsCribe: tagsOnWhichToSend))
                            {
                                //need to remove
                                try
                                {
                                    await SendMessageToGivenConnectionIdsIfSubscribedGivenTags(specificConnectionIdsToSend, tagOnWhichToSend, serializedStringObjectData);
                                }
                                catch (Exception ex)
                                {
                                    if (await SignalRClient.ConnectToHub(signalRHubURL: hubCompleteurl,
                                                                 appName: appName,
                                                                 jwtToken: jwtToken,
                                                                 tagsToSubsCribe: tagsOnWhichToSend))
                                    {
                                        await SendMessageToGivenConnectionIdsIfSubscribedGivenTags(specificConnectionIdsToSend, tagOnWhichToSend, serializedStringObjectData);
                                    }
                                }
                            }
                        }
                    }
                    return "message send successfully.  ";
                }
                else
                {
                    return "Not connected to hub or no tag to subcribed";
                }
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");

                return ex.Message;

            }
        }

        private static async Task SendMessageToGivenConnectionIdsIfSubscribedGivenTags(List<string> specificConnectionIdsToSend, string tagOnWhichToSend, string serializedStringObjectData)
        {
            if (specificConnectionIdsToSend?.Count > 0)
            {

                await hubConnection?.SendAsync("SendMessageToGivenConnectionIdsIfSubscribedGivenTags_ViaSignalRHub", tagOnWhichToSend, serializedStringObjectData, specificConnectionIdsToSend);
            }
            else
            {
                await hubConnection?.SendAsync("SendMessageToClientsViaSignalRHub", tagOnWhichToSend, serializedStringObjectData);
            }
        }

        public async static Task<string> SendMessageToClientsViaSignalRHubToAllClients(string hubCompleteurl, string appName, List<string> tagsOnWhichToSend, object nonSerialezedDataToSend, string jwtToken)
        {

            try
            {
                if (nonSerialezedDataToSend != null && tagsOnWhichToSend != null && tagsOnWhichToSend.Count() > 0)
                {
                    foreach (var tagOnWhichToSend in tagsOnWhichToSend)
                    {
                        string serializedStringObjectData = JsonConvert.SerializeObject(nonSerialezedDataToSend);

                        if (hubConnection != null && !string.IsNullOrEmpty(hubConnection.ConnectionId))
                        {
                            await hubConnection?.SendAsync("SendMessageToClientsViaSignalRHubToAllClients", tagOnWhichToSend, serializedStringObjectData);
                        }
                        else
                        {
                            if (await SignalRClient.ConnectToHub(signalRHubURL: hubCompleteurl,
                                                                 appName: appName,
                                                                 jwtToken: jwtToken,
                                                                 tagsToSubsCribe: tagsOnWhichToSend))
                            {
                                //need to remove
                                try
                                {
                                    await hubConnection?.SendAsync("SendMessageToClientsViaSignalRHubToAllClients", tagOnWhichToSend, serializedStringObjectData);
                                }
                                catch (Exception ex)
                                {
                                    if (await SignalRClient.ConnectToHub(signalRHubURL: hubCompleteurl,
                                                                 appName: appName,
                                                                 jwtToken: jwtToken,
                                                                 tagsToSubsCribe: tagsOnWhichToSend))
                                    {
                                        await hubConnection?.SendAsync("SendMessageToClientsViaSignalRHubToAllClients", tagOnWhichToSend, serializedStringObjectData);
                                    }
                                }
                            }
                        }
                    }
                    return "message send successfully. to clients ";
                }
                else
                {
                    return "Not connected to hub or no tag to subcribed";
                }
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return ex.Message;
            }
        }
        public static async Task<bool> Disconnect()
        {
            try
            {
                SignalRConnectionID = "";
                if (hubConnection is not null)
                {
                    await hubConnection.DisposeAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");

                return false;
            }

        }
        public static expectedType GetDataObject<expectedType>(string json)
        {
            try
            {
                Type expectedTypeType = typeof(expectedType);
                if (json != null)
                {
                    // Deserialize object to JSON
                    var expectedResultobj = JsonConvert.DeserializeObject<expectedType>(json);
                    //expectedType expectedObject = (expectedType)rawObject;
                    return expectedResultobj ?? (expectedType)new object();
                }
                else
                {
                    return (expectedType)new object();
                }
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return (expectedType)new object();
            }
        }
        public static string GetEncryptedTag(Enums.SignalRReceiveType receiveType, string etityId, out string nonEncruptedTagOnlyToDebug, string encruptionKey = ClientSettings.ClientSettings.TagEncruptionKey, int timeInterValForRecivingData_inMS = (int)Enums.SignalRDataReciveTimeInterval.Milliseconds200, bool getRadisTagWithStar = false)
        {
            try
            {
                var tag = string.Empty;

                // created string tag
                tag = $"{timeInterValForRecivingData_inMS}__{receiveType}__{etityId}";
                nonEncruptedTagOnlyToDebug = tag;
                // encripty tag with sha255
                //tag = SignalRClient.EncriptionAndDecritption.EcryptionAndDcryption.EncryptString(tag, encruptionKey);// if key is used
                tag = EncriptionAndDecritption.EcryptionAndDcryption.EncryptStringWithSHA_OneSidedEncruption(tag);

                nonEncruptedTagOnlyToDebug = tag;
                return tag?.Trim() ?? "Invalid tag";
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");

                return nonEncruptedTagOnlyToDebug = ex.Message;
            }
        }
        public static string GetEncruptedErrorLogTag()
        {
            try
            {
                // created string tag
                var tag = $"{(int)Enums.SignalRDataReciveTimeInterval.Milliseconds200}__{Enums.SignalRReceiveType.ErrorLogs}__{0}";
                //tag = SignalRClient.EncriptionAndDecritption.EcryptionAndDcryption.EncryptString(tag, encruptionKey); // if key is used
                tag = EncriptionAndDecritption.EcryptionAndDcryption.EncryptStringWithSHA_OneSidedEncruption(tag);
                return tag?.Trim() ?? "Invalid tag";
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return ex.Message;
            }
        }
        public static string GetPingTag()
        {
            return "ping";
        }

        public static string GetDebugTag()
        {
            return "Debug";
        }
        private static async Task<bool> SubscribeToPingMessage(List<string> tagsToSubsCribe)
        {

            try
            {
                if (IsConnected)
                {
                    SubscribeToTagsIfHubIsAlreadyConnected(new() { "ping" });
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                string tag = SignalRClient.GetEncruptedErrorLogTag();

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";

                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                string excetpion = $"{excetpionLocation}{SerilizingDeserilizing.SerilizingDeserilizing.JSONSerializeOBJ(ex)}";

                _ = SignalRClient.SendMessage(hubCompleteurl: ClientSettings.ClientSettings.SignalRHubUrl,
                                             appName: "SignalRHub",
                                             tagsOnWhichToSend: new List<string>() { tag },
                                             nonSerialezedDataToSend: excetpion,
                                             jwtToken: "hhdfifdsfds-dsfdsfsdf-3dfdsfd-rr-dfsfsf33-dsf");
                return false;
            }
        }
        public async static ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }
    }
}