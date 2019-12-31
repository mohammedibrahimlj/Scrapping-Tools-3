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
        private static string filePath = "platt.txt";
        private static platt platt;
        static void Main(string[] args)
        {
            //SearchPageExtract();
            ProductExtract();
        }
        private static void SearchPageExtract()
        {
            try
            {
                Console.Title = Name;
                while (true)
                {
                    try
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
                                    SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim() + navigate.Replace("xpgnox", ppage.ToString());

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
                    catch { }
                    }
               
            }
            catch { }
        }

        private static void ProductExtract()
        {
            try
            {
                Console.Title = Name;
                while (true)
                {
                    DataSet data = GetData(2);
                    if (data != null)
                    {
                        TotalCount = data.Tables[0].Rows.Count;
                        num = 1;
                        foreach (DataRow row in data.Tables[0].Rows)
                        {
                            try
                            {
                                TotalPage = 0;
                                SourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                //modifiedlink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim() + navigate.Replace("xpgnox", ppage.ToString());
                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                DownloadString();
                                ProductPageParse();
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
        public static void ProductPageParse()
        {
            platt = new platt();
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var categoryhtml = h1.DocumentNode.SelectSingleNode("//div[@class='breadCrumPaddingAll']").InnerHtml.ToString();

                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(categoryhtml);

                    platt.Category = h2.DocumentNode.SelectNodes("//span").Where(w=>w.InnerText.Trim()!="").Select(s => s.InnerText.ToString()).Aggregate((a, b) => a +" | "+ b).ToString();
                    platt.Category = platt.Category.Remove(0, platt.Category.Split('|')[0].Length+2).Trim();

                }
                catch { platt.Category = ""; }
                try
                {
                    platt.ProductTitle = h1.DocumentNode.SelectSingleNode("//span[@class='lblProdHeadline lblProdTitle']").InnerText.ToString();

                }
                catch { platt.ProductTitle = ""; }
                try
                {
                    var prodettable = h1.DocumentNode.SelectSingleNode("//table[@class='productDetailsTable']").InnerHtml.ToString();

                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(prodettable);
                    foreach (var tr in h2.DocumentNode.SelectNodes("//tr"))
                    {
                        h3 = null;
                        h3 = new HtmlDocument();
                        h3.LoadHtml(tr.InnerHtml);
                        var tddata = h3.DocumentNode.SelectNodes("//td");
                        for (int i = 0; i < tddata.Count; i++)
                        {
                            if (tddata[i].InnerText.Contains("Mfr"))
                            {
                                platt.mfr = tddata[i + 1].InnerText.ToString().Trim().Replace("/r","").Replace("/n","").Replace("Products","").Replace("Mfg. Site", "").Trim();
                            }
                            else if (tddata[i].InnerText.Contains("UPC"))
                            {
                                platt.UPC = "#" + tddata[i + 1].InnerText.ToString().Trim();
                            }
                            else if (tddata[i].InnerText.Contains("Item"))
                            {
                                platt.item = "#"+tddata[i + 1].InnerText.ToString().Trim();
                            }
                            else if (tddata[i].InnerText.Contains("Cat"))
                            {
                                platt.CAT = "#"+tddata[i + 1].InnerText.ToString().Trim();
                            }
                        }

                    }
                }
                catch { }
                try
                {
                    var catlinkhtml = h1.DocumentNode.SelectSingleNode("//tr[@class='cutSheetContainer']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(catlinkhtml);
                    platt.Catelog = h2.DocumentNode.SelectNodes("//a[@class='docItemSm']").Select(s => s.Attributes["href"].Value).Aggregate((a, b) => a + " | " + b).ToString();
                }
                catch { platt.Catelog = ""; }
                try
                {
                    //lblDetailDes
                    platt.Long_desc = h1.DocumentNode.SelectSingleNode("//span[@id='lblDetailDes']").InnerText.ToString();
                }
                catch { platt.Long_desc = ""; }
                try
                {
                    platt.Price = h1.DocumentNode.SelectSingleNode("//span[@class='ProductPriceOrderBox']").InnerText.ToString();
                }
                catch { platt.Price = ""; }
                try
                {
                    var pricediv = h1.DocumentNode.SelectSingleNode("//div[@id='rowPrice']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(pricediv);
                    platt.UOM = h2.DocumentNode.SelectNodes("//span")[h2.DocumentNode.SelectNodes("//span").Count - 1].InnerText.ToString().Trim();
                }
                catch { platt.UOM = ""; }

                try
                {
                    var spectable = h1.DocumentNode.SelectSingleNode("//div[@class='seoSpecTables']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(spectable);
                    string[] tablenames= { "seoSpecTable1", "seoSpecTable2" };
                    foreach (var tablen in tablenames)
                    {
                        var table = h2.DocumentNode.SelectSingleNode("//table[@class='" + tablen + "']").InnerHtml.ToString();
                        if(table!=null)
                        {
                            h4 = null;
                            h4 = new HtmlDocument();
                            h4.LoadHtml(table.ToString());
                            foreach (var tr in h4.DocumentNode.SelectNodes("//tr"))
                            {
                                h3 = null;
                                h3 = new HtmlDocument();
                                h3.LoadHtml(tr.InnerHtml.ToString());
                                var tddata = h3.DocumentNode.SelectNodes("//td");
                                int j = 0;
                                while(true)
                                //for (int j = 0; j < tddata.Count; j++)
                                {
                                    try
                                    {
                                        if (tddata[j].InnerText.Trim() != "Platt Item:" && tddata[j].InnerText.Trim() != "UPC:" && tddata[j].InnerText.Trim() != "Cat:")
                                        {
                                            platt.Spec += tddata[j].InnerText.Trim() + " " + tddata[j + 1].InnerText.Trim() + " | ";
                                            j += 1;
                                        }
                                    }
                                    catch { }
                                    j += 1;
                                    if (tddata.Count == j)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
                try
                {
                    platt.image_link = h1.DocumentNode.SelectNodes("//img[@class='FancyProducts zoomImgLink prodCurrentDetailImg']").Select(s => s.Attributes["src"].Value).Aggregate((a, b) => a + " | " + b).ToString();
                }
                catch { }
                try
                {
                    platt.AlsoKnowAs = h1.DocumentNode.SelectSingleNode("//span[@id='ctl00_ctl00_MainContent_uxProduct_lblSEOAlsoKnow']").InnerText.ToString();
                }
                catch { platt.AlsoKnowAs = ""; }
            }
            catch { }
            finally {
                InsertProduct();
                platt = null;
                DownloadedString = "";
                h1 = null;
                h2 = null;
                h3 = null;
                h4 = null;
            }
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
                            Log(Homeurl + link+"\n");
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
                            sqlCommand = new SqlCommand("select productid,productlink from tbl_platt_product where productid  between " + start + " and " + end + " and  isnull(Category,'')=''", sqlConnection);
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
                        cmd.CommandText = "update tbl_platt_product set Category=@Category,ProductTitle=@ProductTitle,mfr=@mfr,item=@item,UPC=@UPC,CAT=@CAT,Long_desc=@Long_desc,Price = @Price,UOM = @UOM,Spec = @Spec,image_link = @image_link,Catelog = @Catelog,AlsoKnowAs = @AlsoKnowAs where productid=@sourceid";
                        cmd.Parameters.AddWithValue(@"Category", platt.Category != null ? ReplaceString.PutString(platt.Category) : "");
                        cmd.Parameters.AddWithValue(@"ProductTitle", platt.ProductTitle != null ? ReplaceString.PutString(platt.ProductTitle) : "");
                        cmd.Parameters.AddWithValue(@"mfr", platt.mfr != null ? ReplaceString.PutString(platt.mfr) : "");
                        cmd.Parameters.AddWithValue(@"item", platt.item != null ? ReplaceString.PutString(platt.item) : "");
                        cmd.Parameters.AddWithValue(@"UPC", platt.UPC != null ? ReplaceString.PutString(platt.UPC) : "");
                        cmd.Parameters.AddWithValue(@"CAT", platt.CAT != null ? ReplaceString.PutString(platt.CAT) : "");
                        cmd.Parameters.AddWithValue(@"Long_desc", platt.Long_desc != null ? ReplaceString.PutString(platt.Long_desc) : "");
                        cmd.Parameters.AddWithValue(@"Price", platt.Price != null ? ReplaceString.PutString(platt.Price) : "");
                        cmd.Parameters.AddWithValue(@"UOM", platt.UOM != null ? ReplaceString.PutString(platt.UOM) : "");
                        cmd.Parameters.AddWithValue(@"Spec", platt.Spec != null ? ReplaceString.PutString(platt.Spec) : "");
                        cmd.Parameters.AddWithValue(@"image_link", platt.image_link != null ? ReplaceString.PutString(platt.image_link) : "");
                        cmd.Parameters.AddWithValue(@"Catelog", platt.Catelog != null ? ReplaceString.PutString(platt.Catelog) : "");
                        cmd.Parameters.AddWithValue(@"AlsoKnowAs", platt.AlsoKnowAs != null ? ReplaceString.PutString(platt.AlsoKnowAs) : "");
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
        public static void Log(string message)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            using (StreamWriter streamWriter = File.AppendText(filePath))
            {
                streamWriter.WriteLine(message);
                streamWriter.Close();
            }
        }
    }
    public class platt
    {
        public string Category { get; set; }
        public string ProductTitle { get; set; }
        public string mfr { get; set; }
        public string item { get; set; }
        public string UPC { get; set; }
        public string CAT { get; set; }
        public string Long_desc { get; set; }
        public string Price { get; set; }
        public string UOM { get; set; }
        public string Spec { get; set; }
        public string image_link { get; set; }
        public string Catelog { get; set; }
        public string AlsoKnowAs { get; set; }
    }
}
