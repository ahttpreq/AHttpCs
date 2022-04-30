using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace AHttp;

[Serializable]
public class AHttpException : Exception
{
    public HttpResponseMessage Res { get; set; }
    public AHttpException(Exception inner, HttpResponseMessage res, string message) : base(message, inner) { Res = res; }
}

[Serializable]
public class AHttpReadBodyJsonFormatException : AHttpException
{
    public Type Type { get; set; }

    public AHttpReadBodyJsonFormatException(Exception inner, HttpResponseMessage res, Type type) : base(inner, res, "解析 Json 格式失败") { Type = type; }

    public static void Raise<T>(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyJsonFormatException(inner, res, typeof(T));
}

[Serializable]
public class AHttpReadBodyObjectFormatException : AHttpException
{
    public Type Type { get; set; }

    public AHttpReadBodyObjectFormatException(Exception inner, HttpResponseMessage res, Type type) : base(inner, res, "解析 Object 格式失败") { Type = type; }

    public static void Raise<T>(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyObjectFormatException(inner, res, typeof(T));
}

[Serializable]
public class AHttpReadBodyStreamFormatException : AHttpException
{
    public AHttpReadBodyStreamFormatException(Exception inner, HttpResponseMessage res) : base(inner, res, "解析 Stream 格式失败") { }

    public static void Raise(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyStreamFormatException(inner, res);
}

[Serializable]
public class AHttpReadBodyBufferFormatException : AHttpException
{
    public AHttpReadBodyBufferFormatException(Exception inner, HttpResponseMessage res) : base(inner, res, "解析 Buffer 格式失败") { }

    public static void Raise(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyBufferFormatException(inner, res);
}

[Serializable]
public class AHttpReadBodyTextFormatException : AHttpException
{
    public AHttpReadBodyTextFormatException(Exception inner, HttpResponseMessage res) : base(inner, res, "解析 Text 格式失败") { }

    public static void Raise(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyTextFormatException(inner, res);
}

[Serializable]
public class AHttpReadBodyQueryFormatException : AHttpException
{
    public AHttpReadBodyQueryFormatException(Exception inner, HttpResponseMessage res) : base(inner, res, "解析 Query 格式失败") { }

    public static void Raise(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyQueryFormatException(inner, res);
}

[Serializable]
public class AHttpReadBodyMultipartFormatException : AHttpException
{
    public AHttpReadBodyMultipartFormatException(Exception inner, HttpResponseMessage res) : base(inner, res, "解析 Multipart 格式失败") { }

    public static void Raise(Exception inner, HttpResponseMessage res) => throw new AHttpReadBodyMultipartFormatException(inner, res);
}
