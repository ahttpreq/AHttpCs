using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AHttp;

/// <summary>
/// 异步 http 请求
/// </summary>
public interface IAHttp
{
    /// <summary>
    /// 开始请求链
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    IAHttpChain Req(Uri uri);
    /// <summary>
    /// 开始请求链
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    IAHttpChain Req(string uri);
    /// <summary>
    /// 创建分叉的请求实例
    /// </summary>
    /// <param name="flows"></param>
    /// <returns></returns>
    IAHttp Use(params IAHttpFlow[] flows);
    /// <summary>
    /// 创建分叉的请求实例
    /// </summary>
    /// <param name="flow"></param>
    /// <returns></returns>
    IAHttp Use(IAHttpFlow flow);
}

/// <summary>
/// 请求返回的数据类型
/// </summary>
public enum AHttpResponseType : byte
{
    /// <summary>
    /// 将自动尝试返回其中的某一个
    /// </summary>
    Auto,
    /// <summary>
    /// 按 JSON 格式进行解析
    /// </summary>
    Json,
    /// <summary>
    /// 二进制反序列化
    /// </summary>
    Object,
    /// <summary>
    /// 返回 NameValueCollection，以 "application/x-www-form-urlencoded" 格式解析
    /// </summary>
    Query,
    /// <summary>
    /// 返回 string
    /// </summary>
    Text,
    /// <summary>
    /// 返回 byte[]
    /// </summary>
    Buffer,
    /// <summary>
    /// 返回 Stream
    /// </summary>
    Stream,
    /// <summary>
    /// 返回 MultipartMemoryStreamProvider
    /// </summary>
    Multipart,
}

/// <summary>
/// 异步 http 的请求实例
/// </summary>
public interface IAHttpRequest
{
    /// <summary>
    /// 基础 Uri
    /// </summary>
    Uri? BaseUri { get; set; }
    /// <summary>
    /// 请求 Uri
    /// </summary>
    Uri Uri { get; set; }
    /// <summary>
    /// 请求的方法，默认 Post
    /// </summary>
    HttpMethod Method { get; set; }
    /// <summary>
    /// 存放在 body 内的数据
    /// </summary>
    HttpContent Data { get; set; }
    /// <summary>
    /// 从 url 传递的参数
    /// </summary>
    NameValueCollection Query { get; set; }
    /// <summary>
    /// Http 头
    /// </summary>
    HttpRequestHeaders Headers { get; }
    /// <summary>
    /// 取消令牌
    /// </summary>
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// 异步 http 的响应实例
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAHttpResponse<T>
{
    /// <summary>
    /// 请求返回的数据类型
    /// </summary>
    AHttpResponseType Type { get; }
    /// <summary>
    /// 响应数据
    /// </summary>
    T? Data { get; set; }
    /// <summary>
    /// 错误数据
    /// </summary>
    object? Err { get; set; }
    /// <summary>
    /// Http 头
    /// </summary>
    HttpResponseHeaders Headers { get; set; }
    /// <summary>
    /// Http 内容头
    /// </summary>
    HttpContentHeaders ContentHeaders { get; set; }
    /// <summary>
    /// 请求是否成功
    /// </summary>
    bool Ok { get; set; }
    /// <summary>
    /// 返回的 Http 状态码
    /// </summary>
    HttpStatusCode Status { get; set; }
    /// <summary>
    /// 异常原因
    /// </summary>
    string ReasonPhrase { get; set; }
    /// <summary>
    /// 响应的 URL
    /// </summary>
    Uri Uri { get; set; }
}

/// <summary>
/// AHttp 实例链
/// </summary>
public interface IAHttpChain
{
    /// <summary>
    /// 增加处理器
    /// </summary>
    /// <param name="flows"></param>
    /// <returns></returns>
    IAHttpChain Use(params IAHttpFlow[] flows);
    /// <summary>
    /// 增加处理器
    /// </summary>
    /// <param name="flow"></param>
    /// <returns></returns>
    IAHttpChain Use(IAHttpFlow flow);
    /// <summary>
    /// 标注是 get 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Get();
    /// <summary>
    /// 标注是 post 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Post();
    /// <summary>
    /// 标注是 put 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Put();
    /// <summary>
    /// 标注是 delete 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Delete();
    /// <summary>
    /// 标注是 patch 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Patch();
    /// <summary>
    /// 标注是 head 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Head();
    /// <summary>
    /// 标注是 trace 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Trace();
    /// <summary>
    /// 标注是 options 请求
    /// </summary>
    /// <returns></returns>
    IAHttpSession Options();
    /// <summary>
    /// 发送指定方法的请求
    /// </summary>
    /// <param name="Method"></param>
    /// <returns></returns>
    IAHttpSession Send(string Method);
    /// <summary>
    /// 发送指定方法的请求
    /// </summary>
    /// <param name="Method"></param>
    /// <returns></returns>
    IAHttpSession Send(HttpMethod Method);

}

/// <summary>
/// AHttp 实例会话，创建会话时尚未实际发起请求
/// </summary>
public interface IAHttpSession
{
    /// <summary>
    /// 增加处理器
    /// </summary>
    /// <param name="flows"></param>
    /// <returns></returns>
    IAHttpSession Use(params IAHttpFlow[] flows);
    /// <summary>
    /// 增加处理器
    /// </summary>
    /// <param name="flow"></param>
    /// <returns></returns>
    IAHttpSession Use(IAHttpFlow flow);
    /// <summary>
    /// 自动推断返回类型
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<object>> Auto();
    /// <summary>
    /// 自动推断返回类型
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<object>> Auto(CancellationToken cancellationToken);
    /// <summary>
    /// 按 JSON 格式解析
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IAHttpResponse<T>> Json<T>(JsonSerializerOptions? options = null);
    /// <summary>
    /// 按 JSON 格式解析
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IAHttpResponse<T>> Json<T>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// 二进制反序列化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IAHttpResponse<T>> Object<T>();
    /// <summary>
    /// 二进制反序列化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IAHttpResponse<T>> Object<T>(CancellationToken cancellationToken);
    /// <summary>
    /// 按 Query 格式解析
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<NameValueCollection>> Query();
    /// <summary>
    /// 按 Query 格式解析
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<NameValueCollection>> Query(CancellationToken cancellationToken);
    /// <summary>
    /// 解析为文本
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<string>> Text();
    /// <summary>
    /// 解析为文本
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<string>> Text(CancellationToken cancellationToken);
    /// <summary>
    /// 解析为字节数组
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<byte[]>> Buffer();
    /// <summary>
    /// 解析为字节数组
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<byte[]>> Buffer(CancellationToken cancellationToken);
    /// <summary>
    /// 解析为流
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<Stream>> Stream();
    /// <summary>
    /// 解析为流
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<Stream>> Stream(CancellationToken cancellationToken);
    /// <summary>
    /// 解析为多部分内容
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<MultipartMemoryStreamProvider>> Multipart();
    /// <summary>
    /// 解析为多部分内容
    /// </summary>
    /// <returns></returns>
    Task<IAHttpResponse<MultipartMemoryStreamProvider>> Multipart(CancellationToken cancellationToken);
}

/// <summary>
/// 异步 http 的请求上下文
/// </summary>
public interface IAHttpContext
{
    /// <summary>
    /// 请求
    /// </summary>
    IAHttpRequest Request { get; }

    /// <summary>
    /// 产生错误，中断后续处理器，将调用 abort，将抛出异常
    /// </summary>
    /// <param name="err"></param>
    /// <returns></returns>
    Task Error(Exception err);
    /// <summary>
    /// 中断后续处理器，可选提供一个错误，返回一个永远不会完成的 Task，并使 next 永远不会结束，将忽略多次调用
    /// </summary>
    /// <param name="err"></param>
    /// <returns></returns>
    Task Abort(Exception? err);
    /// <summary>
    /// 中断事件
    /// </summary>
    event Action<Exception?> OnAbort;

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; }
}

/// <summary>
/// 处理流水线
/// </summary>
public interface IAHttpFlow
{
    /// <summary>
    /// 处理流水线函数
    /// </summary>
    ValueTask<IAHttpResponse<T>> Flow<T>(IAHttpContext ctx, Func<ValueTask<IAHttpResponse<T>>> next);
}
CC