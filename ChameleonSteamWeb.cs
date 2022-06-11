using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;
using SteamWebAPI2;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Linq;
using Steam.Models.SteamCommunity;

namespace BlazeChameleon {
	public class ChameleonSteamWeb {
        public static int callsToday = 0;
        public static DateTime today = new DateTime();

        public static void HandleDateChange() {
		    if (today.Date != DateTime.Now.Date) {
                callsToday = 0;
			}
        }

        public static SteamWebInterfaceFactory SteamWeb = new SteamWebInterfaceFactory(Config.STEAM_WEB_API_KEYS[1]);

        public static async Task<ChameleonCall> GetPlayerSummaries(ulong[] steamIDs) {
            int batches = (int)Math.Ceiling(steamIDs.Length / 100f);

            try {
                var userInterface = SteamWeb.CreateSteamWebInterface<SteamUser>();
                List<PlayerSummaryModel> summaries = new List<PlayerSummaryModel>(); 
                for (var i = 0; i < batches; i++) {
                    ulong[] batch = steamIDs.Skip(i*100).Take(100).ToArray();
                    var userSummary = await userInterface.GetPlayerSummariesAsync(batch);
                    summaries.AddRange(userSummary.Data);
                    await Task.Delay(100);
				}
                
                return new ChameleonCall(true, summaries.ToArray());
            } catch (Exception e) {
                return new ChameleonCall(false, $"Steam Web: {e}");
			}
        }

        public static async Task<ChameleonCall> GetUserStats(ulong steamid) {
            try {
                var statsInterface = SteamWeb.CreateSteamWebInterface<SteamUserStats>();
                var userStatsResponse = await statsInterface.GetUserStatsForGameAsync(steamid, Config.APP_ID);
                return new ChameleonCall(true, userStatsResponse.Data);
            }
            catch (Exception e) {
                return new ChameleonCall(false, $"Steam Web: {e.Message}");
			}
		}

	}
}
