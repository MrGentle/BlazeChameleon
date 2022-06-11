using System;
using Steamworks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Grapevine;

namespace BlazeChameleon {

    class Program {
        static int Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("Please supply BlazeChameleon with arguments (\"blazechameleon --help\" for more info)");
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
                                if (!int.TryParse(arg.Replace("--port=", ""), out port)) Console.WriteLine("Could not assign port, using 23451");
                            }

                            if (arg.StartsWith("--secret")) secret = arg.Replace("--secret=", "").Replace("\"", "");

                            if (arg.StartsWith("--debug")) ChameleonAPI.debug = true;
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
                        "   Optional params: --port --secret --debug\n" +
                        "   Usage: -l --port=8080 --secret=\"yoursecret\" --debug\n" +
                        "\n" +
                        "------------------------------------------\n"
                    );

                    return 0;
			}


            return 1;
        }


        
    }
}
