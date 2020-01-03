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
        // public static columbiapipe columbiapipe;
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
                                DownloadString();
                                ProductExtract();
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
                catch { fishersci.Title = ""; }
                try
                {
                    fishersci.Short_desc = h1.DocumentNode.SelectSingleNode("//div[@class='subhead']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { fishersci.Short_desc = ""; }

                try
                {
                    fishersci.Feature = h1.DocumentNode.SelectSingleNode("//div[@id='tab1']").InnerText.ToString().Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { fishersci.Feature = ""; }
                try
                {
                    fishersci.MFR_Num = "#"+h1.DocumentNode.SelectSingleNode("//input[@name='partNum']").Attributes["value"].Value.ToString();
                }
                catch { fishersci.MFR_Num = ""; }

                try
                {
                    var manudata = h1.DocumentNode.SelectSingleNode("//p[@id='qa_mfr_comp_label']");
                    try
                    {
                        fishersci.Manu_Name = manudata.InnerText.Replace("Manufacturer:", "").ToString().Replace(fishersci.MFR_Num, "").Replace("\n", "").Replace("&nbsp;", "").Trim();
                    }
                    catch { }

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
                try
                {
                    fishersci.Price = h1.DocumentNode.SelectSingleNode("//span[@itemprop='price']").Attributes["content"].Value.ToString().Trim();
                }
                catch { fishersci.Price = ""; }
                try
                {
                    fishersci.UOM = h1.DocumentNode.SelectSingleNode("//span[@itemprop='unitText']").Attributes["content"].Value.ToString().Replace(" / ","").Replace("\n", "").Replace("&nbsp;", "").Trim();
                }
                catch { fishersci.UOM = ""; }
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
                            fishersci.Specfication += spec[0].InnerText.ToString().Trim() + " : " + spec[1].InnerText.ToString().Trim();
                        }
                        catch { }
                    }
                }
                catch { }
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
                            sqlCommand = new SqlCommand("select productid,productlink,IsLink from tbl_fishersci_product where productid  between " + start + " and " + end + " and  isnull(Category,'')=''", sqlConnection);
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
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36";
                request.Headers.Add("Sec-Fetch-User", @"?1");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                request.Headers.Add("Sec-Fetch-Site", @"none");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, @"_ga=GA1.2.157720089.1577142760; s_vi=[CS]v1|2F006214052A0A66-400001064000AF3C[CE]; s_ecid=MCMID%7C52977615209610504400932427448351245210; estore=estore-scientific; estoreCookie=1; new_quote=true; new_cart=true; prevSessionId.GUEST=i5V3CRIbChsPKxWwjkAxJSL; prevOrderId.GUEST=-1; prevMemberId.GUEST=817977639; WCXUID=39907396057515771427620; WCXUID=39907396057515771427620; PFM=unsorted; aam_uuid=47684198351854174410529488831137077299; LPVID=RiZmJlNjU4ZDE1MTNkOTVm; kampyle_userid=74de-829d-1916-d091-05f6-6c7d-7a6d-bbbc; cd_user_id=16f47cfcbaf547-0fd28b448f78f2-6701b35-100200-16f47cfcbb0878; accountId_AAM=Guest or No Account Chosen; _hjid=445bd668-f5e5-43e2-b6fe-50abfa86d666; DECLINED_DATE=1577471653674; _gid=GA1.2.2799477.1577713606; BIGipServerwww.fishersci.com_search_pool=1288357898.37151.0000; vcCookie=1; akacd_Prod_FS_AWS_Search=3755222228~rv=41~id=9130a186963d408235fa9b4da597f666; locale=en_US; _sdsat_landing_page=https://www.fishersci.com/us/en/search/chemical/substructure.html|1577800081527; _sdsat_session_count=4; BIGipServerwww.fishersci.com_commerce_pool=835373066.37407.0000; loginpopuphide=true; BIGipServerwww.fishersci.com_magellan_pool=1288357898.37919.0000; usertype=G; WC_SESSION_ESTABLISHED=true; WC_AUTHENTICATION_821067868=821067868%2coYtfCe%2fBWVM6paJffnsv3BbexEY%3d; WC_ACTIVEPOINTER=%2d1%2c10652; WC_USERACTIVITY_821067868=821067868%2c10652%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cnull%2cif6i5TVYj9jH%2bbC2sLz0H6SPkeh8RtA2CeZhTEB1DzYoKDcxSlyc2rkTl8JUbvj14A6hkR%2fLbBCr%0a5X7%2bDQoVWGj9%2fXer48Gl%2b2YqcS%2f0e6PyZ48BHlJ2c382hUD%2fiTP%2fcm1IVKlhN20pcxtzDOmQEg%3d%3d; akacd_Prod_FS_AWS_CQ=3755222232~rv=79~id=753e426048f72f733302a6c050ef979b; AMCVS_8FED67C25245B39C0A490D4C%40AdobeOrg=1; s_sess=%20s_cc%3Dtrue%3B; WCXSID=00005734587157780008367766666666; WCXSID_expiry=1577800083679; com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId=com.ibm.commerce.ubx.idsync.DSPID_ADOBE%2CaaUserId%2CmcId%2Cx1VisitorId; memberId_AAM=821067868; _hjIncludedInSample=1; pciChecked=Y; _sdsat_traffic_source=https://www.fishersci.com/us/en/products/I9C8L70H/ph-meters.html; kampylePageLoadedTimestamp=1577805760519; _sdsat_lt_pages_viewed=102; _sdsat_pages_viewed=63; adcloud={%22_les_v%22:%22y%2Cfishersci.com%2C1577809987%22}; dmdbase_cdc=DBSET; new_checkout=gm; s_days_since_new_s=Less than 1 day; s_days_since_new=1577808188444; WCS_JSESSIONID=0000LRdd-PA0YDsfQdFZ8WtDq-U:15lh8tdgl; s_pers=%20gpv_pn%3D%253Aus%253Aen%253Aproducts%253Ajb41athg%253Astem-career-education.html%7C1577809989383%3B; ak_bmsc=FEA15814F4CA415377724B79E3424F6D17CB3F354356000072EB0A5E8D6DEE2E~plFD8YJ2QJuwoaMPBAKpCAuW/472afYRBbMyui8BdZamR+QjSUIh+Jtpt0iaffjd+Uz/MFomH498Ikg1AWRvryQ40RrlvADndUH/lXwUJuvawIyeHJtQvFgE9toehujKicosx9ikdNHNAJZ/c8pGD29ZzS5j4G3C3+8hJueipaoLj8FkCt7I9pUml/LyKB9xFPF1za9iwonl7f+YpU0z+pM9IsJKUpjHmPFTnxBaOSDUUeA2VDTEC5JdCGP9/BUTO+; AMCV_8FED67C25245B39C0A490D4C%40AdobeOrg=-1303530583%7CMCIDTS%7C18261%7CMCMID%7C52977615209610504400932427448351245210%7CMCAAMLH-1578412989%7C12%7CMCAAMB-1578412989%7C6G1ynYcLPuiQxYZrsz_pkqfLG9yMXBpb2zX5dvJdYQJzPXImdj0y%7CMCOPTOUT-1577815389s%7CNONE%7CMCAID%7C2F006214052A0A66-400001064000AF3C%7CvVersion%7C3.3.0%7CMCCIDH%7C-1465750489; LPSID-30683608=hHJTezCLT5-vI0-DKUedlg; bm_sv=33765DEB92448BEF72D4B1D4D68279E5~FcQBGb9IYTOiGjN1k3gUt/vwm1rPb6dV9Bt9vAw4jT+RdmappdUT4FYqT2cIyd02ewl4vOEGCOanHlsQG6oZrj9d7qjWMLBsxNmnKSNBoGDnzyf6vAxJBUpO9YPHgkKj1xU6oIerCwn6KQKiuWSy3SvHPY5FjW5IbCnRR7LugEg=; kampyleUserSession=1577808211446; kampyleUserSessionsCount=72; kampyleSessionPageCounter=1; kampyleUserPercentile=71.19060899032313");
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
            try
            {
                h1 = null;
                h1 = new HtmlDocument();
                h1.LoadHtml(DownloadedString);
                foreach (var plink in h1.DocumentNode.SelectNodes("//a[@class='chemical_fmly_glyph']"))
                {
                    InsertProductLink(Homeurl + plink.Attributes["href"].Value.ToString());
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
