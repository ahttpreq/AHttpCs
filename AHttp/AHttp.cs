using System;
using System.Collections.Generic;
using System.Text;

namespace AHttp;

/// <summary>
/// 异步 http 请求
/// </summary>
public static class AHttp
{
    /// <summary>
    /// 开始请求链
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static IAHttpChain Req(Uri uri) => new AHttpChainImpl(uri, null);
    /// <summary>
    /// 开始请求链
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static IAHttpChain Req(string uri) => new AHttpChainImpl(new Uri(uri), null);

    /// <summary>
    /// 默认的实例
    /// </summary>
    public static readonly IAHttp Default = new AHttpInstance(null);
}

record class AHttpInstance(AHttpFlowGroup? Flows) : IAHttp
{
    public IAHttpChain Req(Uri uri) => new AHttpChainImpl(uri, null);
    public IAHttpChain Req(string uri) => new AHttpChainImpl(new Uri(uri), null);
    public IAHttp Use(params IAHttpFlow[] flows) => new AHttpInstance(new AHttpFlowGroup(Flows, flows));
    public IAHttp Use(IAHttpFlow flow) => new AHttpInstance(new AHttpFlowGroup(Flows, flow));
}
