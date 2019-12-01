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

namespace fastenal.com
{
    class Program
    {
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString(),Homeurl= "https://www.fastenal.com",ProductURL= "https://www.fastenal.com/products/details/";
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString();
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, DownloadedString;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static readonly string PageQuery = "&page=XpageX&pageSize=48&exactSkuMatchLevel=useData";
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
                            ppcount = 1;
                            SourceLink = string.Empty;
                            sourceid = 0;
                            Console.WriteLine("Processing link " + num + " of " + TotalCount);
                            SourceLink = row.ItemArray[1].ToString().Replace("&amp;","&").Trim();
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            DownloadHTMLString();
                            FastenalSearchProLinkPage();
                            //SearchProLink();
                            //ProcessProductHTML();
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
        private static void FastenalSearchProLinkPage()
        {
            ppage = 0;
            PageLoop:
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//span[@class='pagination-text']").InnerText.ToString().Split(' ')[0].ToString());

                try
                {
                    var productitem = h1.DocumentNode.SelectNodes("//div[@class='media-item-row']");
                    foreach (var protag in productitem)
                    {
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(protag.InnerHtml.ToString());
                        var productlinks = h2.DocumentNode.SelectNodes("//a");
                        
                        foreach(var product in productlinks)
                        {
                            ppage += 1;
                            InsertProductLink(Homeurl + product.Attributes["href"].Value.ToString());
                        }
                    }
                }
                catch { }
                if (ppage < TotalPage)
                {
                    ppcount += 1;
                    FastenalUpdateSearchLinkStatus(0);
                    DownloadHTMLString();
                    goto PageLoop;
                }
                else
                {
                    FastenalUpdateSearchLinkStatus(1);
                }

            }
            catch { }
        }
        private static void SearchProLink()
        {
            try
            {
                TotalPage = 0;
                ppcount = 0;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var s1Link = h1.DocumentNode.SelectSingleNode("//div[@class='category-container']").InnerHtml.ToString();
                    if (!string.IsNullOrEmpty(s1Link))
                    {
                        FastenalUpdateSearchLinkStatus(1);
                        //h2 = null;
                        //h2 = new HtmlDocument();
                        //h2.LoadHtml(s1Link.ToString());
                        //var searchLinks = h2.DocumentNode.SelectNodes("//a").ToList();
                        //foreach (var searchlink in searchLinks)
                        //{
                        //    try
                        //    {
                        //        if (searchlink.Attributes["href"].Value.Contains("product"))
                        //            InsertSearchLink(Homeurl + "" + searchlink.Attributes["href"].Value.ToString());
                        //    }
                        //    catch { }
                        //}
                    }
                }
                catch { }


                try
                {
                    var s2link = h1.DocumentNode.SelectNodes("//div[@class='product__family--tile']");

                    if (s2link != null)
                    {
                        FastenalUpdateSearchLinkStatus(1);
                        //foreach (var searchlink in s2link)
                        //{
                        //    h2 = null;
                        //    h2 = new HtmlDocument();
                        //    h2.LoadHtml(searchlink.InnerHtml.ToString());
                        //    var link = Homeurl + h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString();
                        //    try
                        //    {
                        //        TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//div[@class='product--count']").InnerText.ToString().Replace("items", "").Trim());
                        //    }
                        //    catch { }
                        //    InsertSearchLink(link);
                        //}
                    }
                }
                catch { }
                try
                {
                    var s3link = h1.DocumentNode.SelectSingleNode("//div[@id='counterbook_sections']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(s3link);
                    var plinks = h2.DocumentNode.SelectNodes("//td[@class='cb-sku']");
                    ppcount = plinks.Count();
                    foreach (var plink in plinks)
                    {
                        InsertProductLink(ProductURL + plink.InnerText.ToString().Replace("\n","").Trim());
                    }

                    FastenalUpdateSearchLinkStatus(1);
                }
                catch { }
            }
            catch { }
            finally
            {
                
                h1 = null;
                h2 = null;
            }
        }
        private static void FastenalInsertSearchLink(string productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "insert into tbl_fastenal_SearchLink (SearchTitle,TotalLink) values(@SearchTitle,@TotalLink)";
                        cmd.Parameters.AddWithValue("@SearchTitle", productlink);
                        cmd.Parameters.AddWithValue("@TotalLink", TotalPage);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while inserting search link");
            }
        }
        private static void FastenalInsertProductLink(string productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "insert into tbl_fastenal_product (Producturl,sourceid) values(@productlink,@sourceid)";
                        cmd.Parameters.AddWithValue("@productlink", productlink);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while inserting Product");
            }
        }

        private static void FastenalUpdateSearchLinkStatus(int iscompleted)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "update tbl_fastenal_SearchLink set iscompleted=@iscompleted, TotalLink=@TotalLink, processingpage=@processingpage where searchid=@sourceid";
                        cmd.Parameters.AddWithValue("@TotalLink", TotalPage);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.Parameters.AddWithValue("@processingpage", ppcount);
                        cmd.Parameters.AddWithValue("@iscompleted", iscompleted);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error While update status.....");
            }
        }
        public static void DownloadHTMLString()
        {
            try
            {
                DownloadedString = string.Empty;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink + PageQuery.Replace("XpageX", ppcount.ToString()));
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                //request.Referer = "https://www.fastenal.com/product/abrasives/coated-and-non-woven-abrasives/600955?categoryId=600955&level=2&isExpanded=true";
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"JSESSIONID=6Fptw7ybGcwqge4f2nSU7LL-.d36f637b-91a9-3b2b-9fa6-50af87272845; NEW_SEARCH_EXPERIENCE=0.84647965; _ga=GA1.3.1995995924.1575084835; _gid=GA1.3.113100706.1575084835; _dc_gtm_UA-2555468-6=1; __mauuid=040b86dd-2c6f-4937-83f0-19945f839d12; __mauuid=040b86dd-2c6f-4937-83f0-19945f839d12; _gali=cookieAgreementModal; mt.v=2.248824700.1575084833043; _gcl_au=1.1.238987160.1575084834; _ga=GA1.2.1995995924.1575084835; _gid=GA1.2.113100706.1575084835; _gat_UA-2555468-6=1; __mauuid=040b86dd-2c6f-4937-83f0-19945f839d12; _hjid=3c3408f5-fe33-485c-ab61-cd27d69bfa9c; COOKIE_AGREEMENT=1");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                DownloadedString = streamReader.ReadToEnd();
            }
            catch (WebException ex)
            {

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
                            sqlCommand = new SqlCommand("select searchid,SearchTitle from tbl_fastenal_SearchLink where  iscompleted=0 ", sqlConnection);
                            break;
                        case 2:
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
    }
}
