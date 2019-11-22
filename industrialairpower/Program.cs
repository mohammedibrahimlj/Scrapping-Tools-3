using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace industrialairpower
{
    class Program
    {

        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), P;
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, CookieString = string.Empty, searchquery = string.Empty, Scode = string.Empty, DownloadedString=string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static readonly string Homeurl = "https://store.industrialairpower.com/", pdflink = "https://cdn-assets.unilogcorp.com";
        static void Main(string[] args)
        {
            SearchPageExtract();
        }

        private static void SearchPageExtract()
        {
            try
            {
                Console.Title = Name;
                while (true)
                {
                    DataSet data = GetData(1);
                    if (data != null)
                    {
                        TotalCount = data.Tables[0].Rows.Count;
                        num = 1;
                        //GetCookie();
                        foreach (DataRow row in data.Tables[0].Rows)
                        {
                            try
                            {
                                CookieCount = 0;
                                ppcount = 0;
                                SourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                DownloadString();
                                GetSearchLinkInitial();
                                num++;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch { }
        }
        private static void UpdateStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_industrialairpower_SearchLink set iscompleted=@iscompleted where id=@id", con);
                    cmd.Parameters.AddWithValue("@iscompleted", completed);
                    //cmd.Parameters.AddWithValue("@processingpage", ppage);
                    //cmd.Parameters.AddWithValue("@TotalLink", TotalPage);
                    cmd.Parameters.AddWithValue("@id", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while updating status");
            }
        }
        public static void GetSearchLinkInitial()
        {
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                var divlink = h1.DocumentNode.SelectNodes("//div[@class='sub-categories']");
                foreach (var links in divlink)
                {
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(links.InnerHtml.ToString());
                    var link = h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString();
                    InsertProductLink(Homeurl+link);
                }
                UpdateStatus(1);
            }
            catch { }
        }

        private static void InsertProductLink(string Productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into tbl_industrialairpower_SearchLink (Searchurl)"
                        + "values(@Productlink)", con);
                    cmd.Parameters.AddWithValue("@Productlink", Productlink.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while inserting search");
            }
        }
        public static DataSet GetData(int option = 1)
        {
            SqlCommand sqlCommand = new SqlCommand();
            DataSet dataSet = new DataSet();
            Console.WriteLine("Fetching data from database................");
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    switch (option)
                    {
                        case 1:
                            sqlCommand = new SqlCommand("select id,Searchurl,Processing_Page from tbl_industrialairpower_SearchLink where isnull(iscompleted,'')=0 order by id", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select ProductId,ProductURL from tbl_turtle_ProductLink where ProductId  between " + start + " and " + end + " and  isnull(Category,'')=''", sqlConnection);
                            break;
                    }
                    sqlCommand.CommandTimeout = 6000;
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                    sqlDataAdapter.Fill(dataSet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error While loading search data!!! \n" + ex.ToString());
            }
            return dataSet;
        }

        public static void DownloadString()
        {
            try
            {
                DownloadedString = string.Empty;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink);
                request.KeepAlive = true;
                request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                //request.Referer = "https://store.industrialairpower.com/Air-Dryers_c_8.html";
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"ASPSESSIONIDQABBBTRQ=KJGHEAHCOJELOBMPCKFMJBGK; affiliate=; _ga=GA1.2.1926837194.1573743094; _gid=GA1.2.761922379.1573743094; __utmc=160729599; __utmz=160729599.1573743094.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); _hjid=24278d93-a23c-473d-b6ab-7762a8097ba0; catFilter=; referer=https%3A%2F%2Fstore%2Eindustrialairpower%2Ecom%2F; _hjIncludedInSample=1; 3dvisit=2; igCountry=IN; igSplash=igSplash; viewall%5F252=1; viewall%5F296=1; viewall%5F301=1; viewall%5F246=1; viewall%5F306=1; viewall%5F283=1; viewall%5F260=1; __utma=160729599.1926837194.1573743094.1573743094.1573747810.2; ASPSESSIONIDSCDADQTQ=ACAGFOHCGOFNJPMBAFDCJCCK; lastCat=240; thiscat=240; __utmb=160729599.13.10.1573747810");

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                DownloadedString = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                //GetCookie();
                CookieCount = 1;
            }
        }
    }
}
