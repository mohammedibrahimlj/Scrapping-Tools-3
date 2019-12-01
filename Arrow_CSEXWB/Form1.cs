using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTMLCodeReplacer;

namespace CSEXWBrowser
{
    public partial class Form1 : Form
    {

        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(), PLink = "https://www.arrow.com/productsearch/productlinesearchresultajax?page=XpageX&q=&prodline=XProductX&perPage=100";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num = 0, ppcount = 0, TotalCount = 0, CookieCount = 0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty, DSourceLink = string.Empty, CookieString = string.Empty, searchquery = string.Empty;
        private bool isprocess = false;
        static HtmlAgilityPack.HtmlDocument h1, h2, h3, h4, h5, h6;
        private static StringBuilder sb, breadcrum, spec;
        private static List<string> Cookie;
        private static string cookiestr = "";
        private static bool InitilCount = false;
        private static readonly string Homeurl = "https://www.arrow.com";
        private static ArrowProduct ArrowProduct;
        private csExWB.cEXWB fbrowser = null;
        private DataSet data;

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isprocess)
            {
                try
                {
                    isprocess = false;
                    DownloadedString = fbrowser.DocumentSource.ToString();
                    
                    if (DownloadedString != string.Empty)
                        ProcessArrowHTML();
                }
                catch {
                    timer.Stop();
                    num++;
                    DownloadedString = string.Empty;
                    ProcessArrowLinkAsync();
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            ArrowProcess();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Form1.Name = Name;
            num = 0;
            fbrowser = cEXWB1;
            cEXWB1.RegisterAsBrowser = true;
            //panel1.Controls.Add(fbrowser);
            fbrowser.Navigate("www.google.com");
            Task.Delay(5000);
            //ArrowProcess();
            FastenalProcess();
        }

        private void ArrowProcess()
        {
            data =GetDataAsync(2).GetAwaiter().GetResult();
            TotalCount = data.Tables[0].Rows.Count;
            ProcessArrowLinkAsync();

        }

        public async Task ProcessArrowLinkAsync()
        {
            try {


                //Console.Title = Name;
                while (num < TotalCount)
                {
                    
                    //foreach (DataRow row in data.Tables[0].Rows)
                    //{
                        try
                        {
                        //cEXWB1 = new csExWB.cEXWB();
                        //fbrowser = cEXWB1;
                        isprocess = true;
                            ppcount = 0;
                            SourceLink = string.Empty;
                            LabelLog("Processing link " + (num + 1 )+ " of " + TotalCount);
                            sourceid = 0;
                            SourceLink = data.Tables[0].Rows[num].ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(data.Tables[0].Rows[num].ItemArray[0].ToString());
                        fbrowser.Navigate(SourceLink);
                        await Task.Delay(7000);
                        //ProcessArrowHTML();
                        FastenalSearchProLinkPageAsync();
                        //timer.Start();
                    }
                        catch
                        {
                        }
                    //}
                }
            }
            catch { }
        }
        public async Task<DataSet> GetDataAsync(int option = 1)
        {
            SqlCommand sqlCommand = new SqlCommand();
            DataSet dataSet = new DataSet();

            LabelLog("Fetching data from database................");
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    switch (option)
                    {
                        case 1:
                            sqlCommand = new SqlCommand("select searchid,SearchLink,TotalLink,processingpage,Searchquery from tbl_Arrow_searchLink where Iscompleted=0 and searchid between " + start + " and " + end + " ", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select Productid,ProductLink from tbl_Arrow_Product where isnull(itemtitle,'')='' and Productid  between " + start + " and " + end + " ", sqlConnection);
                            break;
                        case 3:
                            sqlCommand = new SqlCommand("select searchid,SearchTitle from tbl_fastenal_SearchLink where  iscompleted=0 ", sqlConnection);
                            break;
                    }
                    sqlCommand.CommandTimeout = 6000;
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                    sqlDataAdapter.Fill(dataSet);
                }
            }
            catch (Exception ex)
            {

                LabelLog("Error While loading search data!!! \n" + ex.ToString());
            }
            return dataSet;
        }
        private void ProcessArrowHTML()
        {
            //timer.Stop();
            ArrowProduct = new ArrowProduct();
            breadcrum = new StringBuilder();
            try
            {
                DownloadedString = fbrowser.DocumentSource.ToString();
                h1 = new HtmlAgilityPack.HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    ArrowProduct.itemtitle = h1.DocumentNode.SelectSingleNode("//i[@class='Product-Summary-SubHeading-ProductLine']").InnerText.ToString().Replace("\n","").Trim();
                }
                catch
                {
                    ArrowProduct.itemtitle = "";
                }
                try
                {
                    ArrowProduct.Productdescription = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[2].InnerText.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.Productdescription = "";
                }
                try
                {
                    ArrowProduct.Manufacturename = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[0].InnerText.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.Manufacturename = "";
                }
                try
                {
                    ArrowProduct.MPN = h1.DocumentNode.SelectSingleNode("//span[@class='product-summary-name--Original']").InnerText.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                }
                try
                {
                    ArrowProduct.productcategory = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[1].InnerText.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.productcategory = "";
                }
                try
                {
                    ArrowProduct.UOM = h1.DocumentNode.SelectSingleNode("//span[@class='BuyingOptions-caption BuyingOptions-total-priceFor']").InnerText.ToString().Trim().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.UOM = "";
                }
                try
                {
                    ArrowProduct.price = h1.DocumentNode.SelectSingleNode("//span[@class='BuyingOptions-total-price ng-star-inserted']").InnerText.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.price = "";
                }
                try
                {
                    ArrowProduct.imageurl = "https:" + h1.DocumentNode.SelectSingleNode("//img[@class='Product-Summary-Image']").Attributes["src"].Value.ToString().Replace("\n", "").Trim();
                }
                catch
                {

                    try
                    {
                        ArrowProduct.imageurl = h1.DocumentNode.SelectNodes("//a[@class='Product-Summary-ImageCarousel-slide slick-slide slick-current slick-active is-active ng-star-inserted']").Select(s => "https:" + s.Attributes["data-image"].Value).Aggregate((a, b) => a + " | " + b).ToString().Replace("\n", "").Trim();
                    }
                    catch
                    {
                        try
                        {
                            ArrowProduct.imageurl = "https:" + h1.DocumentNode.SelectSingleNode("//img[@class='PDPImageModal-image ng-star-inserted']").Attributes["src"].Value.ToString();
                        }
                        catch
                        {
                            ArrowProduct.imageurl = "";
                        }
                    }
                }
                try
                {
                    HtmlNodeCollection htmlNodeCollection = h1.DocumentNode.SelectNodes("//li[@class='Breadcrumb-item ng-star-inserted']");
                    foreach (HtmlNode item in (IEnumerable<HtmlNode>)htmlNodeCollection)
                    {
                        breadcrum.Append(item.InnerText.ToString().Trim() + " | ");
                    }
                    ArrowProduct.category = breadcrum.ToString().Replace("\n", "").Trim();
                }
                catch
                {
                    ArrowProduct.category = "";
                }
                try
                {
                    spec = new StringBuilder();
                    try
                    {
                        HtmlNodeCollection htmlNodeCollection2 = h1.DocumentNode.SelectNodes("//tr[@class='row ng-star-inserted']");
                        foreach (HtmlNode item2 in (IEnumerable<HtmlNode>)htmlNodeCollection2)
                        {
                            h2 = new HtmlAgilityPack.HtmlDocument();
                            h2.LoadHtml(item2.InnerHtml.ToString());
                            HtmlNodeCollection htmlNodeCollection3 = h2.DocumentNode.SelectNodes("//td");
                            try
                            {
                                spec.Append(htmlNodeCollection3[0].InnerText.ToString().Trim() + " : " + htmlNodeCollection3[1].InnerText.ToString().Trim() + " | ");
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch { }
                    try
                    {
                        HtmlNodeCollection htmlNodeCollection4 = h1.DocumentNode.SelectNodes("//tr[@class='row SimpleAccordion-toggle ng-star-inserted']");
                        foreach (HtmlNode item2 in (IEnumerable<HtmlNode>)htmlNodeCollection4)
                        {
                            h2 = new HtmlAgilityPack.HtmlDocument();
                            h2.LoadHtml(item2.InnerHtml.ToString());
                            HtmlNodeCollection htmlNodeCollection5 = h2.DocumentNode.SelectNodes("//td");
                            try
                            {
                                spec.Append(htmlNodeCollection5[0].InnerText.ToString().Trim() + " : " + htmlNodeCollection5[1].InnerText.ToString().Trim() + " | ");
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch { }
                    ArrowProduct.techspec = spec.ToString().Replace("\n", "").Trim();

                }
                catch
                {
                    ArrowProduct.techspec = "";
                }

            }
            catch (Exception)
            {
            }
            finally
            {
                if (string.IsNullOrEmpty(DownloadedString))
                {
                    ArrowProduct.itemtitle = "404 Error";
                    UpdateProductData(ArrowProduct);
                }
                else
                {
                    UpdateProductData(ArrowProduct);
                }
                breadcrum = null;
                ArrowProduct = null;
                h2 = null;
                h1 = null;
                spec = null;
                num++;
                DownloadedString = string.Empty;
                //ProcessArrowLinkAsync();
            }
        }
        public void UpdateProductData(ArrowProduct ArrowProduct)
        {
            try
            {
                LabelLog("Update product data !!");
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("update [dbo].[tbl_Arrow_Product] set itemtitle=@itemtitle,Productdescription=@Productdescription,MPN=@MPN,Manufacturename=@Manufacturename,productcategory=@productcategory,UOM=@UOM,price=@price,imageurl=@imageurl,category=@category,techspec=@techspec where Productid=@id", sqlConnection);
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Parameters.AddWithValue("@itemtitle", (ArrowProduct.itemtitle == null) ? "" :ReplaceString.PutString(ArrowProduct.itemtitle));
                    sqlCommand.Parameters.AddWithValue("@Productdescription", (ArrowProduct.Productdescription == null) ? "" : ReplaceString.PutString(ArrowProduct.Productdescription));
                    sqlCommand.Parameters.AddWithValue("@MPN", (ArrowProduct.MPN == null) ? "" : ReplaceString.PutString(ArrowProduct.MPN));
                    sqlCommand.Parameters.AddWithValue("@Manufacturename", (ArrowProduct.Manufacturename == null) ? "" : ReplaceString.PutString(ArrowProduct.Manufacturename));
                    sqlCommand.Parameters.AddWithValue("@productcategory", (ArrowProduct.productcategory == null) ? "" : ReplaceString.PutString(ArrowProduct.productcategory));
                    sqlCommand.Parameters.AddWithValue("@UOM", (ArrowProduct.UOM == null) ? "" : ReplaceString.PutString(ArrowProduct.UOM));
                    sqlCommand.Parameters.AddWithValue("@price", (ArrowProduct.price == null) ? "" : ReplaceString.PutString(ArrowProduct.price));
                    sqlCommand.Parameters.AddWithValue("@imageurl", (ArrowProduct.imageurl == null) ? "" : ReplaceString.PutString(ArrowProduct.imageurl));
                    sqlCommand.Parameters.AddWithValue("@category", (ArrowProduct.category == null) ? "" : ReplaceString.PutString(ArrowProduct.category));
                    sqlCommand.Parameters.AddWithValue("@techspec", (ArrowProduct.techspec == null) ? "" : ReplaceString.PutString(ArrowProduct.techspec));
                    //sqlCommand.Parameters.AddWithValue("@ProductURL", SourceLink);
                    sqlCommand.Parameters.AddWithValue("@id", sourceid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LabelLog("updateing product data failed!!");
            }
        }
        private void LabelLog(string Logmsg)
        {
            Log.Invoke((MethodInvoker)(() => Log.Text = Logmsg.ToString()));

        }

        #region Fastenal
        private void FastenalProcess()
        {
            button1.Enabled = false;
            data = GetDataAsync(3).GetAwaiter().GetResult();
            TotalCount = data.Tables[0].Rows.Count;
            ProcessArrowLinkAsync();


        }

        private async Task FastenalSearchProLinkPageAsync()
        {
            ppage = 0;
        PageLoop:
            try
            {
                DownloadedString = fbrowser.DocumentSource.ToString();
                h1 = null;
                h1 = new HtmlAgilityPack.HtmlDocument();
                h1.LoadHtml(DownloadedString);
                TotalPage = int.Parse(h1.DocumentNode.SelectSingleNode("//span[@class='pagination-text']").InnerText.ToString().Split(' ')[0].ToString());

                try
                {
                    var productitem = h1.DocumentNode.SelectNodes("//div[@class='media-item-row']");
                    foreach (var protag in productitem)
                    {
                        h2 = null;
                        h2 = new HtmlAgilityPack.HtmlDocument();
                        h2.LoadHtml(protag.InnerHtml.ToString());
                        var productlinks = h2.DocumentNode.SelectNodes("//a");

                        foreach (var product in productlinks)
                        {
                            ppage += 1;
                            FastenalInsertProductLink(Homeurl + product.Attributes["href"].Value.ToString());
                        }
                    }
                }
                catch { }
                if (ppage < TotalPage)
                {
                    ppcount += 1;
                    FastenalUpdateSearchLinkStatus(0);
                    //DownloadHTMLString();
                    fbrowser.Navigate(SourceLink);
                    await Task.Delay(7000);
                    DownloadedString = string.Empty;
                    goto PageLoop;
                }
                else
                {
                    FastenalUpdateSearchLinkStatus(1);
                }

            }
            catch { }
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
        #endregion
    }
    public class ArrowProduct
    {
        public string itemtitle
        {
            get;
            set;
        }

        public string Productdescription
        {
            get;
            set;
        }

        public string MPN
        {
            get;
            set;
        }

        public string Manufacturename
        {
            get;
            set;
        }

        public string productcategory
        {
            get;
            set;
        }

        public string UOM
        {
            get;
            set;
        }

        public string price
        {
            get;
            set;
        }

        public string imageurl
        {
            get;
            set;
        }

        public string category
        {
            get;
            set;
        }

        public string techspec
        {
            get;
            set;
        }
    }
}
