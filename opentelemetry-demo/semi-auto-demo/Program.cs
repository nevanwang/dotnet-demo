// 引入所需包
using OpenTelemetry;
using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// ================== OpenTelemetry 正确初始化 ==================
var serviceName = "test-demo";

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    // ActivitySource（可选，但建议保留）
    .AddSource(serviceName)


    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceInstanceId: Environment.MachineName
            )
            .AddAttributes(new Dictionary<string, object>
            {

                ["token"] = "",


                ["host.name"] = Environment.MachineName
            })
    )

    // 自动埋点
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()

    // 本地调试用
    .AddConsoleExporter()

    // ===== OTLP gRPC Exporter（官方推荐）=====
    .AddOtlpExporter(opt =>
    {
        // 这里如果是Jaeger并且使用grpc协议上报，endpoint需要填写成http://JaegerIP:4317
        // 这里如果是Jaeger并且使用http协议上报，endpoint需要填写成http://JaegerIP:4318/v1/traces
        // 如果是腾讯云APM,端口地址为4319和4320，使用grpc的方式上报，地址填写为http://ap-shanghai.apm.tencentcs.com:4319即可，不需要添加路径，并注意上方attributes中token的填写
        opt.Endpoint = new Uri("");

        opt.Protocol = OtlpExportProtocol.Grpc;
        // opt.Protocol = OtlpExportProtocol.HttpProtobuf;

    })

    .Build();

// 确保生命周期跟随应用
builder.Services.AddSingleton(tracerProvider);
// =============================================================


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 🔴 强制 flush（验证 exporter 是否真的存在）
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("ForceFlush OTLP exporter...");
    tracerProvider.ForceFlush();
    tracerProvider.Dispose();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// 创建DiagnosticsConfig类
public static class DiagnosticsConfig
{
    public const string ServiceName = "test-demo"; // 服务名
    public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
}



