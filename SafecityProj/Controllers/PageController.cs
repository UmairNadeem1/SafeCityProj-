using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebsocketPlaygroundChat.Websocket;

namespace MyApplication.Controllers 
{
    public class PageController : Controller
    {
        private IMediator Mediator { get; }
        public IWebsocketHandler WebsocketHandler { get; }

        public PageController(IWebsocketHandler websocketHandler,IMediator mediator)
        {
            this.Mediator = mediator;
            WebsocketHandler = websocketHandler;
        }

        public IActionResult Index()
        {
            //ExecHandler exec = new ExecHandler(this.Mediator.Exec1);

            //ViewData["Message"] = this.Mediator.Exec1("hello world from controller") ?? "nothing from hosted service";
            return View();
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var services = context.RequestServices;
            var requestServices = (IWebsocketHandler)services.GetService(typeof(IWebsocketHandler));
            await requestServices.Handle(Guid.NewGuid(), webSocket);
        }
        public async Task<WebSocket> getWebSocket() {
            return await HttpContext.WebSockets.AcceptWebSocketAsync();
        }
        public IActionResult signalsControl()
        {
            var resp = new BaseResponse();
            WebsocketHandler.GetConnectedDevices(resp);

            var obj = (List<SocketConnection>)resp.Data;

            var list = new List<DevicesList>();
            foreach (var item in obj)
            {
                list.Add(new DevicesList { Id = item.Id.ToString() });
            }

            ViewBag.resp = list;
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}

public class DevicesList
{
    public string Id { get; set; }
}