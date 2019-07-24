namespace ServerX.Route
{
    internal class ResponseData
    {
        public ResponseData(object info, byte statuscode, string typename)
        {
            Data = info;
            StatusCode = statuscode;
            TypeName = typename;
        }

        public byte StatusCode { get; }
        public string TypeName { get; }
        public object Data { get; }
    }
}