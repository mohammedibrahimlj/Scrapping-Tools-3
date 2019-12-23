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


namespace columbiapipe.com
{
    class Program
    {
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), navigate = "?Nrpp=1000&No=xpgnox";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, CookieString = string.Empty, searchquery = string.Empty, Scode = string.Empty, DownloadedString = string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static bool initial = false;
        private static readonly string Homeurl = "https://www.columbiapipe.com";
        private static string filePath = "columbiapipe.txt";
        public static columbiapipe columbiapipe;
        static void Main(string[] args)
        {
            //SearchPageExtract();
            ProductExtract();
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
                                sourceid = 0;
                                SourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                //SourceLink = "https://www.columbiapipe.com/cutting-tool-oil/12399";
                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                DownloadString();
                                ProductParse();
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
        private static void ProductParse()
        {
            columbiapipe = new columbiapipe();
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    columbiapipe.ProTitle = h1.DocumentNode.SelectSingleNode("//h1[@class='pdp-title']").InnerText.ToString().Trim();
                }
                catch { columbiapipe.ProTitle = ""; }
                try
                {
                    var breadcrumdata = h1.DocumentNode.SelectSingleNode("//ol[@class='breadcrumb']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(breadcrumdata);
                    columbiapipe.Category = h2.DocumentNode.SelectNodes("//li").Select(s => s.InnerText.ToString().Trim()).Aggregate((a, b) => a + " | " + b).ToString().Trim();
                }
                catch { columbiapipe.Category = ""; }
                try
                {
                    var htmldata = h1.DocumentNode.SelectSingleNode("//ul[@class='pdp-skus']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(htmldata);
                    foreach (var data in h2.DocumentNode.SelectNodes("//li"))
                    {
                        if (data.InnerText.Contains("Mfr #:"))
                        {
                            columbiapipe.mfr = "#"+data.InnerText.Replace("Mfr #:", "").ToString().Trim();
                        }
                        else if (data.InnerText.Contains("Item #:"))
                        {
                            columbiapipe.item = "#" + data.InnerText.Replace("Item #:", "").ToString().Trim();
                        }
                        else if (data.InnerText.Contains("UPC:"))
                        {
                            columbiapipe.upc = "#" + data.InnerText.Replace("UPC:", "").ToString().Trim();
                        }
                        else if (data.InnerText.Contains("Brand:"))
                        {
                            columbiapipe.brand = data.InnerText.Replace("Brand:", "").ToString().Trim();
                        }
                        else if (data.InnerText.Contains("Min Order Qty:"))
                        {
                            columbiapipe.MOQ = "#" + data.InnerText.Replace("Min Order Qty:", "").ToString().Trim();
                        }
                        else if (data.InnerText.Contains("Qty Interval:"))
                        {
                            columbiapipe.QtyInter = "#" + data.InnerText.Replace("Qty Interval:", "").ToString().Trim();
                        }
                    }
                }
                catch { }
                try
                {
                    var deschtml = h1.DocumentNode.SelectSingleNode("//div[@class='col-md-4 col-sm-6 col-xs-12 pdp-intro']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(deschtml);
                    columbiapipe.LongDesc = h2.DocumentNode.SelectSingleNode("//p").InnerText.ToString().Trim();
                }
                catch { columbiapipe.LongDesc = ""; }
                try
                {
                    columbiapipe.Price = h1.DocumentNode.SelectSingleNode("//div[@class='pdp-price text-center']").InnerText.ToString().Trim().Split('/')[0].ToString().Trim();
                }
                catch { columbiapipe.Price = ""; }
                try
                {
                    columbiapipe.UOM = h1.DocumentNode.SelectSingleNode("//div[@class='pdp-price text-center']").InnerText.ToString().Trim().Split('/')[1].ToString().Trim();
                }
                catch { columbiapipe.UOM = ""; }
                try
                {
                    columbiapipe.Feature = h1.DocumentNode.SelectSingleNode("//div[@id='featuresSectionDetailsDiv']").InnerText.ToString().Trim().Replace("\n"," ");
                }
                catch { columbiapipe.Feature = ""; }
                try
                {
                    var tabledata = h1.DocumentNode.SelectSingleNode("//table[@class='table table-pdp table-mobile table-pdp-specs word-wrapping']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(tabledata);
                    foreach (var tr in h2.DocumentNode.SelectNodes("//tr"))
                    {
                        h3 = null;
                        h3 = new HtmlDocument();
                        h3.LoadHtml(tr.InnerHtml.ToString());
                        var td = h3.DocumentNode.SelectNodes("//td");
                        columbiapipe.Spec += td[0].InnerText.ToString().Trim() + " : " + td[1].InnerText.ToString().Trim() + " | ";

                    }
                }
                catch { }
                try
                {
                    var imgdiv = h1.DocumentNode.SelectSingleNode("//div[@class='pdp-gallery-thumbs row row-narrow']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(imgdiv);
                    columbiapipe.img_Link = h2.DocumentNode.SelectNodes("//img").Select(s => s.Attributes["data-src"].Value.ToString()).Aggregate((a, b) => a + " | " + b).ToString().Trim();
                }
                catch { }
            }
            catch { }
            finally {
                InsertProduct();
                h1 = null;
                h2 = null;
                h3 = null;
                h4 = null;
                DownloadedString = string.Empty;
                columbiapipe = null;
            }
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
                                ppage = int.Parse(row.ItemArray[2].ToString()) != 0 ? (int.Parse(row.ItemArray[2].ToString())+1) : 1;
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
            }
            catch { }
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
                request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
               //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=d0c5d1a1a210a4c17c84b627085991fe31576733099; _ga=GA1.2.1227370022.1576767372; _gid=GA1.2.1007622779.1576767372; _hjid=728f88a0-8bf7-43e4-9fcb-14906b4113e7; JSESSIONID=FbMhrJsuvxdc1Iwx4f_zdmeM4Tex9Gjv19W9c91Mzfi8l7-TUiMX!-367229809; rxVisitor=1576852192656UHQ8C3I177KDEUF2JQ6H4HO86EC25A56; guestAccessCount=2; guestLastAccess=1576852740078; dtLatC=2; rxvt=1576856031484|1576852192659; dtPC=2$52408286_32h-vHGGGVBLPKKHBAJGKOOMFIBMPMAPMGPUB; dtSa=true%7CKD%7C-1%7CPage%3A%20AMC-%7C-%7C1576854440655%7C52739186_414%7Chttps%3A%2F%2Fwww.columbiapipe.com%2Famc-%3FNrpp%3D30%26No%3D30%7CAMC%C2%AE%20-%20Columbia%20Pipe%3A%20Industrial%20Pipes%5Ec%20Fittings%20%26%20Connectors%7C1576853538127%7C%7C; dtCookie=2$6310E49F3814883E86A7A8F998F91AD4|5f735d65411e6c47|1");
                request.Timeout = 30000;
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
        public static void GetSearchLinkInitial()
        {
            try
            {
                while (true)
                {
                    if (!string.IsNullOrEmpty(DownloadedString))
                    {
                        h1 = null;
                        h1 = new HtmlDocument();
                        h1.LoadHtml(DownloadedString);
                        try
                        {
                            if (initial)
                            {
                                var TotalPagedata = h1.DocumentNode.SelectSingleNode("//ul[@class='pagination']").InnerHtml.ToString();
                                h2 = null;
                                h2 = new HtmlDocument();
                                h2.LoadHtml(TotalPagedata);
                                TotalPage = int.Parse(h2.DocumentNode.SelectSingleNode("//li").InnerText.ToString().Split(' ')[h2.DocumentNode.SelectSingleNode("//li").InnerText.ToString().Split(' ').Count() - 1].ToString().Trim());
                                initial = false;
                            }
                        }
                        catch { }
                        try
                        {
                            var links = h1.DocumentNode.SelectNodes("//div[@class='item-title fade_description']");
                            Console.WriteLine("Total Link found " + links.Count());
                            foreach (var link in links)
                            {
                                h2 = null;
                                h2 = new HtmlDocument();
                                h2.LoadHtml(link.InnerHtml.ToString());
                                //Log(Homeurl + h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString());
                               // InsertProductLink(Homeurl + h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString());
                            }
                            UpdatePageStatus(ppage, TotalPage);
                            if (TotalPage > (ppage * 1000))
                            {
                                Console.WriteLine("Processing Page " + ppage);
                                SourceLink = modifiedlink + navigate.Replace("xpgnox", (ppage * 1000).ToString());
                                ppage = (ppage + 1);
                                DownloadString();
                            }
                            else
                            {
                                UpdateStatus(1);
                                break;
                            }

                        }
                        catch { }
                    }
                    else
                    {
                        UpdateStatus(1);
                        break;
                    }
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
                    SqlCommand cmd = new SqlCommand("insert into tbl_columbiapipe_product (productlink)"
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
        private static void UpdateStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_columbiapipe_search set iscompleted=@iscompleted where searchid=@id", con);
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
        private static void UpdatePageStatus(int ppage, int totallink)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_columbiapipe_search set Processing_Page=@ppage, TotalLink=@TotalLink where searchid=@id", con);
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
                            sqlCommand = new SqlCommand("select searchid,Searchurl,Processing_Page from tbl_columbiapipe_search where isnull(iscompleted,'')=0 order by searchid", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select productid,productlink from tbl_columbiapipe_product where ProductId  between " + start + " and " + end + " and  isnull(Category,'')='' ", sqlConnection);
                                //
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
                        cmd.CommandText = "update tbl_columbiapipe_product set Category=@Category,ProTitle=@ProTitle,mfr=@mfr,item=@item,upc=@upc,brand=@brand,MOQ=@MOQ,QtyInter=@QtyInter,LongDesc=@LongDesc,Price=@Price,UOM=@UOM,Feature=@Feature,Spec=@Spec,img_Link=@img_Link where productid=@sourceid";
                        cmd.Parameters.AddWithValue(@"Category", columbiapipe.Category != null ? ReplaceString.PutString(columbiapipe.Category) : "");
                        cmd.Parameters.AddWithValue(@"ProTitle", columbiapipe.ProTitle != null ? ReplaceString.PutString(columbiapipe.ProTitle) : "");
                        cmd.Parameters.AddWithValue(@"mfr", columbiapipe.mfr != null ? ReplaceString.PutString(columbiapipe.mfr) : "");
                        cmd.Parameters.AddWithValue(@"item", columbiapipe.item != null ? ReplaceString.PutString(columbiapipe.item) : "");
                        cmd.Parameters.AddWithValue(@"upc", columbiapipe.upc != null ? ReplaceString.PutString(columbiapipe.upc) : "");
                        cmd.Parameters.AddWithValue(@"brand", columbiapipe.brand != null ? ReplaceString.PutString(columbiapipe.brand) : "");
                        cmd.Parameters.AddWithValue(@"MOQ", columbiapipe.MOQ != null ? ReplaceString.PutString(columbiapipe.MOQ) : "");
                        cmd.Parameters.AddWithValue(@"QtyInter", columbiapipe.QtyInter != null ? ReplaceString.PutString(columbiapipe.QtyInter) : "");
                        cmd.Parameters.AddWithValue(@"LongDesc", columbiapipe.LongDesc != null ? ReplaceString.PutString(columbiapipe.LongDesc) : "");
                        cmd.Parameters.AddWithValue(@"Price", columbiapipe.Price != null ? ReplaceString.PutString(columbiapipe.Price) : "");
                        cmd.Parameters.AddWithValue(@"UOM", columbiapipe.UOM != null ? ReplaceString.PutString(columbiapipe.UOM) : "");
                        cmd.Parameters.AddWithValue(@"Feature", columbiapipe.Feature != null ? ReplaceString.PutString(columbiapipe.Feature) : "");
                        cmd.Parameters.AddWithValue(@"Spec", columbiapipe.Spec != null ? ReplaceString.PutString(columbiapipe.Spec) : "");
                        cmd.Parameters.AddWithValue(@"img_Link", columbiapipe.img_Link != null ? ReplaceString.PutString(columbiapipe.img_Link) : "");

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
    }
    public class columbiapipe
    {
        public string Category { get; set; }
        public string ProTitle { get; set; }
        public string mfr { get; set; }
        public string item { get; set; }
        public string upc { get; set; }
        public string brand { get; set; }
        public string MOQ { get; set; }
        public string QtyInter { get; set; }
        public string LongDesc { get; set; }
        public string Price { get; set; }
        public string UOM { get; set; }
        public string Feature { get; set; }
        public string Spec { get; set; }
        public string img_Link { get; set; }

    }
}
