using System;
using Steamworks;
using System.Threading.Tasks;
using Steamworks.Data;

namespace BlazeChameleon {
	public class ChameleonSteam {

        public static void InitializeSteam() {
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) {
                try {
                    SteamClient.Init(Config.APP_ID);
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed initializing steam client:\n{e.Message}");
                }
            }
		}

        public static async Task<ChameleonCall> GetUserCount() {
            return await ChameleonCall.CallAsync(SteamUserStats.PlayerCountAsync());
        }

        public static async Task<ChameleonCall> GetLeaderboard(string lbName) {
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
            public ulong SteamID { get; set; }
            public string NickName { get; set; }
            public bool IsInGame { get; set; }

            public ChameleonUser(Friend user) {
                SteamID = user.Id.Value;
                NickName = user.Name;
                IsInGame = user.IsPlayingThisGame;
            }
		}
	}

}
