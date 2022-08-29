using System;
using Steamworks;
using System.Threading.Tasks;
using Steamworks.Data;

namespace BlazeChameleon {
    public class ChameleonSteam {
        public static bool stopOnSteamFail = false;

        public static void InitializeSteam() {
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) {
                try {
                    SteamClient.Init(Config.APP_ID);
                }
                catch (Exception e) {
                    if (stopOnSteamFail) throw e;
                    else Log.Error($"Failed initializing steam client:\n{e.Message}");
                }
            }
        }

        public static async Task<ChameleonCall> GetUserCount() {
            InitializeSteam();
            return await ChameleonCall.CallAsync(SteamUserStats.PlayerCountAsync());
        }

        public static async Task<ChameleonCall> GetLeaderboard(string lbName) {
            InitializeSteam();
            ChameleonCall leaderboardCall = await ChameleonCall.CallAsync(SteamUserStats.FindLeaderboardAsync(lbName));
            if (leaderboardCall.CallSuccess) {
                Leaderboard leaderboard = (Leaderboard)leaderboardCall.Data;
                ChameleonCall entriesCall = await ChameleonCall.CallAsync(leaderboard.GetScoresAsync(leaderboard.EntryCount));
                if (entriesCall.CallSuccess) {
                    LeaderboardEntry[] entries = entriesCall.Data;
                    
                    ChameleonEntry[] chameleonEntries = new ChameleonEntry[leaderboard.EntryCount];
                    for (var i = 0; i < chameleonEntries.Length; i++) {
                        LeaderboardEntry entry = entries[i];
                        chameleonEntries[i] = new ChameleonEntry(new ChameleonUser(entry.User), entry.GlobalRank, entry.Score);
                    }

                    ChameleonLeaderboard finalLB = new ChameleonLeaderboard(lbName, leaderboard.EntryCount, chameleonEntries);
                    return new ChameleonCall(true, finalLB);
                } else return entriesCall;
            } else return leaderboardCall;
        }
        
        
        public struct ChameleonLeaderboard {
            public string LeaderboardName { get; set; }
            public int EntriesCount { get; set; }
            public ChameleonEntry[] Entries { get; set; }
            
            public ChameleonLeaderboard(string name, int entriesCount, ChameleonEntry[] entries) {
                LeaderboardName = name;
                EntriesCount = entriesCount;
                Entries = entries;
            }
        }

        public struct ChameleonEntry {
            public ChameleonUser User { get; set; }
            public int Rank { get; set; }
            public int Score { get; set; }

            public ChameleonEntry(ChameleonUser user, int rank, int score) {
                Rank = rank;
                Score = score;
                User = user;
            }
        }

        public struct ChameleonUser {
            public string SteamID { get; set; }
            public string Nickname { get; set; }
            public bool IsInGame { get; set; }

            public ChameleonUser(Friend user) {
                SteamID = user.Id.Value.ToString();
                Nickname = user.Name;
                IsInGame = user.IsPlayingThisGame;
            }
        }
    }
}
