using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Data.SQLite;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace Test
{
    public class YoutubeMain
    {
        public string? kind { get; set; }
        public IList<YoutubeChannelItems?> items { get; set; }
        public YoutubePageInfo? pageInfo { get; set; }
    }

    public class YoutubeChannelItems
    {
        public string? kind { get; set; }
        public YoutubeItemsSnippet? snippet { get; set; }
    }

    public class YoutubeItemsSnippet
    {
        public string? publishedAt { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public int? position { get; set; }


        public YoutubeItemsSnippet(string publishedAt, string title, string description, int position)
        {
            this.publishedAt = publishedAt;
            this.title = title;
            this.description = description;
            this.position = position;
        }

    }

    public class YoutubePageInfo
    {
        public int? totalResults { get; set; }
    }



    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static string url = "https://youtube.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults=25&playlistId=PLSN6qXliOioz5lnckfofNcLJ3CnZJvEJO&key=AIzaSyAjUcFFlZW2NIo6vZHITLJX5hI-uRi_Vvc";
        private static string urlForSend = "https://server-1-x67l.onrender.com";


        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            GetYoutubeInfo();
            //GetFromDB();
            //GetFromDBAndWriteToTxt();
            //GetFromDBAndSend();

            Console.ReadKey();
        }

        public static void SaveToDB(YoutubeMain youtubeMain)
        {
            SQLiteConnection sqlite_conn = CreateConnection();
            CreateTable(sqlite_conn);
            InsertData(sqlite_conn, youtubeMain);
        }


        public static void GetFromDB()
        {
            SQLiteConnection sqlite_conn = CreateConnection();
            ReadData(sqlite_conn);
        }

        public static async void GetFromDBAndWriteToTxt()
        {
            SQLiteConnection sqlite_conn = CreateConnection();
            List<YoutubeItemsSnippet> youtubeList = ReadData(sqlite_conn);
            string path = "note1.txt";
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                foreach (var youtubeItem1 in youtubeList)
                {
                    await writer.WriteLineAsync(youtubeItem1.title);
                    await writer.WriteLineAsync(youtubeItem1.publishedAt);
                    await writer.WriteLineAsync(youtubeItem1.description);
                    await writer.WriteLineAsync(youtubeItem1.position.ToString());
                }
            }
        }

        public static void GetFromDBAndSend()
        {
            SQLiteConnection sqlite_conn = CreateConnection();
            List<YoutubeItemsSnippet> youtubeList = ReadData(sqlite_conn);
            string json = JsonSerializer.Serialize(youtubeList);
            SendYoutubeInfo(json);
        }



        static SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source = database.db; Version = 3; New = True; Compress = True;");
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

                ex.ToString();
            }
            return sqlite_conn;
        }

        static void CreateTable(SQLiteConnection conn)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS YOUTUBEINFO (title VARCHAR(20), publishedAt VARCHAR(20), description VARCHAR(20), position INT)";
            sqlite_cmd.ExecuteNonQuery();
        }

        static void InsertData(SQLiteConnection conn, YoutubeMain youtubeMain)
        {
            SQLiteCommand sqlite_cmd;
            foreach (var youtubeItem in youtubeMain.items)
            {
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = $"INSERT INTO YOUTUBEINFO (title, publishedAt, description, position) VALUES ('{youtubeItem.snippet.title}', '{youtubeItem.snippet.publishedAt}', '{youtubeItem.snippet.description}', {youtubeItem.snippet.position})";
                sqlite_cmd.ExecuteNonQuery();
            }
        }


        static List<YoutubeItemsSnippet> ReadData(SQLiteConnection conn)
        {
            List<YoutubeItemsSnippet> youtubeList = new List<YoutubeItemsSnippet>();
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM YOUTUBEINFO";
            sqlite_datareader = sqlite_cmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                string title = sqlite_datareader.GetString(0);
                string publishedAt = sqlite_datareader.GetString(1);
                string description = sqlite_datareader.GetString(2);
                int position = sqlite_datareader.GetInt32(3);
                Console.WriteLine(title + " " + publishedAt + " " + description + " " + position);
                YoutubeItemsSnippet? youtubeItemsSnippet = new YoutubeItemsSnippet(title, publishedAt, description, position);
                youtubeList.Add(youtubeItemsSnippet);
            }
            conn.Close();
            return youtubeList;
        }


        public static async void GetYoutubeInfo()
        {
            string response = "";
            await Task.Run(() => { response = client.GetStringAsync(url).Result.ToString(); });
            YoutubeMain? youtubeMain = JsonSerializer.Deserialize<YoutubeMain>(response);
            foreach (var youtubeItem in youtubeMain.items)
            {
                Console.WriteLine(youtubeItem?.snippet?.title);
                Console.WriteLine(youtubeItem?.snippet?.publishedAt);
                Console.WriteLine(youtubeItem?.snippet?.description);
                Console.WriteLine(youtubeItem?.snippet?.position);
            }
            Console.WriteLine($"Total results: {youtubeMain.pageInfo.totalResults}");
            SaveToDB(youtubeMain);
        }


        public static async void SendYoutubeInfo(string json)
        {
            string response = "";
            await Task.Run(() =>
            {
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                response = client.PostAsync(urlForSend, content).Result.ToString();
                Console.WriteLine(response);
            });
        }


    }
}