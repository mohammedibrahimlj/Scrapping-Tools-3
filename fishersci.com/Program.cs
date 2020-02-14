using HtmlAgilityPack;
using HTMLCodeReplacer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fishersci.com
{
    class Program
    {
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), navigate = "?Nrpp=1000&No=xpgnox";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0, IsLink=0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, CookieString = string.Empty, searchquery = string.Empty, Scode = string.Empty, DownloadedString = string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static bool initial = false;
        private static readonly string Homeurl = "https://www.fishersci.com";
        private static string filePath = "columbiapipe.txt";
        public static fishersci fishersci;
        public static HttpWebResponse response;
        public static string responseText;
        // public static columbiapipe columbiapipe;
        static void Main(string[] args)
        {
            // SearchPageExtract();
            SourceLink = "https://www.fishersci.com/shop/products/glassy-carbon-splinter-powder-0-4-12-m-type-1-alfa-aesar/AA3800109#";

            if (Request_www_fishersci_com(out response))
            {
                //responseText = ReadResponse(response);

                response.Close();
            }
            if (!string.IsNullOrEmpty(DownloadedString))
            {
                ProductExtract();
            }
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
                        foreach (DataRow row in data.Tables[0].Rows)
                        {
                            try
                            {
                                IsLink = 0;
                                TotalPage = 0;
                                initial = true;
                                ppage = 1;
                                CookieCount = 0;
                                ppcount = 0;
                                SourceLink = string.Empty;
                                searchquery = string.Empty;
                                Console.WriteLine("Processing link " + num + " of " + TotalCount);
                                sourceid = 0;
                                SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                                IsLink = int.Parse(row.ItemArray[2].ToString());
                                //ppage = int.Parse(row.ItemArray[2].ToString()) != 0 ? (int.Parse(row.ItemArray[2].ToString()) + 1) : 1;
                                //SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim() + navigate.Replace("xpgnox", ppage.ToString());

                                sourceid = int.Parse(row.ItemArray[0].ToString());
                                // DownloadString();
                               

                                if (Request_www_fishersci_com(out response))
                                {
                                    //responseText = ReadResponse(response);

                                    response.Close();
                                }
                                if (!string.IsNullOrEmpty(DownloadedString))
                                {
                                    ProductExtract();
                                }
                                //GetSearchLinkInitial();
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
        private static void ProductExtract()
        {
            fishersci = new fishersci();
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    var breadcrum = h1.DocumentNode.SelectSingleNode("//div[@class='breadcrumbs']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(breadcrum);
                    foreach (var li in h2.DocumentNode.SelectNodes("//li"))
                    {
                        fishersci.category += li.InnerText.Trim() + " | ";
                    }
                }
                catch { }

                try
                {
                    fishersci.Title = h1.DocumentNode.SelectSingleNode("//h1[@id='item_header_text']").InnerText.ToString().Replace("\n","").Replace("&nbsp;", "").Trim();
                }
                catch {
                    try
                    {
                        fishersci.Title = h1.DocumentNode.SelectSingleNode("//h1[@id='qa_item_header_text']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                    }
                    catch
                    {
                        try
                        {
                            fishersci.Title = h1.DocumentNode.SelectSingleNode("//h1[@id='qa_product_description']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                        }
                        catch { fishersci.Title = ""; }
                        
                    }
                }
                try
                {
                    fishersci.Short_desc = h1.DocumentNode.SelectSingleNode("//div[@class='subhead']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch {
                    try
                    {
                        fishersci.Short_desc = h1.DocumentNode.SelectSingleNode("//p[@id='product_intro_para']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                    }
                    catch
                    {
                        fishersci.Short_desc = "";
                    }
                }

                try
                {
                    fishersci.Feature = h1.DocumentNode.SelectSingleNode("//div[@id='tab1']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { fishersci.Feature = ""; }
                try
                {
                    fishersci.MFR_Num = "#"+h1.DocumentNode.SelectSingleNode("//input[@name='partNum']").Attributes["value"].Value.ToString();
                }
                catch {
                    try
                    {
                        fishersci.MFR_Num = "#" + h1.DocumentNode.SelectSingleNode("//span[@id='qa_manufacturer_number_label']").InnerText.ToString().Trim();
                    }
                    catch
                    {
                        fishersci.MFR_Num = "";
                    }
                }

                try
                {
                    var manudata = h1.DocumentNode.SelectSingleNode("//p[@id='qa_mfr_comp_label']");
                    try
                    {
                        fishersci.Manu_Name = manudata.InnerText.Replace("Manufacturer:", "").ToString().Replace(fishersci.MFR_Num, "").Replace("\n", "").Replace("&nbsp;", "").Trim();
                    }
                    catch { try
                        {
                            fishersci.Manu_Name= h1.DocumentNode.SelectSingleNode("//strong[@id='qa_manufacturer_name_label']").InnerText.Replace("Mfr:", "").Trim();
                        }
                        catch { fishersci.Manu_Name = ""; } }

                    try
                    {
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(manudata.InnerHtml.ToString());
                        fishersci.SDP = h2.DocumentNode.SelectSingleNode("//img").Attributes["title"].Value.ToString();
                    }
                    catch { fishersci.SDP = ""; }
                }
                catch {  }
                try
                {
                    fishersci.Categlog = "#"+h1.DocumentNode.SelectSingleNode("//span[@id='qa_prod_code_labl']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { fishersci.Categlog = ""; }

                //qa_prod_code_labl
                try
                {
                    fishersci.Price = h1.DocumentNode.SelectSingleNode("//span[@itemprop='price']").Attributes["content"].Value.ToString().Trim();
                }
                catch {
                    try
                    {
                        fishersci.Price = h1.DocumentNode.SelectSingleNode("//p[@id='qa_discount_price_txt']").InnerText.ToString().Trim();
                        //qa_discount_price_txt
                    }
                    catch
                    {
                        try
                        {
                            fishersci.Price = h1.DocumentNode.SelectSingleNode("//span[@id='qa_single_price']").InnerText.ToString().Trim();
                            //qa_single_price
                        }
                        catch { fishersci.Price = ""; }
                    }
                }
                try
                {

                    fishersci.UOM = h1.DocumentNode.SelectSingleNode("//span[@itemprop='unitText']").Attributes["content"].Value.ToString().Replace(" / ","").Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { try
                    {
                        fishersci.UOM = h1.DocumentNode.SelectSingleNode("//span[@id='qa_single_display_unit']").InnerText.ToString().Trim();
                    }
                    catch { fishersci.UOM = ""; }
                }
                try
                {
                    fishersci.Price = fishersci.Price.Replace(fishersci.UOM, "").Trim();
                }
                catch { }
                try
                {
                    var specdata = h1.DocumentNode.SelectSingleNode("//div[@id='tab2']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(specdata);
                    foreach (var tr in h2.DocumentNode.SelectNodes("//tr"))
                    {
                        try
                        {
                            h3 = null;
                            h3 = new HtmlDocument();
                            h3.LoadHtml(tr.InnerHtml.ToString());
                            var spec = h3.DocumentNode.SelectNodes("//td");
                            fishersci.Specfication += spec[0].InnerText.ToString().Trim() + " : " + spec[1].InnerText.ToString().Trim() +" | ";
                        }
                        catch { }
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(fishersci.Specfication))
                {
                    try
                    {
                        var specdata = h1.DocumentNode.SelectNodes("//table[@class='specs_data']");
                        foreach (var table in specdata)
                        {
                            h2 = null;
                            h2 = new HtmlDocument();
                            h2.LoadHtml(table.InnerHtml.ToString());
                            foreach (var tr in h2.DocumentNode.SelectNodes("//tr"))
                            {
                                try
                                {
                                    h3 = null;
                                    h3 = new HtmlDocument();
                                    h3.LoadHtml(tr.InnerHtml.ToString());
                                    var spec = h3.DocumentNode.SelectNodes("//td");
                                    fishersci.Specfication += spec[0].InnerText.ToString().Trim() + " : " + spec[1].InnerText.ToString().Trim() + " | ";
                                }
                                catch { }
                            }
                        }

                    }
                    catch { }
                }
                try
                {
                    try
                    {
                        var imgdata = h1.DocumentNode.SelectSingleNode("//ul[@class='csPager cSGallery']").InnerHtml.ToString();
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(imgdata);
                        foreach (var imglink in h2.DocumentNode.SelectNodes("//img"))
                        {
                            fishersci.Image_Link += imglink.Attributes["src"].Value.ToString() + " | ";
                        }
                    }
                    catch { }
                    try
                    {
                        fishersci.Image_Link += h1.DocumentNode.SelectSingleNode("//img[@id='productImage']").Attributes["src"].Value.ToString();
                    }
                    catch { }
                }
                catch { fishersci.Image_Link = ""; }
            }
            catch {
                Console.WriteLine("Error While Processing the HTML data....");
            }
            finally {
                UpdateProductData();
                if (IsLink == 0)
                {
                    ProductLinkCheck();
                }
                
                fishersci = null;
                h1 = null;
                h2 = null;
                h3 = null;
            }
        }

        public static void UpdateProductData()
        {
            try
            {
                Console.WriteLine("Update product data !!");
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("update [dbo].[tbl_fishersci_product] set Image_Link=@Image_Link,Specfication=@Specfication,UOM=@UOM,Price=@Price,SDP=@SDP,Categlog=@Categlog,MFR_Num=@MFR_Num,Manu_Name=@Manu_Name,Feature=@Feature,Short_desc=@Short_desc,Title=@Title,category=@category,IsLink=@IsLink where Productid =@Productid", sqlConnection);
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Parameters.AddWithValue("@category", (fishersci.category == null) ? "" : ReplaceString.PutString(fishersci.category));
                    sqlCommand.Parameters.AddWithValue("@Title", (fishersci.Title == null) ? "" : ReplaceString.PutString(fishersci.Title));
                    sqlCommand.Parameters.AddWithValue("@Short_desc", (fishersci.Short_desc == null) ? "" : ReplaceString.PutString(fishersci.Short_desc));
                    sqlCommand.Parameters.AddWithValue("@Feature", (fishersci.Feature == null) ? "" : ReplaceString.PutString(fishersci.Feature));
                    sqlCommand.Parameters.AddWithValue("@Manu_Name", (fishersci.Manu_Name == null) ? "" : ReplaceString.PutString(fishersci.Manu_Name));
                    sqlCommand.Parameters.AddWithValue("@MFR_Num", (fishersci.MFR_Num == null) ? "" : ReplaceString.PutString(fishersci.MFR_Num));
                    sqlCommand.Parameters.AddWithValue("@Categlog", (fishersci.Categlog == null) ? "" : ReplaceString.PutString(fishersci.Categlog));
                    sqlCommand.Parameters.AddWithValue("@SDP", (fishersci.SDP == null) ? "" : ReplaceString.PutString(fishersci.SDP));
                    sqlCommand.Parameters.AddWithValue("@Price", (fishersci.Price == null) ? "" : ReplaceString.PutString(fishersci.Price));
                    sqlCommand.Parameters.AddWithValue("@UOM", (fishersci.UOM == null) ? "" : ReplaceString.PutString(fishersci.UOM));
                    sqlCommand.Parameters.AddWithValue("@Specfication", (fishersci.Specfication == null) ? "" : ReplaceString.PutString(fishersci.Specfication));
                    sqlCommand.Parameters.AddWithValue("@Image_Link", (fishersci.Image_Link == null) ? "" : ReplaceString.PutString(fishersci.Image_Link));
                    sqlCommand.Parameters.AddWithValue("@IsLink", 1);
                    sqlCommand.Parameters.AddWithValue("@Productid", sourceid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateing product data failed!!");
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
                            sqlCommand = new SqlCommand("select searchid,Searchurl,Processing_Page from tbl_fishersci_search where isnull(iscompleted,'')=2 order by searchid", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select productid,productlink,IsLink from tbl_fishersci_product where productid  between " + start + " and " + end + " and  isnull(Specfication,'')=''", sqlConnection);
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
                //request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                //request.Headers.Add("Sec-Fetch-Site", @"same-origin");
                //request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                request.Referer = SourceLink;
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"_ga=GA1.2.157720089.1577142760; s_vi=[CS]v1|2F006214052A0A66-400001064000AF3C[CE]; s_ecid=MCMID%7C52977615209610504400932427448351245210; estore=estore-scientific; estoreCookie=1; new_quote=true; new_cart=true; prevSessionId.GUEST=i5V3CRIbChsPKxWwjkAxJSL; prevOrderId.GUEST=-1; prevMemberId.GUEST=817977639; WCXUID=39907396057515771427620; WCXUID=39907396057515771427620; PFM=unsorted; aam_uuid=47684198351854174410529488831137077299; LPVID=RiZmJlNjU4ZDE1MTNkOTVm; kampyle_userid=74de-829d-1916-d091-05f6-6c7d-7a6d-bbbc; cd_user_id=16f47cfcbaf547-0fd28b448f78f2-6701b35-100200-16f47cfcbb0878; accountId_AAM=Guest or No Account Chosen; _hjid=445bd668-f5e5-43e2-b6fe-50abfa86d666; DECLINED_DATE=1577471653674; kampylePageLoadedTimestamp=1577982267215; akacd_Prod_FS_AWS_CQ=3756609963~rv=41~id=c5c98ff0bbcc2ef5d7d9db05e31e3c51; locale=en_US; _sdsat_landing_page=https://www.fishersci.com/us/en/products/JB41ATHG/stem-career-education.html|1579191436903; _sdsat_session_count=11; AMCVS_8FED67C25245B39C0A490D4C%40AdobeOrg=1; _gid=GA1.2.962356089.1579191437; loginpopuphide=true; WCS_JSESSIONID=0000XE5A6xALPTxzf7fsoVkKbX3:177font5m; usertype=G; WC_SESSION_ESTABLISHED=true; WC_AUTHENTICATION_827773841=827773841%2cs8kKgv0LFyhO0Q9N8eDgUiAnD%2bo%3d; WC_ACTIVEPOINTER=%2d1%2c10652; WC_USERACTIVITY_827773841=827773841%2c10652%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2crHgFEeF1j1X2oTB%2bIdZ6zmeUlMZY3sBYMUYtRYR9qCtYviQA7VYopWT8Pcp0sMMQreDQrYxO6Zxc%0artyXYSq%2b8ih3QKje4C8w56zLr9X%2fOWuTOxPtjB1d%2fleD4a8W6iFLjPj5QQdFz5MKDbP843QZpQ%3d%3d; BIGipServerwww.fishersci.com_commerce_pool=818595850.38175.0000; vcCookie=1; s_days_since_new_s=More than 7 days; BIGipServerwww.fishersci.com_magellan_pool=1271580682.37919.0000; ak_bmsc=5AC266D3C4C292E457A42433F3969B6417CB3F35296C0000AD06205EA1BD702F~plHaU9iNW8Cm90wLyzGZImptKxKihPa7V0KlPg+QCyrCo4cNFKQDE/1zYA2O1lYf35OiiOnOpMbLCRmN53ehBUzrrVn0ErhKPbetXrCFw0Mu8RGmuD4JFiA90pOynZ3V9+u8ctTsqWXOC0ScavAf4gAhmqT3sPPccL0NIUfHaMDbOQgf3lMF92EwwVJFsAzFseMS+m9hWZtWVXpTLNOKgHr69otLbeHAAZ3MLkkVRBPcO7iUYeo5gryWbkN2hx/Swi; _sdsat_traffic_source=https://www.fishersci.com/us/en/products/JB41ATHG/stem-career-education.html; pciChecked=Y; WCXSID=00004162833157919144844466666666; WCXSID_expiry=1579191448446; com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId=com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId; memberId_AAM=827773841; LPSID-30683608=RvEiKb8BSjy9VzovyvZKow; AMCV_8FED67C25245B39C0A490D4C%40AdobeOrg=-1303530583%7CMCIDTS%7C18278%7CMCMID%7C52977615209610504400932427448351245210%7CMCAAMLH-1579796303%7C12%7CMCAAMB-1579796303%7C6G1ynYcLPuiQxYZrsz_pkqfLG9yMXBpb2zX5dvJdYQJzPXImdj0y%7CMCOPTOUT-1579198637s%7CNONE%7CMCAID%7C2F006214052A0A66-400001064000AF3C%7CvVersion%7C3.3.0%7CMCCIDH%7C696854824; akacd_Prod_FS_AWS_Search=3756610063~rv=61~id=e0b731bb80c37c1568e748c7e92df1be; _hjIncludedInSample=1; dmdbase_cdc=DBSET; kampyleUserSessionsCount=102; kampyleUserSession=1579194809763; kampyleSessionPageCounter=1; kampyleUserPercentile=88.49969865773566; BIGipServerwww.fishersci.com_search_pool=1288357898.37151.0000; _sdsat_lt_pages_viewed=156; _sdsat_pages_viewed=20; adcloud={%22_les_v%22:%22y%2Cfishersci.com%2C1579196962%22}; _gat=1; s_days_since_new=1579195163584; new_checkout=gm; s_pers=%20gpv_pn%3D%253Aus%253Aen%253Asearch%253Achemical%253Asubstructure.html%7C1579196965186%3B; s_sess=%20s_sq%3D%3B%20s_cc%3Dtrue%3B; bm_sv=ACD1FD86EFFFB062BF211660E4C4347E~2kLpmYM021M5lE0o1h/D5jyUj80GA7V8MrAKOvJ7GScJm0vEjDMOERqbxV+3XoBBkbNBoxW31qu2b8xIG2rlviytWC5mxbb7pvCrGYRd0IrPS4U8SvLSxWQHzINCscSQfKC8QeaNNV3qAcjS4haub8VJzqpFcPVkarpTXkY5xgM=");
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

        private static string ReadResponse(HttpWebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {
                Stream streamToRead = responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                {
                    streamToRead = new GZipStream(streamToRead, CompressionMode.Decompress);
                }
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                {
                    streamToRead = new DeflateStream(streamToRead, CompressionMode.Decompress);
                }

                using (StreamReader streamReader = new StreamReader(streamToRead, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private static bool Request_www_fishersci_com(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink);

                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                request.Headers.Add("Sec-Fetch-Site", @"none");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"BIGipServerwww.fishersci.com_commerce_pool=818595850.38175.0000; locale=en_US; _sdsat_landing_page=https://www.fishersci.com/shop/products/glassy-carbon-splinter-powder-0-4-12-m-type-1-alfa-aesar/aa3800109|1580980637612; _sdsat_session_count=1; _sdsat_traffic_source=; AMCVS_8FED67C25245B39C0A490D4C%40AdobeOrg=1; _ga=GA1.2.1279803548.1580980638; _gid=GA1.2.543276975.1580980638; loginpopuphide=true; estore=estore-scientific; WCS_JSESSIONID=0000MLDJEhFYIomSTc7dtB3KdAl:177font5m; usertype=G; WC_SESSION_ESTABLISHED=true; WC_AUTHENTICATION_838280260=838280260%2cHTlQMhn5bblQABSbFQudQFI8wok%3d; WC_ACTIVEPOINTER=%2d1%2c10652; WC_USERACTIVITY_838280260=838280260%2c10652%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cdlaaEKIWbL0wVw0KJJU%2btO7%2f7Jj8QDwZNP847z%2bDJ3mDJ57NwWkSSKKT8ZudzxPvJg6c0XxR7qll%0aZCjzssHogRHjqbW4S5nDU6nKvu49%2by2kPgGS86ep6uYPOuHwoIxhYsl9lHYIJ7c3SKei%2b2gMzA%3d%3d; estoreCookie=1; dmdbase_cdc=DBSET; new_quote=true; new_cart=true; vcCookie=1; prevMemberId.GUEST=838280260; prevSessionId.GUEST=MLDJEhFYIomSTc7dtB3KdAl; prevOrderId.GUEST=-1; ak_bmsc=893D60063D0E38BB77E0C4507BBF1234170BD7347564000087D93B5E0FAE970B~pl+KRHd+pE7oIqUlgYSVVdwjR3gw0LgjwfIrZqscCYL6eUgwzlSD2v/lBogerJXoRxWPwRIx2coNm4lAyLRmJ6UbfM25F5CXEv46h1nf9dVeJpMe0ljD2vBBj83qM3oNYWpxNZHH7XfyjsPXnlgYbIYmz6cBri6O1iR5YN79fM3XbvIw9gUGH0B0peHlA4xxhDUK2TKBR50akyIfB+y+mJGVt+ZsyTznD1IwTn+7p6LlNmfGJDUxfQyPDSKDYjT4dc; PFM=unsorted; aam_uuid=84911056668235718014374266718728538385; s_days_since_new_s=First Visit; BIGipServerwww.fishersci.com_magellan_pool=1271580682.37919.0000; memberId_AAM=838280260; _fbp=fb.1.1580980684228.2060918183; _hjid=445bd668-f5e5-43e2-b6fe-50abfa86d666; accountId_AAM=Guest or No Account Chosen; _sdsat_lt_pages_viewed=2; _sdsat_pages_viewed=2; adcloud={%22_les_v%22:%22y%2Cfishersci.com%2C1580983152%22}; AMCV_8FED67C25245B39C0A490D4C%40AdobeOrg=-1303530583%7CMCIDTS%7C18299%7CMCMID%7C91152295968255393123971399698649600696%7CMCAAMLH-1581586152%7C12%7CMCAAMB-1581586152%7CRKhpRz8krg2tLO6pguXWp5olkAcUniQYPHaMWWgdJ3xzPWQmdj0y%7CMCOPTOUT-1580987837s%7CNONE%7CMCAID%7CNONE%7CvVersion%7C3.3.0%7CMCCIDH%7C1408361480; new_checkout=gm; s_sess=%20s_sq%3D%3B%20s_cc%3Dtrue%3B; pciChecked=Y; akacd_Prod_FS_AWS_CQ=3758434131~rv=90~id=1d4afadbd270420de7966449c8d8897d; WCXUID=39820000289715809813549; WCXUID=39820000289715809813549; WCXSID=00006599937158098135491066666666; WCXSID_expiry=1580981354911; s_pers=%20gpv_pn%3D%253Ashop%253Aproducts%253Aglassy-carbon-splinter-powder-0-4-12-m-type-1-alfa-aesar%253Aaa3800109%7C1580983154966%3B; com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId=com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId; s_days_since_new=1580981355657; bm_sv=A80F39C76FFF3AA472E3622F4F1A8FAC~yORSbyop2UXDR9jQeNvKNx5BTNgQBgMHLA54V5xYvTpAlfOuR1vgROZU1R4KSHCaW2mgLIs19Y+NYuH8UPEW66PaebNZEjUfz9HdxWVNiaqJIYzp6fagPLvhzoN17opDa258rUDtRSHMhS4UhCtk7IHAl4myg9iutIiq0zgoJxw=; kampyleUserSession=1580981385819; kampyleUserSessionsCount=124; kampyleSessionPageCounter=1; kampyleUserPercentile=83.93529198440639; cd_user_id=17019d57ac2558-0e77a6770e29db-b383f66-100200-17019d57ac374f; LPVID=k2Y2QwYTg3MDJiMzM0ODVl; LPSID-30683608=RBT8ns0NRGeyUK89kCR47g");

                response = (HttpWebResponse)request.GetResponse();
                DownloadedString=ReadResponse(response).ToString();
            }
            catch (WebException e)
            {
                Console.WriteLine("404 Exception!!!" +e.Status.ToString());
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (response != null) response.Close();
                return false;
            }

            return true;
        }
        public static void GetSearchLinkInitial()
        {
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    //left_nav_links
                    var ullink = h1.DocumentNode.SelectSingleNode("//ul[@class='left_nav_links']").InnerHtml.ToString();
                    h2 = null;
                    h2 = new HtmlDocument();
                    h2.LoadHtml(ullink);
                    foreach (var links in h2.DocumentNode.SelectNodes("//a"))
                    {
                        try
                        {
                            InsertProductLink(Homeurl + links.Attributes["href"].Value.ToString());
                        }
                        catch {}
                    }
                }
                catch
                {
                    try
                    {
                        var ullink = h1.DocumentNode.SelectSingleNode("//ul[@class='left_nav_links show_scroll_short js-brand-list']").InnerHtml.ToString();
                        h2 = null;
                        h2 = new HtmlDocument();
                        h2.LoadHtml(ullink);
                        foreach (var links in h2.DocumentNode.SelectNodes("//a"))
                        {
                            try
                            {
                                InsertProductLink(Homeurl + links.Attributes["href"].Value.ToString());
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                UpdateStatus(3);
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
                    SqlCommand cmd = new SqlCommand("insert into tbl_fishersci_product (productlink,IsLink)"
                        + "values(@Productlink,@IsLink)", con);
                    cmd.Parameters.AddWithValue("@Productlink", Productlink.Trim());
                    cmd.Parameters.AddWithValue("@IsLink", 1);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while inserting search");
            }
        }

        private static void ProductLinkCheck()
        {
            //try
            //{
            //    h1 = null;
            //    h1 = new HtmlDocument();
            //    h1.LoadHtml(DownloadedString);
            //    foreach (var plink in h1.DocumentNode.SelectNodes("//a[@class='chemical_fmly_glyph']"))
            //    {
            //        InsertProductLink(Homeurl + plink.Attributes["href"].Value.ToString());
            //    }
            //}
            //catch { }
        }
        private static void UpdateStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_fishersci_search set iscompleted=@iscompleted where searchid=@id", con);
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
    }
    public class fishersci
    {
        public string category { get; set; }
        public string Title { get; set; }
        public string Short_desc { get; set; }
        public string Feature { get; set; }
        public string Manu_Name { get; set; }
        public string MFR_Num { get; set; }
        public string Categlog { get; set; }
        public string SDP { get; set; }
        public string Price { get; set; }
        public string UOM { get; set; }
        public string Specfication { get; set; }
        public string Image_Link { get; set; }

    }
}
