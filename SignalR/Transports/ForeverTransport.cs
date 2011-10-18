using System;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Transports {
    public class ForeverTransport : ITransport {
        protected IJsonStringifier JsonStringifier {
            get;
            private set;
        }

        protected HttpContextBase Context {
            get;
            private set;
        }

        public ForeverTransport(HttpContextBase context, IJsonStringifier jsonStringifier) {
            Context = context;
            JsonStringifier = jsonStringifier;
        }

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public event Action<Exception> Error;

        public Func<Task> ProcessRequest(IConnection connection) {
            if (Context.Request.Path.EndsWith("/send")) {
                
                string data = Context.Request["data"];
                OnReceived(data);
            }
            else {
                if (Connected != null) {
                    Connected();
                }

                InitializeResponse(connection);
                return () => ProcessMessages(connection);
            }

            return null;
        }

        protected virtual void InitializeResponse(IConnection connection) {
            // Don't timeout and never buffer any output
            connection.ReceiveTimeout = TimeSpan.FromTicks(Int32.MaxValue - 1);
            Context.Response.BufferOutput = false;
            Context.Response.Buffer = false;
            //Context.Response.CacheControl = "no-cache";
        }

        protected void OnReceived(string data) {
            if (Received != null) {
                Received(data);
            }
        }

        private Task ProcessMessages(IConnection connection) {
            //if (Context.Response.IsClientConnected) {
                return connection.ReceiveAsync().ContinueWith(t => {
                    Send(t.Result);
                    return ProcessMessages(connection);
                }).Unwrap();
            //}

            //if (Disconnected != null) {
            //    Disconnected();
            //}

            //return TaskAsyncHelper.Empty;
        }

        public void Send(object value) {
            Context.Response.Write(JsonStringifier.Stringify(value));
        }

        protected virtual void Send(PersistentResponse response) {
            Send((object)response);
        }
    }
}