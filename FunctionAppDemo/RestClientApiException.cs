using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FunctionAppDemo;

[Serializable]
public class RestClientApiException : Exception
{
    public HttpStatusCode StatusCode { get; set; }

    public RestClientApiException()
    {
    }

    public RestClientApiException(string message) : this(message, null)
    {
    }

    public RestClientApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected RestClientApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        StatusCode = (HttpStatusCode)info.GetValue("StatusCode", typeof(HttpStatusCode));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException("info");
        }

        info.AddValue("StatusCode", StatusCode);

        base.GetObjectData(info, context);
    }
}