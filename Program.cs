using System;
using System.Collections.Generic;

namespace BlazeChameleon {
    
    class Program {
        public static Dictionary<string, string> argsDict = new Dictionary<string, string>();

        static int Main(string[] args) {
            if (args.Length == 0) {
                Log.Error("Please supply BlazeChameleon with arguments (\"blazechameleon --help\" for more info)");
                return 0;
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

                    ChameleonAPI.InitializeClient(port, secret);
                    break;

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
                        "------------------------------------------\n"
                    );

                    return 0;
            }
            return 1;
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
