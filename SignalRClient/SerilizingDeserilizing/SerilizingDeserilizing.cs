using Newtonsoft.Json;

namespace SignalRClient.SerilizingDeserilizing
{
    public static class SerilizingDeserilizing
    {
        public static string JSONSerializeOBJ(object objToSerilize)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(objToSerilize);
                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    return string.Empty;
                }
                else
                {
                    return jsonData;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }
        public static expectedType JSONDeserializeOBJ<expectedType>(string json)
        {
            try
            {
                Type expectedTypeType = typeof(expectedType);
                if (json != null)
                {
                    // Deserialize object to JSON
                    var expectedResultobj = JsonConvert.DeserializeObject<expectedType>(json);
                    return expectedResultobj ?? (expectedType)new object();
                }
                else
                {
                    return (expectedType)new object();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static string ConvertToBase64String(byte[] byteArray)
        {
            try
            {
                string base64String = Convert.ToBase64String(byteArray);
                return base64String;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static byte[] ConvertFromBase64String(string base64DataString)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64DataString);
                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
