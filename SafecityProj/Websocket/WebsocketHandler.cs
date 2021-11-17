using log4net;
//using Newtonsoft.Json;
using SafecityProj.Websocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace SafeCityProj.Websocket
{
    public class WebsocketHandler : IWebsocketHandler
    {

        //private static readonly ILog log = LogHelper.GetLogger();

        public List<SocketConnection> websocketConnections = new List<SocketConnection>();

        public WebsocketHandler()
        {
            SetupCleanUpTask();
        }



        public async Task Handle(Guid id, WebSocket webSocket)
        {
            lock (websocketConnections)
            {
                websocketConnections.Add(new SocketConnection
                {
                    Id = id,
                    WebSocket = webSocket
                });
            }

            logFile.LogRequestResponse("Connected WebSocket Client.........: \t" + id);
            //log.Info($"Connected WebSocket Client: " + id);
            var data = ConvertDataToFrame("$610452982526252020000@");
            await SendMessageToSockets("$610452982526252020000@");
            while (webSocket.State == WebSocketState.Open)
            {
                var message = await ReceiveMessage(id, webSocket);
                if (message != null)
                {
                    updateList(id, ConvertDataToFrame(message));
                    var item = websocketConnections.Where(x => x.Id == id).FirstOrDefault();
                   // log.Info("Frame Recieved : " + JsonConvert.SerializeObject(item.Frame));
                    await SendMessageToSockets(message);
                }
            }
        }

        public void updateList(Guid Id, Frames frame)
        {
            var item = websocketConnections.Where(x => x.Id == Id).FirstOrDefault();
            if (item != null)
            {
                var index = websocketConnections.IndexOf(item);
                websocketConnections[index].Frame = frame;
            }
        }

        private async Task<string> ReceiveMessage(Guid id, WebSocket webSocket)
        {
            try
            {
                var arraySegment = new ArraySegment<byte>(new byte[4096]);
                var receivedMessage = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);
                if (receivedMessage.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.Default.GetString(arraySegment).TrimEnd('\0');
                    if (!string.IsNullOrWhiteSpace(message))
                        return $"<b>{id}</b>: {message}";

                    // return message;
                }
                return null;
            }
            catch (Exception e)
            {

                throw e;
            }

        }




        public async Task SendMessageToSocketsBrowser(string message)
        {
            IEnumerable<SocketConnection> toSentTo;

            lock (websocketConnections)
            {
                toSentTo = websocketConnections.ToList();
            }

            logFile.LogRequestResponse("Socket Recieved....................: \t" + message);

            var frame = ConvertDataToFrame(message);

            logFile.LogRequestResponse("Socket Recieved....................: \t" + message);


            var tasks = toSentTo.Select(async websocketConnection =>
            {
                if (websocketConnection.WebSocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.Default.GetBytes(frame.BreakerStatus);
                    var arraySegment = new ArraySegment<byte>(bytes);
                    await websocketConnection.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
            await Task.WhenAll(tasks);
        }

        private async Task SendMessageToSockets(string message)
        {
            IEnumerable<SocketConnection> toSentTo;

            lock (websocketConnections)
            {
                toSentTo = websocketConnections.ToList();
            }

            var frame = ConvertDataToFrame(message);

            var tasks = toSentTo.Select(async websocketConnection =>
            {
                if (websocketConnection.WebSocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.Default.GetBytes(frame.BreakerStatus);
                    var arraySegment = new ArraySegment<byte>(bytes);
                    await websocketConnection.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
            await Task.WhenAll(tasks);
        }

        public async Task SendDeviceMessage(string Id, string message)
        {
            IEnumerable<SocketConnection> toSentTo;

            lock (websocketConnections)
            {
                toSentTo = websocketConnections.Where(x => x.Id.ToString() == Id).ToList();
            }

            var tasks = toSentTo.Select(async websocketConnection =>
            {
                if (websocketConnection.WebSocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.Default.GetBytes(message);
                    var arraySegment = new ArraySegment<byte>(bytes);
                    await websocketConnection.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
            await Task.WhenAll(tasks);
        }

        public void GetConnectedDevices(BaseResponse resp)
        {
            resp.Code = "00";
            resp.Message = "Get Devices";
            resp.Data = websocketConnections;
        }

        private Frames ConvertDataToFrame(string FrameReceived)
        {
            Frames frame = new Frames();
            frame.StartFrame = FrameReceived.Substring(0, 1);
            frame.IMEI = FrameReceived.Substring(1, 15);
            frame.DbNumber = FrameReceived.Substring(16, 2);
            frame.BreakerStatus = FrameReceived.Substring(18, 4);
            frame.EndFrame = FrameReceived.Substring(22, 1);

            return frame;
        }

        //public async task broadcasttouser(guid id, string message)
        //{
        //    await client.client(id).sendasync(message);
        //}

        private void SetupCleanUpTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    IEnumerable<SocketConnection> openSockets;
                    IEnumerable<SocketConnection> closedSockets;

                    lock (websocketConnections)
                    {
                        openSockets = websocketConnections.Where(x => x.WebSocket.State == WebSocketState.Open || x.WebSocket.State == WebSocketState.Connecting);
                        closedSockets = websocketConnections.Where(x => x.WebSocket.State != WebSocketState.Open && x.WebSocket.State != WebSocketState.Connecting);

                        websocketConnections = openSockets.ToList();
                    }

                    foreach (var closedWebsocketConnection in closedSockets)
                    {
                        await SendMessageToSockets($"<b>{closedWebsocketConnection.Id}</b> has left the chat");
                       //log.Info($"<b>{closedWebsocketConnection.Id}</b> has left the chat");
                    }

                    await Task.Delay(3000);
                }

            });
        }

    }

    public class SocketConnection
    {
        public Guid Id { get; set; }
        public WebSocket WebSocket { get; set; }
        public Frames Frame { get; set; }
    }

    public class Frames
    {
        public string StartFrame { get; set; }
        public string IMEI { get; set; }
        public string DbNumber { get; set; }
        public string BreakerStatus { get; set; }
        public string EndFrame { get; set; }
    }
}
