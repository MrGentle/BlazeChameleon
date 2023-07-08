using System;
using Grapevine;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using Pastel;
using System.Threading;

namespace BlazeChameleon {
    class ChameleonAPI {
        public static IRestServer server { get; set; }
        public static bool debug = false;

        /*
         * Builds and starts the Grapevine REST client 
        */
        public static void InitializeClient(int _port, string _secret) {
            using (server = debug ? RestServerBuilder.UseDefaults().Build() : RestServerBuilder.From<Startup>().Build()) {
                server.Prefixes.Add($"http://+:{_port}/");
                server.BeforeRoutingSubscriber();
                server.AfterRoutingSubscriber();
                server.Locals.TryAdd("secret", _secret);

                ChameleonSteam.InitializeSteam();
                ChameleonSteamWeb.GenerateSteamWebFactories();
                ChameleonSteamWeb.Connect();

                server.Start();

                Log.System($"BlazeChameleon is listening on port {_port}...".Pastel("#ffff00"));

                Console.WriteLine("Press enter to stop the server");
                Console.ReadLine();
            }
        }

        public class Startup {
            public void ConfigureServices(IServiceCollection services) {
                services.AddLogging(logging => logging.ClearProviders());
            }
        }


		[RestResource]
        public class Routes {
            /* 
             * Returns a full leaderboard with entries
             * Supply it with a leaderboard name
            */
            [RestRoute("Get", "/api/steamworks/leaderboards/{leaderboardname}")]
            public async Task GetLeaderboard(IHttpContext context) {
                
                ChameleonResult res = await ChameleonSteam.GetLeaderboard(context.Request.PathParameters["leaderboardname"], context.CancellationToken);
                context.Locals.TryAdd("data", res);
            }

            /*
             * Returns a specific users stats
             * Supply it with a steam user id
            */
            [RestRoute("Get", "/api/web/users/stats/{steamid}")]
            public async Task GetGlobalStats(IHttpContext context) {
                var parseok = ulong.TryParse(context.Request.PathParameters["steamid"], out ulong steamid);
                if (parseok) {
                    ChameleonResult res = await ChameleonSteamWeb.GetUserStats(steamid);
                    context.Locals.TryAdd("data", res);
                } else {
                    await context.Response.SendResponseAsync($"Failed converting steamid to ulong").ConfigureAwait(false);
                }
            }

            /*
             * Returns N users summaries
             * Supply it with an array of steam user ids
            */
            [RestRoute("Post", "/api/web/users/summaries")]
            public async Task GetUserStats(IHttpContext context) {
                try {
                    ulong[] ulongIDs = ((JArray)context.Locals.Get("body")).ToObject<ulong[]>();
                    ChameleonResult res = await ChameleonSteamWeb.GetPlayerSummaries(ulongIDs.ToArray());
                    context.Locals.TryAdd("data", res);
                } catch (Exception ex) {
                    await context.Response.SendResponseAsync($"Failed converting body to ulong array: {ex.Message}").ConfigureAwait(false);
                    return;
                }
            }

            /*
             * Returns number of users playing the game as reported by steam 
            */
            [RestRoute("Get", "/api/steamworks/users/count")]
            public async Task GetUserCount(IHttpContext context) {
                ChameleonResult res = await ChameleonSteam.GetUserCount();
                context.Locals.TryAdd("data", res);
            }
        }
    }

    public static class IRestServerExtensions {
        //Middleware before routing
        public static void BeforeRoutingSubscriber(this IRestServer server) {
            server.Router.BeforeRoutingAsync += MatchedRoute;
            server.Router.BeforeRoutingAsync += Authorize;
            server.Router.BeforeRoutingAsync += StartSteam;
            server.Router.BeforeRoutingAsync += ParseJsonAsync;
        }

        public static async Task StartSteam(IHttpContext context) {
            await Task.Run(() => ChameleonSteam.InitializeSteam());
        }

        public static async Task ParseJsonAsync(IHttpContext context) {
            if (context.Request.ContentType != ContentType.Json || !context.Request.HasEntityBody) return;

            StreamReader reader = new StreamReader(context.Request.InputStream);
            string json = await Task.Run(() => reader.ReadToEnd());

            var body = JsonConvert.DeserializeObject(json);
            context.Locals.TryAdd("body", body);
        }

        public static async Task MatchedRoute(IHttpContext context) {
            string endpoint = context.Request.Endpoint;
            Log.Info($"Received request for {endpoint} from {context.Request.RemoteEndPoint}");

            //TODO: grab the second part of path as a variable and switch through it
            if (endpoint.Contains("/api/web/")) {
                if (!ChameleonSteamWeb.ServiceAvailable) {
                    if (!ChameleonSteamWeb.Connect()) {
                        context.Response.StatusCode = 503;
                        await context.Response.SendResponseAsync("SteamWeb service is not available.").ConfigureAwait(false);
                        throw new TimeoutException();
                    }
				}
			} else {
                ChameleonSteam.InitializeSteam();
            }
        }

        public static async Task Authorize(IHttpContext context) {
            string localSecret = ChameleonAPI.server.Locals.GetAs<string>("secret");
            string remoteSecret = context.Request.Headers.Get("secret");
            if (localSecret != "" && remoteSecret != localSecret) { 
                context.Response.StatusCode = 401;
                await context.Response.SendResponseAsync("Not Authorized.").ConfigureAwait(false);
                throw new UnauthorizedAccessException();
            }
        }

        //Middleware after routing
        public static void AfterRoutingSubscriber(this IRestServer server) {
            server.Router.AfterRoutingAsync += SendResponse;
            server.Router.AfterRoutingAsync += HandleSteamWeb;
        }

        public static async Task SendResponse(IHttpContext context) {
            try {
                ChameleonResult data = (ChameleonResult)context.Locals.Get("data");
                string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                context.Response.StatusCode = data.StatusCode;
                await context.Response.SendResponseAsync(json).ConfigureAwait(false);
                Log.Info($"Responded to: [{context.Request.RemoteEndPoint}]s requested {context.Request.Endpoint} ({data.StatusCode})");
            } catch(Exception ex) {
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
                await context.Response.SendResponseAsync(ex.ToString()).ConfigureAwait(false);
            }
        }

        public static async Task HandleSteamWeb(IHttpContext context) {
            ChameleonSteamWeb.HandleDateChange();
        }
    }


    public class ChameleonCall {

        public static async Task<ChameleonResult> CallAsync<T>(Task<T> task, string errorMsg = "Internal server error", string nullMsg = "Result was null") {
            try { 
                object data = await task;
                return new ChameleonResult(HttpStatusCode.Ok, data == null ? nullMsg : data);
            } catch (Exception e) {
                return new ChameleonResult(HttpStatusCode.InternalServerError, $"{errorMsg}: {e.Message}");
            }
        }

        public static ChameleonResult Call(Delegate method, string errorMsg = "Internal server error", params object[] parameters) {
            try { 
                var data = method.DynamicInvoke(parameters);
                return new ChameleonResult(HttpStatusCode.Ok, data);
            } catch (Exception e) {
                return new ChameleonResult(HttpStatusCode.InternalServerError, $"{errorMsg}: {e.Message}");
            }
        }

        
    }

    public class ChameleonResult {
        public int StatusCode = HttpStatusCode.Ok;
        public dynamic Data;
        public string Error;

        public ChameleonResult(int statusCode, dynamic data) {
            StatusCode = statusCode;
            Data = data;
        }

        public ChameleonResult(int statusCode, string error) {
            StatusCode = statusCode;
            Error = error;
        }

        public dynamic GetResult() {
            if (StatusCode == 200) return Data;
            else return Error;
        }
    }

    public static class Log {
        public static void Debug(dynamic data, [CallerMemberName] string callerName = "") {
            if (ChameleonAPI.debug) Console.WriteLine(" " + "[DEBUG]".Pastel("#000000").PastelBg("#ffffff") + $" {callerName} : {data}");
        }

        public static void Info(dynamic data, [CallerMemberName] string callerName = "") {
            Console.WriteLine($" [INFO] {callerName} : {data}");
        }

        public static void System(dynamic data, [CallerMemberName] string callerName = "") {
            Console.WriteLine(" " + "[SYSTEM]".Pastel("#000000").PastelBg("#FFFFFF") + $" {callerName} : {data}");
        }

        public static void Warning(dynamic data, [CallerMemberName] string callerName = "") {
            Console.WriteLine(" " + "[WARNING]".Pastel("#FF0000").PastelBg("#FFFFFF") + $" {callerName} : {data}");
        }

        public static void Error(dynamic data, [CallerMemberName] string callerName = "") {
            Console.WriteLine(" " + "[ERROR]".Pastel("#000000").PastelBg("#FF0000") + $" {callerName} : {data}");
        }
    }
}
