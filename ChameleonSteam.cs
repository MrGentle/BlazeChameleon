using System;
using System.Collections.Generic;
using System.Text;
using Steamworks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;
using Steamworks.Data;
using System.Threading;

namespace BlazeChameleon {
	public class ChameleonSteam {

        public static void InitializeSteam() {
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) {
                try {
                    SteamClient.Init(Config.APP_ID);
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed initializing client:\n{e.Message}");
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

        /* We give up on this for now
         * LLB seems to close a lobby when a game starts
        public static ChameleonCall GetLobbyCounts() {
            return new ChameleonCall(true, LobbyData.GetLobbies());
        }*/

        
        
        
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

    /* We give up on this for now
     * LLB seems to close a lobby when a game starts
    public static class LobbyData {
        private static List<Lobby> CachedLobbies = new List<Lobby>();
        private static Dictionary<string, int> PlayerCounts = new Dictionary<string, int>() {{"ranked", 0 }, {"quickplay", 0}};
            
        private static CancellationTokenSource CancelGather = new CancellationTokenSource();
        public static bool IsGathering = false;

        public static bool StartGather(TimeSpan interval) {
            ChameleonSteam.InitializeSteam();
            Action<Lobby> checkLobbyExists = (Lobby q) => {
                Debug.Log(q.MemberCount);
                var data = q.Data;
                foreach (var dataw in q.Data) {
                    Debug.Log(dataw);
				}
                try {
                    if (data == null) {
                        var existingLobby = CachedLobbies.FindIndex(lobby => lobby.Id.Value == q.Id.Value);
                        if (existingLobby != -1) { 
                            CachedLobbies.RemoveAt(existingLobby);
                            Debug.Log("Removed Lobby");
                        }
                    }
                } catch (Exception ex) {
                    Debug.Log(ex.Message);
				}
		    };

            Action<Lobby, Friend, string> ost = (Lobby q, Friend f, string msg) => {
                Debug.Log("MSG RECEIVED");
                Debug.Log(q);
                Debug.Log(f);
                Debug.Log(msg);
		    };

            SteamMatchmaking.OnLobbyDataChanged += checkLobbyExists;
            SteamMatchmaking.OnChatMessage += ost;
            try {
                if (!IsGathering) GatherLobbyData(interval, CancelGather.Token);
                IsGathering = true;
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return false;
			}
		}


        public static void StopGather() {
            if (IsGathering) CancelGather.Cancel();
		}

        public static Dictionary<string, int> GetCounts() {
            return PlayerCounts;
		}

        public static List<Lobby> GetLobbies() {
            return CachedLobbies;
		}

        private static async Task GetPlayerCounts() {
            PlayerCounts["ranked"] = 0;
            PlayerCounts["quickplay"] = 0;

            foreach(Lobby lobby in CachedLobbies.ToArray()) {
                var success = lobby.Refresh();
                Debug.Log("Refresh success " + success.ToString());
                Debug.Log(lobby.GetData("hs"));

                if (lobby.GetData("st") == "4") PlayerCounts["ranked"] += lobby.MemberCount;
                else if (lobby.MaxMembers == 4) PlayerCounts["quickplay"] += lobby.MemberCount;
			}

			LobbyQuery query = SteamMatchmaking.LobbyList;
            Lobby[] lobbies = await query.RequestAsync();

            if (lobbies != null) {
                foreach(Lobby lobby in lobbies) {
                    var cachedLobby = CachedLobbies.Exists(cached => lobby.Id.Value == cached.Id.Value);
                    if (!cachedLobby) CachedLobbies.Add(lobby);
                }
            }; 
		}

        private static async Task GatherLobbyData(TimeSpan interval, CancellationToken cancellationToken) {
            while (true) {
                await GetPlayerCounts();
                await Task.Delay(interval);
                Debug.Log("Gathering lobby data");
                if (cancellationToken.IsCancellationRequested) { 
                    IsGathering = false;
                    return;
                }
			}
		}
    }*/
}
