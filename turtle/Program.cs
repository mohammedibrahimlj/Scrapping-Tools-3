using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
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
using HTMLCodeReplacer;

namespace turtle
{
    class Program
    {
        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), P;
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, DSourceLink = string.Empty, CookieString = string.Empty, searchquery = string.Empty, Scode=string.Empty,searchpage= "https://www.turtle.com/itemLevelFilterPage.action?pageClick=Y&codeId=XCodeX&levelNo=$!levelNoSkip&gallery=0&srchTyp=&sortBy=mpn_asc&resultPage=48&keyWordTxt=&pageNo=";
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static StringBuilder sb, breadcrum, spec;
        private static List<string> Cookie;
        private static string cookiestr = "";
        private static bool InitilCount = false;
        private static readonly string Homeurl = "https://www.turtle.com/",pdflink= "https://cdn-assets.unilogcorp.com";
        public static Turtle Turtle;
        static void Main(string[] args)
        {
            ProductLinkExtract();
        }
        private static void SearchPageExtract()
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
                        //GetCookie();
                        foreach (DataRow row in data.Tables[0].Rows)
                        {
                            try
                            {
                                CookieCount = 0;
                                ppcount = 0;
                                SourceLink = string.Empty;
                                DSourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                SourceLink = "https://www.turtle.com/4173979/Product/abb-10105";
                                //SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                //TotalPage = int.Parse(row.ItemArray[2] == null ? "0" : row.ItemArray[2].ToString());
                                //ppage = (int.Parse(row.ItemArray[3].ToString()) == 0) ? 1 : int.Parse(row.ItemArray[3].ToString());
                                DownloadString();
                                //GetSearhPageLink();
                                GetProductData();
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
        private static void UpdateProductData()
        {
            try
            {
                Console.WriteLine("Update product data !!");
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("update tbl_turtle_ProductLink set Category=@Category, ProductTitle = @ProductTitle, ProductDescription = @ProductDescription, Manufacturers = @Manufacturers, BrandName = @BrandName, Part = @Part, MNP = @MNP, UPC = @UPC, Description = @Description, Specification = @Specification, Document = @Document, Features = @Features, UOM = @UOM, Price = @Price where ProductId = @ProductId", sqlConnection);
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Parameters.AddWithValue(@"Category", (Turtle.Category == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Category)));
                    sqlCommand.Parameters.AddWithValue(@"ProductTitle", (Turtle.ProductTitle == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.ProductTitle)));
                    sqlCommand.Parameters.AddWithValue(@"ProductDescription", (Turtle.ProductDescription == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.ProductDescription)));
                    sqlCommand.Parameters.AddWithValue(@"Manufacturers", (Turtle.Manufacturers == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Manufacturers)));
                    sqlCommand.Parameters.AddWithValue(@"BrandName", (Turtle.BrandName == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.BrandName)));
                    sqlCommand.Parameters.AddWithValue(@"Part", (Turtle.Part == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Part)));
                    sqlCommand.Parameters.AddWithValue(@"MNP", (Turtle.MNP == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.MNP)));
                    sqlCommand.Parameters.AddWithValue(@"UPC", (Turtle.UPC == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.UPC)));
                    sqlCommand.Parameters.AddWithValue(@"Description", (Turtle.Description == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Description)));
                    sqlCommand.Parameters.AddWithValue(@"Specification", (Turtle.Specification == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Specification)));
                    sqlCommand.Parameters.AddWithValue(@"Document", (Turtle.Document == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Document)));
                    sqlCommand.Parameters.AddWithValue(@"Features", (Turtle.Features == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Features)));
                    sqlCommand.Parameters.AddWithValue(@"UOM", (Turtle.UOM == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.UOM)));
                    sqlCommand.Parameters.AddWithValue(@"Price", (Turtle.Price == null) ? "" : Stringreplace(ReplaceString.PutString(Turtle.Price)));
                    sqlCommand.Parameters.AddWithValue("@ProductId", sourceid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch(Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }
        private static void GetProductData()
        {
            try
            {
                var data=ReplaceString.PutString("");
                Turtle = new Turtle();
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var category = h1.DocumentNode.SelectSingleNode("//ul[@class='breadcrumb']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(category);
                    Turtle.Category = h2.DocumentNode.SelectNodes("//li").Select(s => s.InnerText.ToString().Replace("/n","").Trim()).Aggregate((a, b) => a + "> " + b).ToString();
                }
                catch { Turtle.Category = string.Empty; }
                try
                {
                    Turtle.ProductTitle = h1.DocumentNode.SelectSingleNode("//h2[@class='cimm_prodDetailTitle']").InnerText.ToString().Trim();
                }
                catch { Turtle.ProductTitle = string.Empty; }
                try
                {
                    Turtle.ProductDescription= h1.DocumentNode.SelectSingleNode("//p[@class='cimm_itemShortDesc']").InnerText.ToString().Trim();
                }
                catch { Turtle.ProductDescription = string.Empty; }
                try
                {
                    //productDetailList
                    var tabledata = h1.DocumentNode.SelectSingleNode("//table[@id='productDetailList']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(tabledata);
                    try
                    {
                        Turtle.Manufacturers = h2.DocumentNode.SelectNodes("//tr").Where(w => w.InnerText.Contains("Manufacturers")).Select(s => s.InnerText.ToString().Replace("Manufacturers", "").Replace("/n", "").Trim()).FirstOrDefault();
                    }
                    catch { Turtle.Manufacturers = string.Empty; }
                    try
                    {
                        Turtle.BrandName = h2.DocumentNode.SelectNodes("//tr").Where(w => w.InnerText.Contains("Brand Name")).Select(s => s.InnerText.ToString().Replace("Brand Name", "").Replace("\n","").Trim()).FirstOrDefault();
                    }
                    catch { Turtle.BrandName = string.Empty; }
                    try
                    {
                        Turtle.UPC = h2.DocumentNode.SelectNodes("//tr").Where(w => w.InnerText.Contains("UPC")).Select(s => s.InnerText.ToString().Replace("UPC", "").Replace("/n", "").Trim()).FirstOrDefault();
                    }
                    catch { Turtle.UPC = string.Empty; }
                    try
                    {
                        Turtle.MNP = h2.DocumentNode.SelectNodes("//tr").Where(w => w.InnerText.Contains("MPN")).Select(s => s.InnerText.ToString().Replace("MPN", "").Replace("/n", "").Trim()).FirstOrDefault();
                    }
                    catch { Turtle.MNP = string.Empty; }

                    try
                    {
                        Turtle.Part = h2.DocumentNode.SelectNodes("//tr").Where(w => w.InnerText.Contains("Part #")).Select(s => s.InnerText.ToString().Replace("Part #:", "").Replace("/n", "").Trim()).FirstOrDefault();
                    }
                    catch { Turtle.Part = string.Empty; }
                }
                catch { }
                try
                {
                    var specdata = h1.DocumentNode.SelectSingleNode("//div[@id='specificationSection']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(specdata);
                    Turtle.Specification = Stringreplace(h2.DocumentNode.SelectNodes("//tr").Select(s => s.InnerText.ToString().Replace("&nbsp;","").Replace("\n","").Trim()).Aggregate((a, b) => a + " | " + b).ToString());
                }
                catch { Turtle.Specification = string.Empty; }

                try
                {
                    Turtle.Description= h1.DocumentNode.SelectSingleNode("//div[@id='descriptionSection']").InnerText.ToString().Trim();
                }
                catch { Turtle.Description = ""; }
                try
                {
                    Turtle.Document = h1.DocumentNode.SelectNodes("//span[@class='cimm_pdfLink hideMe']").Select(s=>s.InnerText.ToString().Trim()).Aggregate((a,b)=>a+" | "+b).ToString();
                }
                catch { Turtle.Document = ""; }
                try
                {
                    Turtle.Features = h1.DocumentNode.SelectSingleNode("//div[@id='featureSection']").InnerText.ToString().Trim();
                }
                catch { Turtle.Features = ""; }
                GetPriceString();
            }
            catch { }
            finally
            {
                UpdateProductData();
                Turtle = null;
                h1 = null;
                h2 = null;
            }
        }
        public static string Stringreplace(string inputstring)
        {
            string result = string.Empty;
            try
            {
                result = inputstring;
                inputstring = inputstring.Replace("\t", " ");
                inputstring = inputstring.Replace("\n", " ");
                string pattern = "\\s+";
                string replacement = " ";
                Regex rx = new Regex(pattern);
                result = rx.Replace(inputstring, replacement);
                return result;
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        private static void ProductLinkExtract()
        {
            try
            {
                Console.Title = Name;
                while (true)
                {
                    DataSet data = GetData(2);
                    if (data != null && data.Tables[0].Rows.Count > 0)
                    {
                        TotalCount = data.Tables[0].Rows.Count;
                        num = 1;
                        foreach (DataRow row in data.Tables[0].Rows)
                        {
                            try
                            {
                                ppcount = 0;
                                SourceLink = string.Empty;
                                Scode = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                #region PageNavigation
                                //TotalPage = int.Parse(row.ItemArray[2] == "" ? "0" : row.ItemArray[2].ToString());
                                //GetCategorycode();
                                //if (Scode == string.Empty)
                                //{
                                //    Scode = new Uri(SourceLink).Segments[1].Replace("/", "").ToString();
                                //}
                                //ppage = (int.Parse(row.ItemArray[3].ToString()) == 0) ? 0 : int.Parse(row.ItemArray[3].ToString());
                                //SourceLink = searchpage.Replace("XCodeX", Scode) + ppage;
                                #endregion
                                DownloadString();
                                GetProductData();
                                num++;
                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                Console.ReadKey();

            }
            catch { }
        }
        private static void GetPriceString()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.turtle.com/getPriceDetailPage.action");
                request.KeepAlive = true;
                request.Headers.Add("Origin", @"https://www.turtle.com");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.87 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                request.Headers.Add("X-Postman-Interceptor-Id", @"207bec76-d035-547d-e85a-4d238e22f351");
                request.Headers.Add("Postman-Token", @"b97a16d6-7a1c-0fc0-9009-d1cd71398a2a");
                request.Headers.Add("Sec-Fetch-Site", @"cross-site");
                request.Headers.Add("Sec-Fetch-Mode", @"cors");
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"_ga=GA1.2.707436210.1572332714; _hjid=01fb6f51-5d91-4d65-8cb4-87bc2fdc9be2; _gid=GA1.2.905018769.1572904025; JSESSIONID=D853174673AB01E6914B741DDA13BC15; _hjIncludedInSample=1; _trackatronId=1w93f37h1; _gat_UA-59170845-1=1");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;
                string body = @"LABAvailability=Y&productIdList="+Turtle.Part.Replace(" ","+")+ "%3A%3A1".ToString();
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                DownloadedString = streamReader.ReadToEnd();

                try
                {
                    var Jsondata = JArray.Parse(DownloadedString);
                    Turtle.Price = "$"+Jsondata[0]["cimm2BCentralPricingWarehouse"]["costPrice"].ToString();
                    Turtle.UOM = Jsondata[0]["cimm2BCentralPricingWarehouse"]["uom"].ToString();
                }
                catch { }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static void GetCategorycode()
        {
            
            try
            {
                DownloadString();
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                Scode = h1.DocumentNode.SelectSingleNode("//input[@name='codeId']").Attributes["value"].Value.ToString().Trim();
                //Scode = int.Parse(code.ToString());
            }
            catch
            {
            }
            finally
            {
                h1 = null;
            }
        }
        private static void ProcessProductURL()
        {
            try
            {
                //codeId
                Looplink:
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                var productli = h1.DocumentNode.SelectNodes("//li[@class='sessionImg']");
                ppage += productli.Count();

                if (TotalPage == 0)
                {
                    try
                    {
                        TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//div[@class='searchResults']").InnerText.ToString().Replace("Results Found ", "").Replace("Items(s)", ""));
                        UpdateSearchStatus(0);
                    }
                    catch { try { TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//h3[@class='cimm_pageTitle noMargin']").InnerText.ToString().Replace("Results Found ", "").Replace("Items(s)", "")); } catch { TotalPage = 0; } }
                }
                Console.WriteLine("Total page found " + ppage);
                foreach (var li in productli)
                {
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(li.InnerHtml.ToString());
                    var ProductLink = Homeurl + h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString();
                    InsertProductLink(ProductLink);
                }

                if (ppage != TotalPage)
                {
                    UpdateSearchStatus(0);
                    DownloadedString = string.Empty;
                    SourceLink = searchpage.Replace("XCodeX", Scode) + ppage;
                    DownloadString();
                    goto Looplink;
                }
                else
                {
                    UpdateSearchStatus(1);
                }

            }
            catch
            {
            }
        }
        private static void GetSearhPageLink()
        {
            try
            {
                try
                {
                    h1 = null;
                    h1 = new HtmlDocument();
                    h1.LoadHtml(DownloadedString);
                    var searchdiv = h1.DocumentNode.SelectNodes("//div[@class='cimm_categoryItemBlock']");
                    Console.WriteLine("Total Page found " + searchdiv.Count());
                    foreach (var div in searchdiv)
                    {
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(div.InnerHtml.ToString());
                        var SearchProductLink = Homeurl + h2.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.ToString();
                        InsertSearchLink(SearchProductLink);
                    }
                    UpdateSearchStatus(1);
                }
                catch { }
                try
                {
                    TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//div[@class='searchResults']").InnerText.ToString().Replace("Results Found ", "").Replace("Items(s)", ""));
                    UpdateSearchStatus(0);
                }
                catch { try { TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//h3[@class='cimm_pageTitle noMargin']").InnerText.ToString().Replace("Results Found ", "").Replace("Items(s)", "")); } catch { TotalPage = 0; } }
                //
            }
            catch(Exception ex)
            {
            }
            finally
            {
                h1 = null;
                h2 = null;
            }
        }
        private static void InsertSearchLink(string SearchLink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into tbl_turtle_searchLink (SearchLink)"
                        + "values(@SearchLink)", con);
                    cmd.Parameters.AddWithValue("@SearchLink", SearchLink);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while inserting search");
            }
        }
        private static void InsertProductLink(string Productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into tbl_turtle_Product (Productlink)"
                        + "values(@Productlink)", con);
                    cmd.Parameters.AddWithValue("@Productlink", Productlink);
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
        private static void UpdateSearchStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_industrialairpower_SearchLink set Processing_Page=@processingpage,iscompleted=@iscompleted,TotalLink=@TotalLink where id=@id", con);
                    cmd.Parameters.AddWithValue("@iscompleted", completed);
                    cmd.Parameters.AddWithValue("@processingpage", ppage);
                    cmd.Parameters.AddWithValue("@TotalLink", TotalPage);
                    cmd.Parameters.AddWithValue("@id", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while updating status");
            }
        }
        public static void DownloadString()
        {
            try
            {
                //if (CookieCount == 100)
                //{
                //    ///GetCookie();
                //}
                CookieCount += 1;
                DownloadedString = string.Empty;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink);
                request.KeepAlive = true;
                request.Accept = "application/json, text/plain, */*";
                //request.Headers.Add("X-NewRelic-ID", @"VgADVVFRGwIBU1laDwgAXw==");
                //request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.120 Safari/537.36";
                //request.Headers.Add("Sec-Fetch-Mode", @"cors");
                //request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                //request.Headers.Set(HttpRequestHeader.Cookie, CookieString);

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
                            sqlCommand = new SqlCommand("select searchid,SearchLink,TotalLink,processingpage from tbl_turtle_searchLink where Iscompleted=0 and totallink!=0 and searchid between " + start + " and " + end + " order by searchid", sqlConnection);
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
    public class Turtle
    {
        public string Category { get; set; }
        public string ProductTitle { get; set; }
        public string ProductDescription { get; set; }
        public string Manufacturers { get; set; }
        public string BrandName { get; set; }
        public string Part { get; set; }
        public string MNP { get; set; }
        public string UPC { get; set; }
        public string Description { get; set; }
        public string Specification { get; set; }
        public string Document { get; set; }
        public string Features { get; set; }
        public string UOM { get; set; }
        public string Price { get; set; }
    }
}
//create table tbl_turtle_ProductLink(ProductId int identity, ProductURL varchar(500),Category varchar(750),ProductTitle varchar(500),
//ProductDescription varchar(max),Manufacturers varchar(150),BrandName varchar(150),Part varchar(150),MNP varchar(150),UPC varchar(150),
//Description varchar(max),Specification varchar(max),Document varchar(750),Features varchar(750),UOM varchar(15),Price varchar(15))
//create clustered index tbl_turtle_ProductLink_cl on tbl_turtle_ProductLink(ProductId asc)