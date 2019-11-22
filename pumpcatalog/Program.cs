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
using HTMLCodeReplacer;
using System.Data;

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
        private static readonly string Homeurl = "https://www.pumpcatalog.com",specat= "pro_tab_content table1 dib clearfix | pro_tab_content table2 clearfix";
        private static PumpCatelog DPumpCatelog;
        static void Main(string[] args)
        {
            Console.Title = Name;
            while (true)
            {
                DataSet data = GetData(1);
                if (data != null)
                {
                    TotalCount = data.Tables[0].Rows.Count;
                    if (TotalCount == 0)
                        break;

                    num = 1;
                    foreach (DataRow row in data.Tables[0].Rows)
                    {
                        try
                        {
                            SourceLink = string.Empty;
                            sourceid = 0;
                            Console.WriteLine("Processing link " + num + " of " + TotalCount);
                            SourceLink = "https://www.pumpcatalog.com/berkeley/water-system-parts/001355/";
                            SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            DownloadHTMLString();
                            ProcessProductHTML();
                            num++;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            Console.WriteLine("Completed....");
            Console.ReadKey();
        }
        private static void InitialProcess()
        {
            for (int sl = 1; sl < 60; sl++)
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
                        DownloadHTMLString();
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
        private static void ProcessProductHTML()
        {
            try
            {
                DPumpCatelog = new PumpCatelog();
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var breadcrum = h1.DocumentNode.SelectSingleNode("//div[@class='breadcrumbs clearfix']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(breadcrum);
                    DPumpCatelog.Category = h2.DocumentNode.SelectNodes("//li").Select(s => s.InnerText.ToString()).Aggregate((a, b) => a + " | " + b).ToString();
                }
                catch { DPumpCatelog.Category = ""; }
                try
                {
                    var Ctitle = h1.DocumentNode.SelectSingleNode("//div[@class='product_features p60 fright clearfix']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Ctitle);
                    DPumpCatelog.Title = h2.DocumentNode.SelectSingleNode("//p").InnerText.ToString();
                }
                catch { DPumpCatelog.Title = ""; }

                try
                {
                    var Cprice = h1.DocumentNode.SelectSingleNode("//div[@class='price_tag product-info clearfix']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Cprice);
                    DPumpCatelog.Price = h2.DocumentNode.SelectSingleNode("//h3").InnerText.ToString().Trim();
                }
                catch { DPumpCatelog.Price = ""; }
                try
                {
                    var Cprice = h1.DocumentNode.SelectSingleNode("//div[@class='product_code clearfix']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Cprice);
                    var imodeltext = h2.DocumentNode.SelectSingleNode("//span").InnerText.ToString();
                    var spmodeltest = imodeltext.Split('|');
                    foreach (var spdata in spmodeltest)
                    {
                        if (spdata.Contains("Model"))
                        {
                            DPumpCatelog.PartNo = "#" + spdata.Replace("Model #:", "").Trim();
                        }
                        else
                        {
                            DPumpCatelog.MPN = "#" + spdata.Replace("Item:", "").Trim();
                        }
                    }

                }
                catch { }
                try
                {
                    foreach (var tclass in specat.Split('|'))
                    {

                        var Cspec = h1.DocumentNode.SelectSingleNode("//table[@class='"+ tclass.ToString().Trim() + "']").InnerHtml.ToString();
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(Cspec);
                        var spectr = h2.DocumentNode.SelectNodes("//tr");
                        foreach (var tr in spectr)
                        {
                            h3 = null;
                            h3 = new HtmlDocument();
                            h3.LoadHtml(tr.InnerHtml.ToString());
                            var tdlist = h3.DocumentNode.SelectNodes("//td");
                            if (tdlist[0].InnerText.ToString() != "UPC")
                            {
                                DPumpCatelog.Specification += tdlist[0].InnerText.ToString().Trim() + " : " + tdlist[1].InnerText.ToString().Trim() + " | ";
                            }
                            else
                            {
                                DPumpCatelog.UPC = "#" + tdlist[1].InnerText.ToString().Trim().ToString();
                            }

                        }
                    }
                }
                catch
                {
                }
                try
                {
                    var Cspec = h1.DocumentNode.SelectSingleNode("//div[@class='product_features p60 fright clearfix']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Cspec);
                    DPumpCatelog.Manufacture = h2.DocumentNode.SelectSingleNode("//img").Attributes["alt"].Value.ToString();
                }
                catch { DPumpCatelog.Manufacture = ""; }
                try
                {
                    var Cspec = h1.DocumentNode.SelectSingleNode("//div[@class='product-images-slide']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Cspec);
                    DPumpCatelog.Image_URL = h2.DocumentNode.SelectSingleNode("//img").Attributes["src"].Value.ToString();
                }
                catch { DPumpCatelog.Image_URL = ""; }

                try
                {
                    var Cdesc = h1.DocumentNode.SelectSingleNode("//div[@class='resp-tabs-container overview-tab']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(Cdesc);
                    DPumpCatelog.Description = h2.DocumentNode.SelectSingleNode("//p").InnerText.ToString();
                }
                catch { DPumpCatelog.Description = ""; }
            }
            catch { }
            finally {
                InsertProduct();
                DPumpCatelog = null;
                h1 = null;
                h2 = null;
                h3 = null;
                DownloadedString = string.Empty;
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
                            sqlCommand = new SqlCommand("select id,productlink from tbl_pumpcatalog_product where isnull(Category,'')='' and id  between " + start + " and " + end + " ", sqlConnection);
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
        private static void InsertProduct()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "update tbl_pumpcatalog_product set Category=@Category , Title=@Title , Price=@Price , Manufacture=@Manufacture , PartNo=@PartNo , MPN=@MPN , UPC=@UPC , Description=@Description , Specification=@Specification , Image_URL=@Image_URL where id=@sourceid";
                        cmd.Parameters.AddWithValue(@"Category", DPumpCatelog.Category != null ? ReplaceString.PutString(DPumpCatelog.Category) : "");
                        cmd.Parameters.AddWithValue(@"Title", DPumpCatelog.Title != null ? ReplaceString.PutString(DPumpCatelog.Title) : "");
                        cmd.Parameters.AddWithValue(@"Price", DPumpCatelog.Price != null ? ReplaceString.PutString(DPumpCatelog.Price) : "");
                        cmd.Parameters.AddWithValue(@"Manufacture", DPumpCatelog.Manufacture != null ? ReplaceString.PutString(DPumpCatelog.Manufacture) : "");
                        cmd.Parameters.AddWithValue(@"PartNo", DPumpCatelog.PartNo != null ? ReplaceString.PutString(DPumpCatelog.PartNo) : "");
                        cmd.Parameters.AddWithValue(@"MPN", DPumpCatelog.MPN != null ? ReplaceString.PutString(DPumpCatelog.MPN) : "");
                        cmd.Parameters.AddWithValue(@"UPC", DPumpCatelog.UPC != null ? ReplaceString.PutString(DPumpCatelog.UPC) : "");
                        cmd.Parameters.AddWithValue(@"Description", DPumpCatelog.Description != null ? ReplaceString.PutString(DPumpCatelog.Description) : "");
                        cmd.Parameters.AddWithValue(@"Specification", DPumpCatelog.Specification != null ? ReplaceString.PutString(DPumpCatelog.Specification) : "");
                        cmd.Parameters.AddWithValue(@"Image_URL", DPumpCatelog.Image_URL != null ? ReplaceString.PutString(DPumpCatelog.Image_URL) : "");
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.ExecuteNonQuery();
                    }
                }

            }
            catch(Exception ex) {
                Console.WriteLine("Error While Updating the Product");
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
        public static void DownloadHTMLString()
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
    public class PumpCatelog
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string ProductDesc { get; set; }
        public string Price { get; set; }
        public string Manufacture { get; set; }
        public string PartNo { get; set; }
        public string MPN { get; set; }
        public string UPC { get; set; }
        public string Description { get; set; }
        public string Specification { get; set; }
        public string Image_URL { get; set; }

    }
}
