using System;
using System.Collections.Generic;
using System.Text;
using Grapevine;
using System.Threading;
using System.Threading.Tasks;
using BlazeChameleon;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BlazeChameleon {
	class ChameleonAPI {
        public static IRestServer server { get; set; }
        public static bool debug = false;

        public static void InitializeClient(int _port, string _secret) {
            using (server = debug ? RestServerBuilder.UseDefaults().Build() : RestServerBuilder.From<Startup>().Build()) {
                server.Prefixes.Add($"http://+:{_port}/");
                if (_secret != "") {
                    server.BeforeRoutingSubscriber();
                    server.AfterRoutingSubscriber();
                    server.Locals.TryAdd("secret", _secret);
                }

                server.Start();

                //LobbyData.StartGather(TimeSpan.FromSeconds(2));

                Console.WriteLine($"BlazeChameleon is listening on port {_port}...");
                
                while(true) {};
			}
		}

        public class Startup {
            public void ConfigureServices(IServiceCollection services) {
                if (!debug) services.AddLogging(logging => logging.ClearProviders());
            }
        }


		[RestResource]
        public class Routes {
            [RestRoute("Get", "/api/leaderboards/{leaderboardname}")] //Get full leaderboard by leaderboardname
            public async Task GetLeaderboard(IHttpContext context) {
                ChameleonCall res = await ChameleonSteam.GetLeaderboard(context.Request.PathParameters["leaderboardname"]);
                context.Locals.TryAdd("data", res);
            }

            /* We give up on this for now
             * LLB seems to close lobbies when a game starts
            [RestRoute("Get", "/api/matchmaking/lobbies")] //Get all lobbies
            public async Task GetLobbies(IHttpContext context) {
                ChameleonCall res = ChameleonSteam.GetLobbyCounts();
                context.Locals.TryAdd("data", res);
            }*/

            [RestRoute("Get", "/api/users/stats/{steamid}")] //Get user stats
            public async Task GetGlobalStats(IHttpContext context) {
                ChameleonCall res = await ChameleonSteamWeb.GetUserStats(ulong.Parse(context.Request.PathParameters["steamid"]));
                context.Locals.TryAdd("data", res);
            }

            //Users
            [RestRoute("Post", "/api/users/summaries")]
            public async Task GetUserStats(IHttpContext context) {
                try {
                    ulong[] ulongIDs = ((JArray)context.Locals.Get("body")).ToObject<ulong[]>();
                    ChameleonCall res = await ChameleonSteamWeb.GetPlayerSummaries(ulongIDs.ToArray());
                    context.Locals.TryAdd("data", res);
                } catch (Exception ex) {
                    await context.Response.SendResponseAsync($"Failed converting body to ulong array: {ex.Message}").ConfigureAwait(false);
                    return;
				}
            }

            [RestRoute("Get", "/api/users/count")]
            public async Task GetUserCount(IHttpContext context) {
                ChameleonCall res = await ChameleonSteam.GetUserCount();
                context.Locals.TryAdd("data", res);
            }
        }

        
	}

    public static class IRestServerExtensions {
        public static void BeforeRoutingSubscriber(this IRestServer server) {
            server.Router.BeforeRoutingAsync += LogMatchedRoute;
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

        public static async Task LogMatchedRoute(IHttpContext context) {
            Console.WriteLine($"Received request for {context.Request.Endpoint} from {context.Request.RemoteEndPoint}");
		}

        public static async Task Authorize(IHttpContext context) {
            string localSecret = (string)ChameleonAPI.server.Locals.Get("secret");
            string remoteSecret = context.Request.Headers.Get("secret");
            if (remoteSecret != localSecret) { 
                await context.Response.SendResponseAsync("Not Authorized.").ConfigureAwait(false);
                throw new UnauthorizedAccessException();
            }
        }

        public static void AfterRoutingSubscriber(this IRestServer server) {
            server.Router.AfterRoutingAsync += SendResponse;
            server.Router.AfterRoutingAsync += HandleSteamWeb;
		}

        public static async Task SendResponse(IHttpContext context) {
            try {
                ChameleonCall data = (ChameleonCall)context.Locals.Get("data");
                string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                await context.Response.SendResponseAsync(json).ConfigureAwait(false);
			} catch(Exception ex) {
                await context.Response.SendResponseAsync(ex.ToString()).ConfigureAwait(false);
			}
		}

        public static async Task HandleSteamWeb(IHttpContext context) {
            ChameleonSteamWeb.HandleDateChange();
		}
    }


    public class ChameleonCall {
        public bool CallSuccess;
        public dynamic Data;
        public string Error;

        public ChameleonCall(bool success, dynamic data) {
            CallSuccess = success;
            Data = data;
		}

        public ChameleonCall(bool _success, string _err) {
            CallSuccess = _success;
            Error = _err;
		}

        public static async Task<ChameleonCall> CallAsync<T>(Task<T> task, string errorMsg = "Internal server error", string nullMsg = "Result was null") {
            try { 
                object data = await task;
                return new ChameleonCall(true, data == null ? nullMsg : data);
            } catch (Exception e) {
                return new ChameleonCall(false, $"{errorMsg}: {e.Message}");
			}
		}

        public static ChameleonCall Call(Delegate method, string errorMsg = "Internal server error", params object[] parameters) {
            try { 
                var data = method.DynamicInvoke(parameters);
                return new ChameleonCall(true, data);
            } catch (Exception e) {
                return new ChameleonCall(false, $"{errorMsg}: {e.Message}");
			}
		}

        public dynamic GetResult() {
            if (CallSuccess) return Data;
            else return Error;
		}
	}

    public static class Debug {
        public static void Log(dynamic data) {
            if (ChameleonAPI.debug) Console.WriteLine($" [DEBUG] : {data}");
		}
    }
    
}
