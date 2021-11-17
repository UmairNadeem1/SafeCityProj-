using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace SafecityProj.Websocket
{
    public interface IWebsocketHandler
    {
        Task SendMessageToSocketsBrowser(string message);
        Task Handle(Guid id, WebSocket websocket);
        void GetConnectedDevices(BaseResponse resp);
        Task SendDeviceMessage(string Id, string message);
    }
}

