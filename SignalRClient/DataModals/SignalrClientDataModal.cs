namespace SignalRClient.DataModals
{
    public class SignalrClientDataModal
    {
        public int Id { get; set; } //data model object id
        public string? ModalTypeName { get; set; }= string.Empty;//data model object type
        public string? Message { get; set; }=string.Empty;//generao message
        public object DataModal { get; set; } = new object();//assign it actual data model object
    }
}
