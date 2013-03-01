using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Xml;

namespace chanels_xml_creator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("newshows.txt")) { File.Delete("newshows.txt"); }
            string nnurl = "";
            string nnapi = "";
            string maxage = "";
            string minscore = "85";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToUpper().Contains("URL="))
                {
                    string[] temp = args[i].Split('=');
                    if (temp[1].ToUpper().Contains(@"HTTP://"))
                    {
                        nnurl = temp[1];
                        Console.WriteLine("Newznab url: " + nnurl);
                    }
                    else
                    {
                        Console.WriteLine("Newznab URL required");
                        Console.WriteLine("use switch url=http://nzbsite.com");
                    }
                }
                if (args[i].ToUpper().Contains("API="))
                {
                    string[] temp = args[i].Split('=');
                    nnapi = temp[1];
                    Console.WriteLine("Newznab API Key: " + nnapi);
                }
                if (args[i].ToUpper().Contains("MAXAGE="))
                {
                    string[] temp = args[i].Split('=');
                    maxage = temp[1];
                    Console.WriteLine("Newznab search max age: " + maxage);
                }
                if (args[i].ToUpper().Contains("MINSCORE="))
                {
                    string[] temp = args[i].Split('=');
                    minscore = temp[1];
                    Console.WriteLine("minimum matching score: " + minscore);
                }
            }
            if ((nnurl != "") && (nnapi != "") && (maxage != ""))
            {
            }
            else
            {
                Console.WriteLine("arguments missing");
                Console.WriteLine(@"useage channels.exe url=http://nzbsite.com api=mynewznabapikey maxage=SomeNumberOfDays");
                Console.WriteLine("");
                Console.WriteLine(@"optional switch minscore=somenumber  default minscore is 85");
                Console.WriteLine("minscore is a number 1-99 representing percentage required for fuzzy name matching");
                Console.WriteLine("Example..");
                Console.WriteLine(@"Channels.exe url=http://nzb.su api=biglongapikey maxage=7 minscore=92");
                Console.WriteLine("Will scan the last 7 days and will match shows between newznab and tvrage that are a 92% match or greater");
                return;
            }
            int newshowcounter = 0;
            int minimumscore = int.Parse(minscore);
            string[,] shows = new string[50000, 5];
            if (File.Exists("channels.xml"))
            {
                int counter = 0;
                XmlDocument doc = new XmlDocument();
                doc.Load("channels.xml");
                XmlNodeList nodelist = doc.SelectNodes("/tv/channel");
                if (nodelist.Count > 0)
                {
                    foreach (XmlNode node in nodelist)
                    {
                        string channel = node["name"].InnerText;
                        string showname = "";
                        string rageid = "";
                        string thumb = "";
                        string fanart = "";
                        XmlNodeList nl = node.ChildNodes;
                        foreach (XmlNode n in nl)
                        {
                            if (n.Name == "show")
                            {
                                showname = n["title"].InnerText;
                                rageid = n["rageid"].InnerText;
                                thumb = n["thumb"].InnerText;
                                fanart = n["fanart"].InnerText;
                                Console.WriteLine("---------------------------------\n\r" + channel + "\n\r" + showname + "\n\r" + rageid + "\n\r" + thumb + "\n\r" + fanart);
                                shows[counter, 0] = showname;
                                shows[counter, 1] = channel;
                                shows[counter, 2] = rageid;
                                shows[counter, 3] = thumb;
                                shows[counter, 4] = fanart;
                                counter = counter + 1;
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Channels.xml loaded and parsed");
            List<string> showlist = new List<string>();
            List<string> channellist = new List<string>();
            List<string> rageidlist = new List<string>();
            List<string> thumblist = new List<string>();
            List<string> fanartlist = new List<string>();
            for (int i = 0; i < shows.Length / 5; i++)
            {
                if (shows[i, 0] == null) { break; }
                showlist.Add(shows[i, 0]);
                channellist.Add(shows[i, 1]);
                rageidlist.Add(shows[i, 2]);
                thumblist.Add(shows[i, 3]);
                fanartlist.Add(shows[i, 4]);
            }

            bool keeprunning = true;
            int offset = 0;
            List<string> alreadytested = new List<string>();
            while (keeprunning)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Starting Newznab search");
                Console.WriteLine("Maximum age is " + maxage);
                Console.WriteLine("Offset is " + offset.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                string url = nnurl + @"/api?t=tvsearch&cat=5000&offset=" + offset.ToString() + "&maxage=" + maxage + "&apikey=" + nnapi;
                WebClient wc = new WebClient();
                string page = "";
                while (page == "")
                {
                    try
                    {
                        page = wc.DownloadString(url);
                    }
                    catch { }
                }
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(page);
                XmlNodeList nl = xdoc.SelectNodes("/rss/channel/item");
                for (int i = 0; i < nl.Count; i++)
                {
                    string rawshowname = nl[i]["title"].InnerText;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Working on " + rawshowname);
                    string cleanshowname = releaseregex(rawshowname);
                    if (cleanshowname != "")
                    {
                        if (cleanshowname.Contains(" UK")) { cleanshowname = cleanshowname.Replace(" UK", " (UK)"); }
                        if (cleanshowname.Contains(" AU")) { cleanshowname = cleanshowname.Replace(" AU", " (AU)"); }
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Showname is " + cleanshowname);
                        Boolean addit = true;
                        for (int x = 0; x < alreadytested.Count; x++)
                        {
                            if (cleanshowname.ToUpper() == alreadytested[x].ToUpper())
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(cleanshowname + " is already in our list, skipping");
                                Console.ForegroundColor = ConsoleColor.White;
                                addit = false;
                                break;
                            }
                        }
                        alreadytested.Add(cleanshowname);
                        if (addit)
                        {
                            for (int x = 0; x < showlist.Count; x++)
                            {
                                if (cleanshowname.ToUpper() == showlist[x].ToUpper())
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(cleanshowname + " is already in our list, skipping");
                                    Console.ForegroundColor = ConsoleColor.White;
                                    alreadytested.Add(cleanshowname);
                                    addit = false;
                                    break;
                                }
                            }
                        }
                        if (addit)
                        {
                            
                            string rageresults = "";
                            bool retry = true;
                            int counter = 0;
                            while (retry)
                            {
                                try
                                {
                                    wc.Encoding = System.Text.Encoding.UTF8;
                                    rageresults = wc.DownloadString("http://services.tvrage.com/feeds/full_search.php?show=" + Uri.EscapeUriString(cleanshowname));
                                    retry = false;
                                }
                                catch
                                {
                                    if (counter < 5)
                                    {
                                        counter = counter + 1;
                                    }
                                }
                            }
                            XmlDocument rage = new XmlDocument();
                            rage.LoadXml(rageresults);
                            XmlNodeList ragenl = rage.SelectNodes("/Results/show");
                            if (ragenl.Count > 0)
                            {
                                for (int r = 0; r < ragenl.Count; r++)
                                {
                                    string rageshowname = ragenl[r]["name"].InnerText.ToString();
                                    int differnece = score_differences(cleanshowname.ToUpper(), rageshowname.ToUpper());
                                    decimal score = 0;
                                    if (differnece != 0)
                                    {
                                        decimal tempd = (decimal)100 / (decimal)cleanshowname.Length;
                                        decimal tempd2 = (decimal)cleanshowname.Length * (decimal)tempd;
                                        decimal tempd3 = (decimal)differnece * (decimal)tempd;
                                        decimal tempd4 = (decimal)tempd3 / (decimal)tempd2;
                                        tempd4 = tempd4 * 100;
                                        score = (decimal)100 - tempd4;
                                        score = decimal.Round(score, 2, MidpointRounding.AwayFromZero);
                                    }
                                    else { score = 100; }
                                    if (score >= minimumscore)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Found " + score + "% match at TVRage " + rageshowname);
                                        Console.ForegroundColor = ConsoleColor.White;
                                        for (int c = 0; c < showlist.Count; c++)
                                        {
                                            if (showlist[c].ToUpper() == rageshowname.ToUpper())
                                            {
                                                addit = false;
                                                alreadytested.Add(cleanshowname);
                                                alreadytested.Add(rageshowname);
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("Found " + rageshowname + " already in our list, skipping");
                                                Console.ForegroundColor = ConsoleColor.White;
                                                break;
                                            }
                                        }
                                        if (addit)
                                        {
                                            string rageid = ragenl[r]["showid"].InnerText;
                                            String ragenetwork = "Syndicated";
                                            try
                                            {
                                                ragenetwork = ragenl[r]["network"].InnerText.ToString();
                                            }
                                            catch { }
                                            string newthumb = "";
                                            string newfanart = "";
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.WriteLine("Searching TVDB for " +  rageshowname);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            string id = "";
                                            XmlDocument xdoc2 = new XmlDocument();
                                            WebClient wc2 = new WebClient();
                                            string page2 = "";
                                            try
                                            {
                                                page2 = wc.DownloadString("http://www.thetvdb.com/api/GetSeries.php?seriesname=" + Uri.EscapeUriString(rageshowname));
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                xdoc2.LoadXml(page2);
                                                XmlNodeList nl2 = xdoc2.SelectNodes("/Data/Series");
                                                Console.WriteLine("Found " + nl2.Count.ToString() + " results at TVDB");
                                                foreach (XmlNode node in nl2)
                                                {
                                                    string tvdbshowname = node["SeriesName"].InnerText;
                                                    Console.WriteLine("Scoring " + tvdbshowname);
                                                    int score2 = score_differences(rageshowname.ToUpper(), tvdbshowname.ToUpper());
                                                    Console.WriteLine(score2.ToString() + " Differences");
                                                    if (score2 <= 1)
                                                    {
                                                        id = node["seriesid"].InnerText.ToString();
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                        Console.WriteLine("TVDB ID: " + id);
                                                        string url2 = "http://thetvdb.com/banners/posters/" + id + "-1.jpg";
                                                        string tempcrap = wc2.DownloadString(url2);
                                                        if (tempcrap.Contains("JFIF"))
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Thumbnail appears valid " + url2);
                                                            Console.ForegroundColor = ConsoleColor.White;
                                                            newthumb = url2;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("no thumbnail found");
                                                            Console.ForegroundColor = ConsoleColor.White;
                                                        }
                                                        string url3 = "http://thetvdb.com/banners/fanart/original/" + id + "-1.jpg";
                                                        tempcrap = wc2.DownloadString(url3);
                                                        if (tempcrap.Contains("JFIF"))
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Fanart appears valid " + url3);
                                                            Console.ForegroundColor = ConsoleColor.White;
                                                            newfanart = url3;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("no fanart found");
                                                            Console.ForegroundColor = ConsoleColor.White;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.DarkRed;
                                                        Console.WriteLine("Did not match TVDB");
                                                    }
                                                }
                                                showlist.Add(rageshowname);
                                                rageidlist.Add(rageid);
                                                channellist.Add(ragenetwork);
                                                thumblist.Add(newthumb);
                                                fanartlist.Add(newfanart);
                                                alreadytested.Add(rageshowname);
                                                alreadytested.Add(cleanshowname);
                                                log_new_show(rageshowname, ragenetwork);
                                                newshowcounter = newshowcounter + 1;
                                            }
                                            catch
                                            {
                                            }
                                            Console.ForegroundColor = ConsoleColor.Magenta;
                                            Console.WriteLine("Were adding " + rageshowname);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            break;
                                        }
                                    }
                                    if (addit == false)
                                    {
                                        alreadytested.Add(cleanshowname);
                                    }
                                }
                            }

                        }
                    }
                }
                if (nl.Count < 100) { keeprunning = false; }
                offset = offset + 100;
            }
            Console.WriteLine("Completed Scan");
            Console.WriteLine("Added " + newshowcounter.ToString() + " new shows");
            List<string> networks = new List<string>();
            for (int i = 0; i < showlist.Count; i++)
            {
                bool addit = true;
                for (int x = 0; x < networks.Count; x++)
                {
                    if (channellist[i].ToUpper() == networks[x].ToUpper()) { addit = false; }
                }
                if (addit)
                {
                    networks.Add(channellist[i]);
                }
            }
            networks.Sort();
            Console.WriteLine(networks.Count.ToString() + " channels");
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.NewLineOnAttributes = true;
            xmlWriterSettings.Indent = true;
            XmlWriter xml = XmlWriter.Create("channels.xml",xmlWriterSettings);
            xml.WriteStartDocument(true);
            xml.WriteStartElement("tv");
            for (int i = 0; i < networks.Count; i++)
            {
                xml.WriteStartElement("channel");
                xml.WriteStartElement("name");
                xml.WriteValue(networks[i]);
                xml.WriteEndElement();
                for (int x = 0; x < showlist.Count; x++)
                {
                    if (channellist[x].ToUpper() == networks[i].ToUpper())
                    {
                        xml.WriteStartElement("show");
                        xml.WriteStartElement("title");
                        xml.WriteValue(showlist[x]);
                        xml.WriteEndElement();
                        xml.WriteStartElement("rageid");
                        xml.WriteValue(rageidlist[x]);
                        xml.WriteEndElement();
                        xml.WriteStartElement("thumb");
                        xml.WriteValue(thumblist[x]);
                        xml.WriteEndElement();
                        xml.WriteStartElement("fanart");
                        xml.WriteValue(fanartlist[x]);
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                    }
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteEndDocument();
            xml.Close();
            string md5 = GetMD5HashFromFile("channels.xml");
            Console.WriteLine("MD5 Hash computed " + md5);
            TextWriter tw2 = new StreamWriter("channels.xml.md5");
            tw2.WriteLine(md5);
            tw2.Close();
        }

        public static string GetMD5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        static public string releaseregex(string title)
        {
            string Standard = @"^((?<series_name>.+?)[. _-]+)?s(?<season_num>\d+)[. _-]*e(?<ep_num>\d+)(([. _-]*e|-)(?<extra_ep_num>(?!(1080|720)[pi])\d+))*[. _-]*((?<extra_info>.+?)((?<![. _-])-(?<release_group>[^-]+))?)?$";
            var regexStandard = new Regex(Standard, RegexOptions.IgnoreCase);
            Match episode = regexStandard.Match(title);
            var Showname = episode.Groups["series_name"].Value;
            var Season = episode.Groups["season_num"].Value;
            var Episode = episode.Groups["ep_num"].Value;
            Showname = Showname.Replace('.', ' ');
            Showname = Showname.Trim();
            return Showname;
        }

        static private int score_differences(string source, string target)
        {
            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];
            int cost;
            if (n == 0) { return m; }
            if (m == 0) { return n; }
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    cost = (target.Substring(j - 1, 1) == source.Substring(i - 1, 1) ? 0 : 1);
                    d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                              d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
        static void log_new_show(string showname, string channel)
        {
            TextWriter tw = new StreamWriter("newshows.txt", true);
            tw.WriteLine(showname + " on channel " + channel);
            tw.Close();
        }
    }
}
