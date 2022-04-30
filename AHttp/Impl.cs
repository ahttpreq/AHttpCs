using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AHttp;

class AHttpFlowGroup
{
    public AHttpFlowGroup? Prev;
    public IAHttpFlow? Flow;
    public IAHttpFlow[]? Flows;

    AHttpFlowGroup(AHttpFlowGroup? prev) => Prev = prev;
    public AHttpFlowGroup(AHttpFlowGroup? prev, IAHttpFlow flow) : this(prev) => Flow = flow;
    public AHttpFlowGroup(AHttpFlowGroup? prev, IAHttpFlow[] flows) : this(prev) => Flows = flows;

    public static List<IAHttpFlow> Flattern(AHttpFlowGroup? group)
    {
        var list = new List<IAHttpFlow>();
        Flattern(group);
        void Flattern(AHttpFlowGroup? group)
        {
            if (group is null) return;
            if (group.Prev is not null) Flattern(group);
            if (group.Flow is not null) list.Add(group.Flow);
            if (group.Flows is not null) list.AddRange(group.Flows);
        }
        return list;
    }
}

record struct AHttpChainImpl(Uri Uri, AHttpFlowGroup? Flows) : IAHttpChain
{
    public IAHttpChain Use(params IAHttpFlow[] flows) => new AHttpChainImpl(Uri, new(Flows, flows));
    public IAHttpChain Use(IAHttpFlow flow) => new AHttpChainImpl(Uri, new(Flows, flow));
    public IAHttpSession Get() => Send(HttpMethod.Get);
    public IAHttpSession Post() => Send(HttpMethod.Post);
    public IAHttpSession Put() => Send(HttpMethod.Put);
    public IAHttpSession Delete() => Send(HttpMethod.Delete);
    public IAHttpSession Patch() => Send(HttpMethod.Patch);
    public IAHttpSession Head() => Send(HttpMethod.Head);
    public IAHttpSession Trace() => Send(HttpMethod.Trace);
    public IAHttpSession Options() => Send(HttpMethod.Options);
    public IAHttpSession Send(string Method) => Send(new HttpMethod(Method));
    public IAHttpSession Send(HttpMethod Method) => new AHttpSessionImpl(Uri, Method, Flows);
}

record struct AHttpSessionImpl(Uri Uri, HttpMethod Method, AHttpFlowGroup? Flows) : IAHttpSession
{
    public IAHttpSession Use(params IAHttpFlow[] flows) => new AHttpSessionImpl(Uri, Method, new(Flows, flows));
    public IAHttpSession Use(IAHttpFlow flow) => new AHttpSessionImpl(Uri, Method, new(Flows, flow));
    public Task<IAHttpResponse<object>> Auto() => AHttpImpl.Create(in this, AHttpResponseType.Auto, default).TakeAuto();
    public Task<IAHttpResponse<object>> Auto(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Auto, cancellationToken).TakeAuto();
    public Task<IAHttpResponse<T>> Json<T>(JsonSerializerOptions? options = null) => AHttpImpl.Create(in this, AHttpResponseType.Json, default).TakeJson<T>(options);
    public Task<IAHttpResponse<T>> Json<T>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) => AHttpImpl.Create(in this, AHttpResponseType.Json, cancellationToken).TakeJson<T>(options);
    public Task<IAHttpResponse<T>> Object<T>() => AHttpImpl.Create(in this, AHttpResponseType.Json, default).TakeObject<T>();
    public Task<IAHttpResponse<T>> Object<T>(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Json, cancellationToken).TakeObject<T>();
    public Task<IAHttpResponse<NameValueCollection>> Query() => AHttpImpl.Create(in this, AHttpResponseType.Query, default).TakeQuery();
    public Task<IAHttpResponse<NameValueCollection>> Query(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Query, cancellationToken).TakeQuery();
    public Task<IAHttpResponse<string>> Text() => AHttpImpl.Create(in this, AHttpResponseType.Query, default).TakeText();
    public Task<IAHttpResponse<string>> Text(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Query, cancellationToken).TakeText();
    public Task<IAHttpResponse<byte[]>> Buffer() => AHttpImpl.Create(in this, AHttpResponseType.Buffer, default).TakeBuffer();
    public Task<IAHttpResponse<byte[]>> Buffer(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Buffer, cancellationToken).TakeBuffer();
    public Task<IAHttpResponse<Stream>> Stream() => AHttpImpl.Create(in this, AHttpResponseType.Query, default).TakeStream();
    public Task<IAHttpResponse<Stream>> Stream(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Query, cancellationToken).TakeStream();
    public Task<IAHttpResponse<MultipartMemoryStreamProvider>> Multipart() => AHttpImpl.Create(in this, AHttpResponseType.Query, default).TakeMultipart();
    public Task<IAHttpResponse<MultipartMemoryStreamProvider>> Multipart(CancellationToken cancellationToken) => AHttpImpl.Create(in this, AHttpResponseType.Query, cancellationToken).TakeMultipart();
}

record class AHttpRequestImpl(HttpClient Client, HttpRequestMessage Req, CancellationTokenSource CancellationTokenSource) : IAHttpRequest
{
    public Uri? BaseUri { get; set; }
    public Uri Uri
    {
        get => Req.RequestUri;
        set => Req.RequestUri = value;
    }
    public HttpMethod Method
    {
        get => Req.Method;
        set => Req.Method = value;
    }
    public HttpContent Data

    {
        get => Req.Content;
        set => Req.Content = value;
    }
    public NameValueCollection Query { get; set; } = new();

    public HttpRequestHeaders Headers => Req.Headers;
    public CancellationToken CancellationToken => CancellationTokenSource.Token;
}

record class AHttpResponseImpl<T>(AHttpResponseType Type, HttpResponseHeaders Headers, HttpContentHeaders ContentHeaders, bool Ok, HttpStatusCode Status, string ReasonPhrase, Uri Uri) : IAHttpResponse<T>
{
    public T? Data { get; set; }
    public object? Err { get; set; }
    public HttpResponseHeaders Headers { get; set; } = Headers;
    public HttpContentHeaders ContentHeaders { get; set; } = ContentHeaders;
    public bool Ok { get; set; } = Ok;
    public HttpStatusCode Status { get; set; } = Status;
    public string ReasonPhrase { get; set; } = ReasonPhrase;
    public Uri Uri { get; set; } = Uri;
}

record class AHttpImpl(AContextImpl Ctx, List<IAHttpFlow> Flows, AHttpRequestImpl AReq, AHttpResponseType ResType)
{
    public static AHttpImpl Create(in AHttpSessionImpl sess, AHttpResponseType ResType, CancellationToken UserCancellationToken)
    {
        var Flows = AHttpFlowGroup.Flattern(sess.Flows);
        var client = new HttpClient();
        var req = new HttpRequestMessage(sess.Method, sess.Uri);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(UserCancellationToken);
        var areq = new AHttpRequestImpl(client, req, cts);
        var ctx = new AContextImpl(areq, cts);
        return new AHttpImpl(ctx, Flows, areq, ResType);
    }

    public async Task<IAHttpResponse<object>> TakeAuto() => await FlowProcessor(AutoRes, static (_, _) => { });

    public async Task<IAHttpResponse<T>> TakeJson<T>(JsonSerializerOptions? options = null) => await FlowProcessor(res => JsonRes<T>(res, options), AHttpReadBodyJsonFormatException.Raise<T>);
    public async Task<IAHttpResponse<T>> TakeObject<T>() => await FlowProcessor(ObjectRes<T>, AHttpReadBodyObjectFormatException.Raise<T>);
    public async Task<IAHttpResponse<NameValueCollection>> TakeQuery() => await FlowProcessor(QueryRes, AHttpReadBodyQueryFormatException.Raise);
    public async Task<IAHttpResponse<byte[]>> TakeBuffer() => await FlowProcessor(BufferRes, AHttpReadBodyBufferFormatException.Raise);
    public async Task<IAHttpResponse<Stream>> TakeStream() => await FlowProcessor(StreamRes, AHttpReadBodyStreamFormatException.Raise);
    public async Task<IAHttpResponse<string>> TakeText() => await FlowProcessor(TextRes, AHttpReadBodyTextFormatException.Raise);
    public async Task<IAHttpResponse<MultipartMemoryStreamProvider>> TakeMultipart() => await FlowProcessor(MultipartRes, AHttpReadBodyMultipartFormatException.Raise);

    async Task<IAHttpResponse<T>> FlowProcessor<T>(Func<HttpResponseMessage, Task<T?>> TakeVal, Action<Exception, HttpResponseMessage> Err)
    {
        return await Processor(0);
        async Task<IAHttpResponse<T>> Processor(int index)
        {
            var flow = index < Flows.Count ? Flows[index] : null;
            if (flow is null)
            {
                var res = await AReq.Client.SendAsync(AReq.Req, Ctx.CancellationToken);
                return await MakeRes(res, TakeVal, Err);
            }
            return await flow.Flow(Ctx, Next);
            async ValueTask<IAHttpResponse<T>> Next()
            {
                if (Ctx.Aborted && Ctx.AbortReason is not null) throw Ctx.AbortReason;
                return await Processor(index + 1);
            }
        }
    }

    async Task<object?> AutoRes(HttpResponseMessage res) => await AutoImpl.ReadContentByContentType(res, Ctx.CancellationToken);
    async Task<T?> JsonRes<T>(HttpResponseMessage res, JsonSerializerOptions? options = null) => await res.Content.ReadFromJsonAsync<T>(options, Ctx.CancellationToken);
    async Task<T?> ObjectRes<T>(HttpResponseMessage res) => await res.Content.ReadAsAsync<T>(Ctx.CancellationToken);
    async Task<NameValueCollection?> QueryRes(HttpResponseMessage res) => await res.Content.ReadAsFormDataAsync(Ctx.CancellationToken);
    async Task<byte[]?> BufferRes(HttpResponseMessage res) => await res.Content.ReadAsByteArrayAsync();
    async Task<Stream?> StreamRes(HttpResponseMessage res) => await res.Content.ReadAsStreamAsync();
    async Task<string?> TextRes(HttpResponseMessage res) => await res.Content.ReadAsStringAsync();
    async Task<MultipartMemoryStreamProvider?> MultipartRes(HttpResponseMessage res) => await res.Content.ReadAsMultipartAsync(Ctx.CancellationToken);

    async Task<IAHttpResponse<T>> MakeRes<T>(HttpResponseMessage res, Func<HttpResponseMessage, Task<T?>> TakeVal, Action<Exception, HttpResponseMessage> Err)
    {
        var ares = new AHttpResponseImpl<T>(ResType, res.Headers, res.Content.Headers, res.IsSuccessStatusCode, res.StatusCode, res.ReasonPhrase, res.Headers.Location);
        if (ares.Ok)
        {
            try
            {
                var val = await TakeVal(res);
                ares.Data = val;
            }
            catch (AHttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Err(ex, res);
                throw new AHttpException(ex, res, "未知解析错误");
            }
        }
        else
        {
            try
            {
                var err = await AutoImpl.ReadContentByContentType(res, Ctx.CancellationToken);
                ares.Err = err;
            }
            catch (AHttpException) { }
        }
        return ares;
    }
}

record class AContextImpl(IAHttpRequest Request, CancellationTokenSource CancellationTokenSource) : IAHttpContext
{
    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    public event Action<Exception?>? OnAbort;
    readonly object locker = new();

    internal bool Aborted { get; set; }
    internal Exception? AbortReason { get; set; }

    void DoAbort(Exception? err)
    {
        lock (locker)
        {
            if (Aborted) return;
            Aborted = true;
            AbortReason = err;
            OnAbort?.Invoke(err);
        }
    }
    public async Task Abort(Exception? err)
    {
        CancellationTokenSource.Cancel();
        DoAbort(err);
        await Task.Delay(Timeout.Infinite);
    }
    public async Task Error(Exception err)
    {
        DoAbort(err);
        await Task.FromException(err);
    }
}

static class AutoImpl
{
    /// <summary>
    /// 判断是否应该用 json 读
    /// </summary>
    static readonly HashSet<string> JsonTypes = new() { "application/json", "text/json" };

    /// <summary>
    /// 判断是否应该用 text 读
    /// </summary>
    static readonly Regex TextStart = new(@"^(text)(/.+)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly HashSet<string> TextTypes = new() { "application/base64", "application/plain", "application/xml" };

    public static async ValueTask<object?> ReadContentByContentType(HttpResponseMessage res, CancellationToken cancellationToken)
    {
        if (res.Content.IsFormData())
        {
            try
            {
                return await res.Content.ReadAsFormDataAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new AHttpReadBodyTextFormatException(ex, res);
            }
        }
        if (res.Content.IsMimeMultipartContent())
        {
            try
            {
                return await res.Content.ReadAsMultipartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new AHttpReadBodyMultipartFormatException(ex, res);
            }
        }
        var contentType = res.Content.Headers.ContentType;
        var mediaType = contentType.MediaType?.ToLower();
        var contentLength = res.Content.Headers.ContentLength;
        if (contentLength is null) return null;
        if (mediaType is null) goto other;
        if (TextStart.IsMatch(mediaType) || TextTypes.Contains(mediaType))
        {
            try
            {
                return await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new AHttpReadBodyTextFormatException(ex, res);
            }
        }
        if (JsonTypes.Contains(mediaType))
        {
            try
            {
                return await res.Content.ReadFromJsonAsync<object>((JsonSerializerOptions?)null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new AHttpReadBodyTextFormatException(ex, res);
            }
        }
    other:
        try
        {
            return await res.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            throw new AHttpReadBodyStreamFormatException(ex, res);
        }
    }
}
