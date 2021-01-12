using System;
using Steamworks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace BlazeLBLiveFetcher {

    class Program {
        static protected CallResult<LeaderboardFindResult_t> m_SteamLeaderboard;
        static protected CallResult<LeaderboardScoresDownloaded_t> m_LeaderboardScoresDownloaded;

        static void Main(string[] args) {

            var steamInitialized = SteamAPI.Init();
            if (steamInitialized) {
                Console.WriteLine("Initializing live leaderboard data gathering");

                m_SteamLeaderboard = CallResult<LeaderboardFindResult_t>.Create(GetLB);
                SteamAPICall_t lb = SteamUserStats.FindLeaderboard(new Config().lbID);
                m_SteamLeaderboard.Set(lb);

                while (m_SteamLeaderboard.IsActive() || m_LeaderboardScoresDownloaded.IsActive()) SteamAPI.RunCallbacks();
            }
        }


        static private void GetLB(LeaderboardFindResult_t pCallResult, bool bIOFailure) {
            if (bIOFailure || pCallResult.m_bLeaderboardFound != 1) Console.WriteLine("Couldn't find leaderboard...");
            else {
                Console.WriteLine("Found Leaderboard");
                m_LeaderboardScoresDownloaded = CallResult<LeaderboardScoresDownloaded_t>.Create(GetEntries);
                SteamAPICall_t handler = SteamUserStats.DownloadLeaderboardEntries(pCallResult.m_hSteamLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, Int32.MaxValue);
                m_LeaderboardScoresDownloaded.Set(handler);
            }
        }

        static private void GetEntries(LeaderboardScoresDownloaded_t res, bool bIOFailure) {
            if (bIOFailure) Console.WriteLine("Couldn't fetch entries...");
            else {
                Console.WriteLine($"Fetched {res.m_cEntryCount} entries from leaderboard {res.m_hSteamLeaderboard}...");

                LeaderboardEntry_t[] entries = new LeaderboardEntry_t[res.m_cEntryCount];

                for (int i = 0; i < res.m_cEntryCount; i++) {
                    int[] details = new int[3];
                    SteamUserStats.GetDownloadedLeaderboardEntry(res.m_hSteamLeaderboardEntries, i, out entries[i], details, 3).ToString();
                    Console.WriteLine(details[0].ToString() + " " + details[1].ToString() + " " + details[2].ToString());
                }



                Entry[] exportEntries = new Entry[res.m_cEntryCount];

                for (int i = 0; i < res.m_cEntryCount; i++) exportEntries[i] = new Entry(entries[i].m_nGlobalRank, entries[i].m_steamIDUser.ToString(), SteamFriends.GetFriendPersonaName(entries[i].m_steamIDUser), entries[i].m_nScore);

                var lb = new Leaderboard(exportEntries);

                string json = JsonSerializer.Serialize(lb);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/live.json", json);

                Console.WriteLine("Finished writing json to file!");
            }
        }

        public class Leaderboard
        {
            public Entry[] entries { get; set; }
            public Leaderboard(Entry[] _entries) {
                entries = _entries;
            }
        }

        public class Entry
        {
            public int rank { get; set; }
            public string steamid { get; set; }
            public string name { get; set; }
            public int score { get; set; }
            public Entry(int _rank, string _steamid, string _name, int _score) {
                rank = _rank;
                steamid = _steamid;
                name = _name;
                score = _score;
            }
        }
    }
}
