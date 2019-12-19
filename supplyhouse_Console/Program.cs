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
using System.Threading.Tasks;

namespace supplyhouse_Console
{
    class Program
    {
        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(),Homeurl= "https://www.supplyhouse.com/";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, DSourceLink = string.Empty, CookieString = string.Empty, searchquery = string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        static supplyhouse supplyhouse;
        static void Main(string[] args)
        {
            Console.Title = Name;
            while (true)
            {
                DataSet data = GetData(2);
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
                            SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                            SourceLink = "https://www.supplyhouse.com/3M-5631604-CFS9112-S-Sediment-Reduction-Retrofit-Replacement-Cartridge-w-Scale-Inhibition";
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            DownloadHTMLString();
                            //ProcessSupplySearchLink();
                            ProcessProduct();
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

        public static void ProcessProduct()
        {
            supplyhouse = new supplyhouse();
            try
            {
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var breadcrum = h1.DocumentNode.SelectSingleNode("//div[@id='breadcrumbs']").InnerHtml.ToString();
                    h2 = new HtmlDocument();
                    h2.LoadHtml(breadcrum);
                    supplyhouse.category = h2.DocumentNode.SelectNodes("//a").Select(s => s.Attributes["data-target-name"].Value).Aggregate((a, b) => a + " | " + b).ToString();

                }
                catch { supplyhouse.category = ""; }
                try
                {
                    supplyhouse.Title = h1.DocumentNode.SelectSingleNode("//strong[@class='product-name']").InnerText.ToString();
                }
                catch
                {
                    supplyhouse.Title = "";
                }
                try
                {
                    supplyhouse.SKU ="#"+ h1.DocumentNode.SelectSingleNode("//div[@class='desc-sku']").InnerText.ToString().Replace("SKU", "").Replace(":","").Trim();
                }
                catch
                {
                    supplyhouse.SKU = "";
                }
                try
                {
                    supplyhouse.BrandName = h1.DocumentNode.SelectSingleNode("//div[@class='desc-brand ']").InnerText.ToString().Replace("Brand", "").Replace(":", "").Trim();
                }
                catch
                {
                    supplyhouse.BrandName = "";
                }
                try
                {
                    supplyhouse.Price = h1.DocumentNode.SelectSingleNode("//span[@class='unit-price-text  ']").InnerText.ToString();

                }
                catch
                {
                    supplyhouse.Price = "";
                }
                try
                {
                    supplyhouse.UOM = h1.DocumentNode.SelectSingleNode("//div[@class='unit-price']").InnerText.ToString().Replace(supplyhouse.Price, "").Trim();
                }
                catch(Exception ex)
                {
                    supplyhouse.UOM = "";
                }
                try
                {
                    supplyhouse.Description = h1.DocumentNode.SelectSingleNode("//div[@class='prod-desc-content section-content']").InnerText.ToString().Replace("\r", " ").Replace("\n", " ").Trim();
                }
                catch { supplyhouse.Description = ""; }
                try
                {
                    var descfeature = h1.DocumentNode.SelectNodes("//div[@class='product-feature']");
                    foreach (var profea in descfeature)
                    {
                        h2 = null;
                        h2 = new HtmlDocument();
                        supplyhouse.Specification += profea.InnerText.ToString().Replace("\r","").Replace("\n","").Trim() + " | ";
                    }
                }
                catch { supplyhouse.Specification = ""; }
                try
                {
                    //product-images-container
                    var imgdiv = h1.DocumentNode.SelectSingleNode("//div[@class='product-images-container']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(imgdiv);
                    supplyhouse.ImageLink = h2.DocumentNode.SelectNodes("//img").Select(s=>s.Attributes["src"].Value.ToString()).Aggregate((a,b)=>a+" | "+b).ToString();

                }
                catch { supplyhouse.ImageLink = ""; }
            }
            catch
            {
            }
            finally
            {
                UpdateProductData();
                DownloadedString = string.Empty;
                h1 = null;
                h2 = null;
            }
        }
        public static void ProcessSupplySearchLink()
        {
            try
            {
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                var SearchLink = h1.DocumentNode.SelectNodes("//div[@class='subcat']");
                Console.WriteLine("Total Search Link Found :" + SearchLink.Count());
                foreach (var slink in SearchLink)
                {
                    try
                    {
                        h2 = new HtmlDocument();
                        h2.LoadHtml(slink.InnerHtml.ToString());
                        var link = h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString().Trim();
                        if (link.Contains("https://www.supplyhouse.com/"))
                        {
                            insertSearchLink(link);
                        }
                        else
                        {
                            insertSearchLink(Homeurl+link);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally {
                UpdateSearchLinkStatus(1);
                h1 = null;
                h2 = null;
                DownloadedString = string.Empty;
            }
        }
        private static void insertSearchLink(string Searchlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd1 = new SqlCommand("insert into tbl_supplyhouse_SearchLink (SearchURL) values(@searchLink)", con))
                    {
                        cmd1.Parameters.AddWithValue("@searchLink", Searchlink);
                        cmd1.ExecuteNonQuery();
                    }

                }
            }

            catch {
                Console.WriteLine("Error while inserting the search link..");
            }
        }
        private static void UpdateSearchLinkStatus(int iscompleted)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "update tbl_supplyhouse_SearchLink set TotalLink=@TotalLink, processingpage=@processingpage,iscompleted=@iscompleted where searchid=@sourceid";
                        cmd.Parameters.AddWithValue("@TotalLink", TotalCount);
                        cmd.Parameters.AddWithValue("@sourceid", sourceid);
                        cmd.Parameters.AddWithValue("@processingpage", ppcount);
                        cmd.Parameters.AddWithValue("@iscompleted", iscompleted);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error While Executing search link update.....");
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
                request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Add("Sec-Fetch-Site", @"none");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
               // request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"denakhif=NO; denaf=false_8_?; denai=14.140.167.122; ul=false; cto_lwid=bd8dcf00-da4f-41ae-8e44-091011165bed; _ga=GA1.2.270769402.1575584832; _gid=GA1.2.1107357800.1575584832; LPVID=VmZmIwZjVkOTZhZjVlYjYz; LPSID-7347571=QrDG_JSGSVmLSfocWpw2IA; JSESSIONID=47AF51364C61ECC5F7ED129D79BBAB56.jvm2");
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
                            sqlCommand = new SqlCommand("select searchid,SearchURL from tbl_supplyhouse_SearchLink where iscompleted=0 ", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select productid,Producturl from tbl_supplyhouse_product where isnull(category,'')=''  and productid between "+start +" and "+end+" ", sqlConnection);
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
        public static void UpdateProductData()
        {
            try
            {
                Console.WriteLine("Update product data !!");
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("update [dbo].[tbl_supplyhouse_product] set category=@category,Title=@Title,SKU=@SKU,BrandName=@BrandName,Price=@Price,UOM=@UOM,Description=@Description,Specification=@Specification,ImageLink=@ImageLink where ProductID=@id", sqlConnection);
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Parameters.AddWithValue("@category", (supplyhouse.category == null) ? "" :ReplaceString.PutString (supplyhouse.category));
                    sqlCommand.Parameters.AddWithValue("@Title", (supplyhouse.Title == null) ? "" : ReplaceString.PutString(supplyhouse.Title));
                    sqlCommand.Parameters.AddWithValue("@SKU", (supplyhouse.SKU == null) ? "" : ReplaceString.PutString(supplyhouse.SKU));
                    sqlCommand.Parameters.AddWithValue("@BrandName", (supplyhouse.BrandName == null) ? "" : ReplaceString.PutString(supplyhouse.BrandName));
                    sqlCommand.Parameters.AddWithValue("@Price", (supplyhouse.Price == null) ? "" : ReplaceString.PutString(supplyhouse.Price));
                    sqlCommand.Parameters.AddWithValue("@UOM", (supplyhouse.UOM == null) ? "" : ReplaceString.PutString(supplyhouse.UOM));
                    sqlCommand.Parameters.AddWithValue("@Description", (supplyhouse.Description == null) ? "" : ReplaceString.PutString(supplyhouse.Description));
                    sqlCommand.Parameters.AddWithValue("@Specification", (supplyhouse.Specification == null) ? "" : ReplaceString.PutString(supplyhouse.Specification));
                    sqlCommand.Parameters.AddWithValue("@ImageLink", (supplyhouse.ImageLink == null) ? "" : ReplaceString.PutString(supplyhouse.ImageLink));
                    sqlCommand.Parameters.AddWithValue("@id", sourceid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateing product data failed!!");
            }
        }

    }

    public class supplyhouse
    {
        public string category { get; set; }
        public string Title { get; set; }
        public string SKU { get; set; }
        public string BrandName { get; set; }
        public string Price { get; set; }
        public string UOM { get; set; }
        public string Description { get; set; }
        public string Specification { get; set; }
        public string ImageLink { get; set; }
    }
}
