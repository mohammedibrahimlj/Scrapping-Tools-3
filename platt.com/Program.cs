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

namespace platt.com
{
    class Program
    {
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), navigate= "&navPage=xpgnox_1000_0";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, CookieString = string.Empty, searchquery = string.Empty, Scode = string.Empty, DownloadedString = string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static bool initial = false;
        private static readonly string Homeurl = "https://platt.com";
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
                    //initial = true;
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
                                TotalPage = 0;
                                initial = true;
                                ppage = 1;
                                CookieCount = 0;
                                ppcount = 0;
                                SourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                modifiedlink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                ppage = int.Parse(row.ItemArray[2].ToString()) != 0 ? int.Parse(row.ItemArray[2].ToString()) : 1;
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim()+ navigate.Replace("xpgnox", ppage.ToString());

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
        public static void GetSearchLinkInitial()
        {
            try
            {
                while (true)
                {
                    h1 = null;
                    h1 = new HtmlDocument();
                    h1.LoadHtml(DownloadedString);
                    try
                    {
                        if (initial)
                        {
                            TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//div[@class='searchTopBarProductNumber']").InnerText.Replace("Products", "").Replace("\t","").Replace("\r","").Replace("\n","").Replace(",","").Trim().ToString());
                            initial = false;
                        }
                    }
                    catch { }
                    try
                    {
                        var links = h1.DocumentNode.SelectNodes("//a[@class='productListCatNumLink']").Select(s => s.Attributes["href"].Value.ToString());
                        Console.WriteLine("Total Link found " + links.Count());
                        foreach (var link in links)
                        {
                            InsertProductLink(Homeurl + link);
                        }
                        UpdatePageStatus(ppage, TotalPage);
                        ppage = ppage + 1;
                        Console.WriteLine("Processing Page " + ppage);
                        SourceLink = modifiedlink + navigate.Replace("xpgnox", ppage.ToString());
                        DownloadString();
                    }
                    catch { break; }
                }
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
                    SqlCommand cmd = new SqlCommand("insert into tbl_platt_product (productlink)"
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
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                request.Headers.Add("Sec-Fetch-Site", @"cross-site");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"did=56610d8c-1cb4-414f-bae8-bac53ab9b6bf; _ga=GA1.2.831184833.1576526096; ASP.NET_SessionId=b2xj5zqjogs3hqaluxsktlai; __AntiXsrfToken=c414ebddb943411c833e29cbc4bffaca; _gid=GA1.2.1582987475.1576782397");

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
        private static void UpdateStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_platt_search set iscompleted=@iscompleted where searchid=@id", con);
                    cmd.Parameters.AddWithValue("@iscompleted", completed);
                    cmd.Parameters.AddWithValue("@id", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while updating status");
            }
        }
        private static void UpdatePageStatus(int ppage,int totallink)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_platt_search set Processing_Page=@ppage, TotalLink=@TotalLink where searchid=@id", con);
                    cmd.Parameters.AddWithValue("@ppage", ppage);
                    cmd.Parameters.AddWithValue("@TotalLink", totallink);
                    cmd.Parameters.AddWithValue("@id", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while updating status");
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
                            sqlCommand = new SqlCommand("select searchid,Searchurl,Processing_Page from tbl_platt_search where isnull(iscompleted,'')=0 order by searchid", sqlConnection);
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
    }
}
