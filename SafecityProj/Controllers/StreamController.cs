using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using WebsocketPlaygroundChat;
using WebsocketPlaygroundChat.Websocket;
using log4net;


namespace MyApplication.Controllers
{

    [Route("api/[controller]")]
    public class StreamController : Controller
    {
        private IMediator Mediator { get; }
        public IWebsocketHandler WebsocketHandler { get; }

        public StreamController(IWebsocketHandler websocketHandler, IMediator mediator)
        {
            this.Mediator = mediator;
            WebsocketHandler = websocketHandler;
        }

        [HttpGet]
        public async Task Get()
        {
            var context = ControllerContext.HttpContext;
            var isSocketRequest = context.WebSockets.IsWebSocketRequest;

            if (isSocketRequest)
            {
                WebSocket websocket = await context.WebSockets.AcceptWebSocketAsync();

                await WebsocketHandler.Handle(Guid.NewGuid(), websocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }


        [HttpGet]
        [Route("GetDeviceList")]
        public BaseResponse GetDeviceList()
        {
            BaseResponse resp = new BaseResponse();

            WebsocketHandler.GetConnectedDevices(resp);

            return resp;
        }

        [HttpGet]
        [Route("OnOffDevice")]
        public BaseResponse OnOffDevice(string format)
        {
            BaseResponse resp = new BaseResponse();

            logFile.LogRequestResponse("Server Recieved Packet...:" + format);
            //WebsocketHandler.SendDeviceMessage(format);

            var lul = this.Mediator.Exec2(format) ?? "";

            return resp;
        }


    }
}


public class BaseResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}


public interface IMediator
{
    event ExecHandler ExecHandler;
    event ExecHandler1 ExecHandler1;
    string Exec1(string status);
    string Exec2(string status);
    void Echo(string message);
    // ...
}
public delegate string ExecHandler(string status);
public delegate void ExecHandler1(string status);
public class Mediator : IMediator
{

    public IWebsocketHandler WebsocketHandler { get; }
    public Mediator(IWebsocketHandler websocketHandler)
    {
        WebsocketHandler = websocketHandler;
    }
    public event ExecHandler ExecHandler;
    public event ExecHandler1 ExecHandler1;
    public string Exec1(string status)
    {
        if (this.ExecHandler == null)
            return null;
        return this.ExecHandler(status);
    }

    public string Exec2(string status)
    {
        logFile.LogRequestResponse("TCP Server Recieved Packet.........: \t" + status);
        if (this.ExecHandler1 != null)
              this.ExecHandler1(status);

        return "hello";
    }

    public void Echo(string message)
    {
        logFile.LogRequestResponse("Send Message to Socket.............: \t" + message);
        WebsocketHandler.SendMessageToSocketsBrowser(message);

    }
}

public class Netcat : BackgroundService
{
    private static readonly log4net.ILog log = LogHelper.GetLogger();

    CancellationToken stoppingToken1;
    NetworkStream stream = null;
    private IMediator Mediator;
    public Netcat(IMediator mediator)
    {
        this.Mediator = mediator;
    }

    // method that you want to be invoke from somewhere else
    public string Hello(string status)
    {
        return $"{status}:returned from service";
    }

    public async void SendToClient(string status)
    {
        logFile.LogRequestResponse("TCP Server Sent Packet To Hardware.: \t" + status);
        if (stream != null && stoppingToken1 != null && status.Length > 0)
        {
            byte[] array = Encoding.UTF8.GetBytes(status);
            await stream.WriteAsync(array, 0, array.Length, stoppingToken1);
            stream.Flush();
        }
    }


    // method required by `BackgroundService`
    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    TcpListener listener = new TcpListener(IPAddress.Any, 8899);
    //    listener.Start();
    //    while (!stoppingToken.IsCancellationRequested)
    //    {
    //        // ...
    //    }
    //}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
         
        logFile.LogRequestResponse("Waiting Server Connection..........: \t");
        //log.Info($"Waiting Server Connection.....");
        //IPAddress add = new IPAddress(new byte[]
        //                    { 172, 16, 1, 130 });
        //TcpListener listener = new TcpListener(add, 2570);

        TcpListener listener = new TcpListener(IPAddress.Any, 9009);

        listener.Start();
        logFile.LogRequestResponse("TCP Server Connected...............: \t");
        //log.Info($"Server Connected.....");
        stoppingToken1 = stoppingToken;
        while (!stoppingToken.IsCancellationRequested)
        {
            logFile.LogRequestResponse("Waiting a client...................: \t");
            //log.Info($"Waiting a client.....");
            TcpClient client = await listener.AcceptTcpClientAsync();
            logFile.LogRequestResponse("Hardware Client Connected..........: \t");
            //log.Info($"Client Connected.....");

            stream = client.GetStream();
            logFile.LogRequestResponse("Get Stream and enterning while loop: \t");


            while (!stoppingToken.IsCancellationRequested)
            {
                logFile.LogRequestResponse("While Loop Start...................: ");

                byte[] data = new byte[1024];
                int read = await stream.ReadAsync(data, 0, 1024, stoppingToken);
                var cmd = Encoding.UTF8.GetString(data, 0, read);
                logFile.LogRequestResponse("Packet Recieved....................: \t" + cmd);
                //log.Info($"Packet Recieved..: " + cmd);

                if (read > 0)
                {
                    //this.Mediator.ExecHandler -= this.Hello;
                    //this.Mediator.ExecHandler += this.Hello;

                    this.Mediator.ExecHandler1 -= this.SendToClient;
                    this.Mediator.ExecHandler1 += this.SendToClient;

                    this.Mediator.Echo(cmd);
                    //var sWriter = new StreamWriter(stream);
                }
                else
                {
                    break;   //Always executed as soon as the first stream of data has been received
                }

                //if (cmd == "attach")
                //{
                //    //this.Mediator.ExecHandler += this.Hello;
                //    Console.WriteLine($"[-] exec : attached");
                //    continue;
                //}
                //if (cmd == "detach")
                //{
                //    Console.WriteLine($"[-] exec : detached");
                //    //this.Mediator.ExecHandler -= this.Hello;
                //    continue;
                //}

                //await stream.WriteAsync(data, 0, read, stoppingToken);
               // stream.Flush();
            }
        }
    }
}

