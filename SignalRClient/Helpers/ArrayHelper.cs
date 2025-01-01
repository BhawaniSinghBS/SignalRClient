namespace SignalRClient.Helpers
{
    public class ArrayHelper
    {
        public static byte[] ReverseBytes(byte[] byteArray, int Offset, int Size)
        {
            try
            {
                byte[] array = new byte[Size];
                if (byteArray.Length - Offset >= Size)
                {
                    int num = Size - 1;
                    for (int i = Offset; i < Offset + Size; i++)
                    {
                        array[num--] = byteArray[i];
                    }
                }
                else
                {
                    return array;
                }
                return array;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

