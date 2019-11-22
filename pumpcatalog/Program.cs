using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HTMLCodeReplacer;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

namespace pumpcatalog
{
    class Program
    {

        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), PLink = "https://www.pumpcatalog.com/api/search/listings/?page_products=XpgnoX&page_similar=1&page_series=1&page_manufacturers=1&count=12&f=&q=&initial=&instance=manufacturer%3AXMnoX&only_part_numbers=&include_description=&only=products&sorting=a-z";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, DSourceLink = string.Empty, CookieString = string.Empty, searchquery = string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static StringBuilder sb, breadcrum, spec;
        private static List<string> Cookie;
        private static string cookiestr = "";
        private static bool LoopProcess = true;
        private static readonly string Homeurl = "https://www.pumpcatalog.com";
        static void Main(string[] args)
        {
            for(int sl= 1; sl < 60; sl++)
            {
                try
                {
                    Console.WriteLine("Processing link " + sl + " of 60");
                    ppcount = 1;
                    TotalCount = 0;
                    sourceid = 0;
                    LoopProcess = true;
                    InsertSearchLink(sl.ToString());
                    while (LoopProcess)
                    {
                        SourceLink = string.Empty;
                        Console.WriteLine("Processing page " + ppcount);
                        SourceLink = PLink.Replace("XpgnoX", ppcount.ToString()).Replace("XMnoX", sl.ToString());
                        DownloadString();
                        ParseJson();
                        UpdateSearchLinkStatus();
                        ppcount += 1;
                    }
                    num++;
                }
                catch
                {
                    Console.WriteLine("Error on initial process...");
                }
            }
        }
        private static void ParseJson()
        {
            try
            {
                var jobj = JObject.Parse(DownloadedString);

                var linkdatas = jobj["products"]["grid"];
                TotalCount += linkdatas.Count();
                Console.WriteLine("Total Link " + TotalCount);
                foreach (var linkdata in linkdatas)
                {
                    h1 = null;
                    h1 = new HtmlDocument();
                    h1.LoadHtml(linkdata.ToString());
                    var link = Homeurl+ h1.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString();
                    InsertProductLink(link);
                }
            }
            catch { LoopProcess = false;
                Console.WriteLine("Json Parse error.");
            }
        }
        private static void InsertProductLink(string productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "insert into tbl_pumpcatalog_product (productlink,sourceid) values(@productlink,@sourceid)";
                        cmd.Parameters.AddWithValue("@productlink", productlink);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex)
            {
            }
        }
        private static void UpdateSearchLinkStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "update tbl_pumpcatalog_searchLink set TotalLink=@TotalLink, processingpage=@processingpage where searchid=@sourceid";
                        cmd.Parameters.AddWithValue("@TotalLink", TotalCount);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.Parameters.AddWithValue("@processingpage", ppcount);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch {
                Console.WriteLine("Error While Executing search link update.....");
            }
        }

        private static void InsertSearchLink(string Searchlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    sourceid = 0;
                    con.Open();
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("select top 1 searchid,processingpage,TotalLink from tbl_pumpcatalog_searchLink where SearchTitle=@searchLink", con))
                        {
                            cmd.Parameters.AddWithValue("@searchLink", Searchlink);
                            SqlDataReader sdr = cmd.ExecuteReader();
                            while (sdr.Read())
                            {
                                sourceid = Convert.ToInt32(sdr[0].ToString());
                                ppcount = Convert.ToInt32(sdr[1] == DBNull.Value ? "1" : (sdr[1].ToString()=="0"?"1": sdr[1].ToString()));
                                TotalCount= Convert.ToInt32(sdr[2] == DBNull.Value ? "0" : sdr[2].ToString());
                            }
                        }
                    }
                    catch(Exception ex) { sourceid = 0; }

                    if (sourceid == 0)
                    {
                        using (SqlCommand cmd1 = new SqlCommand("insert into tbl_pumpcatalog_searchLink (SearchTitle) OUTPUT INSERTED.searchid values(@searchLink)", con))
                        {
                            cmd1.Parameters.AddWithValue("@searchLink", Searchlink);
                            sourceid = Convert.ToInt32(cmd1.ExecuteScalar());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error while inserting product");
            }
        }
        public static void DownloadString()
        {
            try
            {
                DownloadedString = string.Empty;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink);
                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
                request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Add("Sec-Fetch-Mode", @"cors");
                //request.Referer = "https://www.pumpcatalog.com/search/manufacturer/ampco/";
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=d97158debd9fdc6ac3aacc40a7545a4371574255961; csrftoken=uDKWL2HOO2kM4acRFRUit4yLa0oQWUYO; _ga=GA1.2.832271266.1574290160; _gid=GA1.2.907450770.1574290160; calltrk_referrer=direct; calltrk_landing=https%3A//www.pumpcatalog.com/; calltrk_session_id=9f188b92-24f7-426c-9593-16eaa97c297c; _lo_uid=172743-1574256083010-eb160ffc871d49cb; __lotl=https%3A%2F%2Fwww.pumpcatalog.com%2Fall%2Fmanufacturers%2F; sio_visitor=k37vsaf3h0fqlk37vsaf4; sio_session=k37K37VSAF4; lo_session_in=1; _lo_v=2; _lorid=172743-1574257248609-7a339f7c2c470a3a; sessionid=7zbvgspqavl1d45drextrw6l9g0melxr; _gat=1");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                DownloadedString = streamReader.ReadToEnd();
            }
            catch (WebException ex)
            {

            }
        }
    }
}
