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
                            if (arg.StartsWith("--port")) {
                                if (!int.TryParse(SanitizeArg(arg), out port)) Log.Warning("Could not assign port, using 23451");
                            }

                            if (arg.StartsWith("--secret")) secret = SanitizeArg(arg);

                            if (arg.StartsWith("--debug")) ChameleonAPI.debug = true;

                            if (arg.StartsWith("--stopOnSteamFail")) ChameleonSteam.stopOnSteamFail = true;
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

        private static string SanitizeArg(string arg) {
            string sArg = arg;

            if (sArg.Contains("=")) sArg = sArg.Remove(0, sArg.IndexOf("=") + 1);
            
            if (sArg.Contains("\"")) {
                if (sArg.StartsWith("\"")) sArg = sArg.Remove(0,1);
                if (sArg.EndsWith('\"')) sArg.Remove(sArg.Length-1, 1);
			}

            return sArg;
		}
    }
}
