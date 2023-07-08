using Grapevine;
using Pastel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlazeChameleon {
    
    class Program {
        public static Dictionary<string, string> argsDict = new Dictionary<string, string>();

        static int Main(string[] args) {
            if (args.Length == 0) {
                Log.Warning("Please supply BlazeChameleon with arguments (Run \"blazechameleon --help\" for more info)");
                args = new [] {"-l"};
            }


            switch (args[0]) {
                case "-l":
                case "--listen":
                    int port = 23451;
                    string secret = "";

                    if (args.Length > 1) {
                        foreach(string arg in args) {
                            if (arg != args[0]) {
                                Argument sanitizedArg = SanitizeArg(arg);

                                switch(sanitizedArg.argument) {
                                    case "-port":
                                    case "--port":
                                        if (!int.TryParse(sanitizedArg.value, out port)) Log.Warning("Could not assign port, using 23451");
                                        break;

                                    case "-secret":
                                    case "--secret":
                                        secret = sanitizedArg.value;
                                        break;

                                    case "-debug":
                                    case "--debug":
                                        ChameleonAPI.debug = true;
                                        break;

                                    case "-stopOnSteamFail":
                                    case "--stopOnSteamFail":
                                        ChameleonSteam.stopOnSteamFail = true;
                                        break;

                                    default:
                                        Log.Error($"Invalid argument *{sanitizedArg.argument}*");
                                        return 0;
							    }
                            }
                        }
                    }

                    try {
                        ChameleonAPI.InitializeClient(port, secret);
                    } catch(ObjectDisposedException ex) {
                        Log.Error($"Failed initializing client. Make sure you're running BlazeChameleon with proper permissions. {ex.Message}");
                        return 0;
                    } catch(Exception ex) {
                        Log.Error(ex.Message);
                        return 0;
                    }
                    break;
                case "-r":
                case "--routes":
                    PrintRoutes();
                    return 0;
                case "-h":
                case "--help":
                default:
                    Console.Write(
                        "------------------------------------------\n" +
                        " Listen for HTTP requests\n" +
                        "\n" +
                        "   -l\n" +
                        "   --listen\n" +
                        "   Optional params: --port --secret --debug --stopOnSteamFail\n" +
                        "   Usage: -l --port=8080 --secret=\"yoursecret\" --debug\n" +
                        "\n" +
                        "------------------------------------------\n" +
                        " Print available routes\n" +
                        "\n" +
                        "   -r\n" +
						"   --routes\n" +
                        "------------------------------------------\n"
                    );

                    return 0;
            }
            return 1;
        }

        private static void PrintRoutes() {
            Console.WriteLine(" Available routes:");
            MemberInfo[] members = typeof(ChameleonAPI.Routes).GetMembers();            

            foreach(MemberInfo member in members) {
                RestRouteAttribute route = (RestRouteAttribute)member.GetCustomAttribute(typeof(RestRouteAttribute));
                if (route != null) {
                    Console.WriteLine($"    {(route.Name != "" ? $"({route.Name}) " : "")}{route.HttpMethod.ToString().ToUpper()}: {route.RouteTemplate.Pastel("#AAAAAA")}");
                }
            }
        }

        private static Argument SanitizeArg(string arg) {
            string sArg = arg;
            Argument ret = new Argument();

            if (sArg.Contains("=")) { 
                ret.argument = sArg.Split('=')[0];
                sArg = sArg.Remove(0, sArg.IndexOf("=") + 1);
            } else { 
                ret.argument = sArg;
                return ret;
            }

            
            if (sArg.Contains("\"")) {
                if (sArg.StartsWith("\"")) sArg = sArg.Remove(0,1);
                if (sArg.EndsWith('\"')) sArg.Remove(sArg.Length-1, 1);
			}

            ret.value = sArg;

            return ret;
		}

        private struct Argument {
            public string argument;
            public string value;
        }
    }
}
