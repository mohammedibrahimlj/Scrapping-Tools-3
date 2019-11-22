using HtmlAgilityPack;
using HTMLCodeReplacer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace abf.store
{
    class Program
    {
        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), PLink = "https://www.arrow.com/productsearch/productlinesearchresultajax?page=XpageX&q=&prodline=XProductX&perPage=100";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalLink,TotalProcessLink, num=0;
        private static string SourceLink,searchparam= "?mx=50&p=xpgnox",Plink=string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static bool InitilCount = false;
        private static abfstore Dabfstore;
        private static readonly string Homeurl = "https://www.abf.store";
        static void Main(string[] args)
        {


            Console.Title = Name;
            while (true)
            {
                DataSet data = GetData(2);
                if (data != null)
                {
                    TotalProcessLink = data.Tables[0].Rows.Count;
                    if (TotalProcessLink == 0)
                        break;
                    num = 1;
                    foreach (DataRow row in data.Tables[0].Rows)
                    {
                        try
                        {
                            ppage = 1;
                            TotalLink = 0;
                            Plink = string.Empty;
                            SourceLink = string.Empty;
                            sourceid = 0;
                            Console.WriteLine("Processing link " + num + " of " + TotalProcessLink);
                            SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            DownloadString();
                            GetProductData();
                            num++;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            Console.WriteLine("Completed...");
            Console.ReadKey();
 
        }
        private static void InitialScrap()
        {
            Console.Title = Name;
            while (true)
            {
                DataSet data = GetData(2);
                if (data != null)
                {
                    TotalProcessLink = data.Tables[0].Rows.Count;
                    num = 1;
                    foreach (DataRow row in data.Tables[0].Rows)
                    {
                        try
                        {
                            ppage = 1;
                            TotalLink = 0;
                            Plink = string.Empty;
                            SourceLink = string.Empty;
                            sourceid = 0;
                            Console.WriteLine("Processing link " + num + " of " + TotalProcessLink);
                            SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            TotalLink = string.IsNullOrEmpty(row.ItemArray[2].ToString()) ? 1 : int.Parse(row.ItemArray[2].ToString());
                            ppage = row.ItemArray[3].ToString() == "0" ? 1 : int.Parse(row.ItemArray[3].ToString());

                            Console.WriteLine("Processing page " + ppage);
                            Plink = SourceLink + searchparam.Replace("xpgnox", ppage.ToString());
                            DownloadString();
                            GetProductLink();
                            num++;
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        public static void GetProductLink()
        {
            try
            {
                loop:
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                var links = h1.DocumentNode.SelectNodes("//a[@class='block h3 semi-bold black']");
                TotalLink += links.Count();
                Console.WriteLine("found link " + TotalLink);
                foreach (var link in links)
                {
                    var plink = Homeurl+link.Attributes["href"].Value.ToString();
                    InsertProductLink(plink);
                }
                UpdateSearchLinkStatus();
                ppage += 1;
                Plink = string.Empty;
                Console.WriteLine("Processing page "+ ppage);
                Plink = SourceLink + searchparam.Replace("xpgnox", ppage.ToString());
                DownloadString();
                goto loop;
            }
            catch
            {
                Console.WriteLine("Error while processing the HTML");
            }
        }
        private static void GetProductData()
        {
            try
            {
                Dabfstore = new abfstore();
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    Dabfstore.Category = h1.DocumentNode.SelectSingleNode("//section[@class='border-bottom border-light-gray py2 px2 lg-px3 h5 semi-bold medium-gray']").InnerText.ToString().Trim().Replace("\n","").Replace("\r","");
                    Dabfstore.Category = Regex.Replace(Dabfstore.Category, @"s", "");
                }
                catch { Dabfstore.Category = ""; }
                try
                {

                    var ptd = h1.DocumentNode.SelectSingleNode("//div[@class='md-col-6 md-pr3 lg-pr4 pb4']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(ptd);
                    try
                    {
                        Dabfstore.Title = h2.DocumentNode.SelectSingleNode("//h1[@class='bold m0']").InnerText.ToString();
                    }
                    catch { Dabfstore.Title = ""; }
                    try
                    {
                        Dabfstore.ProductDesc = h2.DocumentNode.SelectSingleNode("//h4[@class='gray mt0 mb2']").InnerText.ToString();
                    }
                    catch { Dabfstore.ProductDesc = ""; }
                }
                catch { }
                try
                {
                    Dabfstore.image_url = h1.DocumentNode.SelectNodes("//span[@class='block absolute top-0 right-0 bottom-0 left-0 z4 pointer lightbox']").Select(s => s.Attributes["href"].Value.ToString()).Aggregate((a, b) => a + " | " + b).ToString();
                }
                catch { Dabfstore.image_url = ""; }

                try
                {
                    //flex border-top border-light-gray py1
                    var specdata = h1.DocumentNode.SelectNodes("//li[@class='flex border-top border-light-gray py1']");
                    foreach (var li in specdata)
                    {
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(li.InnerHtml.ToString());
                        var spandata = h2.DocumentNode.SelectNodes("//span");
                        try
                        {
                            if (spandata[0].InnerText.Trim().ToString() == "Brand")
                                Dabfstore.Manufacture = spandata[1].InnerText.Trim().ToString();
                            else if (spandata[0].InnerText.Trim().ToString() == "Item Number")
                                Dabfstore.PartNo = spandata[1].InnerText.Trim().ToString();
                            else if (spandata[0].InnerText.Trim().ToString().Contains("MPN"))
                                Dabfstore.MNP = spandata[1].InnerText.Trim().ToString();
                            else
                                Dabfstore.Spec += spandata[0].InnerText.Trim().ToString() + " : " + spandata[1].InnerText.Trim().ToString() + " | ";
                        }
                        catch { }

                    }
                }
                catch { }
            }
            catch { }
            finally {
                InsertProduct();
                Dabfstore = null;
                h1 = null;
                h2 = null;
                h3 = null;
            }
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
                        cmd.CommandText = "update tbl_abf_store_product set Category=@Category,Title=@Title,ProductDesc=@ProductDesc,Manufacture=@Manufacture,PartNo=@PartNo,MNP=@MNP,Spec=@Spec,image_url=@image_url where productid = @sourceid";
                        cmd.Parameters.AddWithValue("@Category", Dabfstore.Category != null ? ReplaceString.PutString(Dabfstore.Category) : "");
                        cmd.Parameters.AddWithValue("@Title", Dabfstore.Title != null ? ReplaceString.PutString(Dabfstore.Title) : "");
                        cmd.Parameters.AddWithValue("@ProductDesc", Dabfstore.ProductDesc != null ? ReplaceString.PutString(Dabfstore.ProductDesc) : "");
                        cmd.Parameters.AddWithValue("@Manufacture", Dabfstore.Manufacture != null ? ReplaceString.PutString(Dabfstore.Manufacture) : "");
                        cmd.Parameters.AddWithValue("@PartNo", Dabfstore.PartNo != null ? ReplaceString.PutString(Dabfstore.PartNo) : "");
                        cmd.Parameters.AddWithValue("@MNP", Dabfstore.MNP !=null ? ReplaceString.PutString(Dabfstore.MNP) : "");
                        cmd.Parameters.AddWithValue("@Spec", Dabfstore.Spec != null ? ReplaceString.PutString(Dabfstore.Spec) : "");
                        cmd.Parameters.AddWithValue("@image_url", Dabfstore.image_url != null ? ReplaceString.PutString(Dabfstore.image_url) : "");
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error While Updating the Product");
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
                        cmd.CommandText = "update tbl_abf_store_searchLink set TotalLink=@TotalLink, processingpage=@processingpage where searchid=@sourceid";
                        cmd.Parameters.AddWithValue("@TotalLink", TotalLink);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.Parameters.AddWithValue("@processingpage", ppage);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error While Executing search link update.....");
            }
        }
        private static void InsertProductLink(string Productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into tbl_abf_store_product (Producturl,sourceid)"
                        + "values(@Productlink,@sourceid)", con);
                    cmd.Parameters.AddWithValue("@Productlink", Productlink.Trim());
                    cmd.Parameters.AddWithValue("@sourceid", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while inserting product link....");
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
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                request.Referer = "https://www.abf.store/s/en/";
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"ARRAffinity=9cbe6fe2d203e6a2bd32cfc740bb98cd94c9ede4406559c2de31c43c8c2e9ca3; ASP.NET_SessionId=dlzzrinhwqu2tlppkp4dlu1c; __RequestVerificationToken_L3M1=_CApJCjkQNT6T3wxDdKUDgKFDvuNIS__VWCCy1wK-veKCsf1q7el0tvoWRqSsS5XDfzQ7IeiKt4O98SVNOSU-q-zGOaOBabux7YFtvfXBcY1; welcomed=true; _ga=GA1.2.1349769389.1574373680; _gid=GA1.2.1838566965.1574373680; CookieConsent={stamp:'IfETb/XUo69tlSJbIgCPVeOJc8pEBqRVbhxaSYZTr3q6ijaIE5iWXw=='%2Cnecessary:true%2Cpreferences:true%2Cstatistics:true%2Cmarketing:true%2Cver:1%2Cutc:1574339588458}; _gcl_au=1.1.11347904.1574373789; ai_user=+upin|2019-11-21T22:03:09.590Z; _hjid=2716cb71-da28-4ebd-a433-076a0138eb42; pll_language=en; _gali=categories; _gat_UA-97230176-1=1");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                DownloadedString = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                            sqlCommand = new SqlCommand("select searchid,SearchLink,TotalLink,processingpage from tbl_abf_store_searchLink where Iscompleted=0 and searchid between " + start + " and " + end + " ", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select productid,Producturl from tbl_abf_store_product where isnull(Category,'')='' and productid  between " + start + " and " + end + " ", sqlConnection);
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
    public class abfstore
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string ProductDesc { get; set; }
        public string Manufacture { get; set; }
        public string PartNo { get; set; }
        public string MNP { get; set; }
        public string desc { get; set; }
        public string Spec { get; set; }
        public string image_url { get; set; }
    }
}
