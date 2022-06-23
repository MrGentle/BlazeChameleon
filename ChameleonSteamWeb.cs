using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Linq;
using Steam.Models.SteamCommunity;
using Pastel;

namespace BlazeChameleon {
    public class ChameleonSteamWeb {
        /*
         * Reset the call counter on date change
        */
        public static int callsToday = 0;
        public static int callLimit = 100_000;
        public static DateTime today = new DateTime();
        public static bool ServiceAvailable = false;

        public static void HandleDateChange() {
            if (today.Date != DateTime.Now.Date) {
                callsToday = 0;
            }
        }

        /*
         * Generate factories based on supplied steam keys array 
        */
        public static SteamWebInterfaceFactory[] SteamWebFactories = new SteamWebInterfaceFactory[Config.STEAM_WEB_API_KEYS.Length];
        public static void GenerateSteamWebFactories() {
            for (var i = 0; i < Config.STEAM_WEB_API_KEYS.Length; i++) {
                SteamWebFactories[i] = new SteamWebInterfaceFactory(Config.STEAM_WEB_API_KEYS[i]);
            }
            Log.Debug($"Generated {SteamWebFactories.Length} steam web factories");
        }

        public static bool Connect() {
            if (CheckAPIAvailable()) {
                CheckAPIKeyHealth().Wait();
            }
            return ServiceAvailable;
		}

        /*
         * Returns the proper factory to use depending on number of calls performed today 
        */
        private static SteamWebInterfaceFactory GetFactory(int callsToMake = 0) {
            int i = (int)Math.Floor((double)(callsToday/(100_000 + callsToMake)));
            return SteamWebFactories[i];
        }

        public static void CheckOverCallLimit() {
            if (callsToday >= callLimit * SteamWebFactories.Length) throw new Exception("Steam Web API call limit reached");
        }

        public static bool CheckAPIAvailable() {
            try {
                Log.Info("Checking Steam Web API status..");
                var userStats = SteamWebFactories[0].CreateSteamWebInterface<SteamUserStats>();
                var response = userStats.GetNumberOfCurrentPlayersForGameAsync(Config.APP_ID).Wait(5000);
                if (!response) throw new TimeoutException();
                ServiceAvailable = true;
                Log.Info("Steam Web API is available!");
                return true;
            } catch {
                ServiceAvailable = false;
                Log.Warning("Steam Web API is currently unavaliable");
                return false;
			}
		}

        public static async Task CheckAPIKeyHealth() {
            List<SteamWebInterfaceFactory> factoriesList = SteamWebFactories.ToList();
            Log.System($"Checking API keys");
            var i = 0;
            var dropped = 0;
            foreach (var factory in factoriesList.ToArray()) {
                i++;
                try {
                    var userStats = factory.CreateSteamWebInterface<SteamUserStats>();
                    var response = await userStats.GetSchemaForGameAsync(Config.APP_ID, "english");
                    callsToday++;
                    Log.Info($"API Key {i} OK".Pastel("#22dd22"));
                } catch {
                    factoriesList.Remove(factory);
                    dropped++;
                    Log.Warning($"API Key {i} needs renewal, dropping factory".Pastel("#dd2222"));
                }
            }

            SteamWebFactories = factoriesList.ToArray();
            Log.System($"Finalizing with {SteamWebFactories.Length} factories. Dropped {dropped}");
        }


        // API CALLS
        public static async Task<ChameleonCall> GetPlayerSummaries(ulong[] steamIDs) {
            try {
                CheckOverCallLimit();
                int batches = (int)Math.Ceiling(steamIDs.Length / 100f);
                var factory = GetFactory(batches);

                var userInterface = factory.CreateSteamWebInterface<SteamUser>();
                List<PlayerSummaryModel> summaries = new List<PlayerSummaryModel>(); 

                for (var i = 0; i < batches; i++) {
                    ulong[] batch = steamIDs.Skip(i*100).Take(100).ToArray();
                    var userSummary = await userInterface.GetPlayerSummariesAsync(batch);
                    summaries.AddRange(userSummary.Data);
                    await Task.Delay(100);
                    callsToday++;
                }
                
                return new ChameleonCall(true, summaries.ToArray());
            } catch (Exception e) {
                return new ChameleonCall(false, $"Steam Web: {e.Message}");
            }
        }

        public static async Task<ChameleonCall> GetUserStats(ulong steamid) {
            try {
                CheckOverCallLimit();
                var factory = GetFactory();

                var statsInterface = factory.CreateSteamWebInterface<SteamUserStats>();
                var userStatsResponse = await statsInterface.GetUserStatsForGameAsync(steamid, Config.APP_ID);
                callsToday++;
                return new ChameleonCall(true, userStatsResponse.Data);
            }
            catch (Exception e) {
                return new ChameleonCall(false, $"Steam Web: {e.Message}");
            }
        }
    }
}
