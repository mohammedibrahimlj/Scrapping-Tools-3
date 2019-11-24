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

namespace Arrow_CSEXWB
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
        static HtmlAgilityPack.HtmlDocument h1, h2, h3, h4, h5, h6;

        private void Button1_Click(object sender, EventArgs e)
        {
            ArrowProcess();
        }

        private static StringBuilder sb, breadcrum, spec;
        private static List<string> Cookie;
        private static string cookiestr = "";
        private static bool InitilCount = false;
        private static readonly string Homeurl = "https://www.arrow.com";
        private static ArrowProduct ArrowProduct;
        private csExWB.cEXWB fbrowser=null;
        private DataSet data;
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
        }

        private void ArrowProcess()
        {
            data = GetData(2);
            TotalCount = data.Tables[0].Rows.Count;
            ProcessArrowLink();

        }

        public void ProcessArrowLink()
        {
            try {


                Console.Title = Name;
                if (num < TotalCount)
                {
                    
                    //foreach (DataRow row in data.Tables[0].Rows)
                    //{
                        try
                        {
                            ppcount = 0;
                            SourceLink = string.Empty;
                            
                            LabelLog("Processing link " + num + 1 + " of " + TotalCount);
                            sourceid = 0;
                            SourceLink = data.Tables[0].Rows[num].ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(data.Tables[0].Rows[num].ItemArray[0].ToString());
                        fbrowser.Navigate(SourceLink);
                        Task.Delay(5000);
                        DownloadedString = fbrowser.DocumentSource.ToString();
                            if (DownloadedString != string.Empty)
                                ProcessArrowHTML();
                            
                        }
                        catch
                        {
                        }
                    //}
                }
            }
            catch { }
        }
        public DataSet GetData(int option = 1)
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
                            sqlCommand = new SqlCommand("select Productid,ProductLink from tbl_Arrow_Product where isnull(category,'')='' and Productid  between " + start + " and " + end + " ", sqlConnection);
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
            ArrowProduct = new ArrowProduct();
            breadcrum = new StringBuilder();
            try
            {
                h1 = new HtmlAgilityPack.HtmlDocument();
                h1.LoadHtml(DownloadedString);
                try
                {
                    ArrowProduct.itemtitle = h1.DocumentNode.SelectSingleNode("//i[@class='Product-Summary-SubHeading-ProductLine']").InnerText.ToString();
                }
                catch
                {
                    ArrowProduct.itemtitle = "";
                }
                try
                {
                    ArrowProduct.Productdescription = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[2].InnerText.ToString();
                }
                catch
                {
                    ArrowProduct.Productdescription = "";
                }
                try
                {
                    ArrowProduct.Manufacturename = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[0].InnerText.ToString();
                }
                catch
                {
                    ArrowProduct.Manufacturename = "";
                }
                try
                {
                    ArrowProduct.MPN = h1.DocumentNode.SelectSingleNode("//span[@class='product-summary-name--Original']").InnerText.ToString();
                }
                catch
                {
                }
                try
                {
                    ArrowProduct.productcategory = h1.DocumentNode.SelectNodes("//p[@class='Product-Summary-Details']")[1].InnerText.ToString();
                }
                catch
                {
                    ArrowProduct.productcategory = "";
                }
                try
                {
                    ArrowProduct.UOM = h1.DocumentNode.SelectSingleNode("//span[@class='BuyingOptions-caption BuyingOptions-total-priceFor']").InnerText.ToString().Trim();
                }
                catch
                {
                    ArrowProduct.UOM = "";
                }
                try
                {
                    ArrowProduct.price = h1.DocumentNode.SelectSingleNode("//span[@class='BuyingOptions-total-price ng-star-inserted']").InnerText.ToString();
                }
                catch
                {
                    ArrowProduct.price = "";
                }
                try
                {
                    ArrowProduct.imageurl = "https:" + h1.DocumentNode.SelectSingleNode("//img[@class='Product-Summary-Image']").Attributes["src"].Value.ToString();
                }
                catch
                {

                    try
                    {
                        ArrowProduct.imageurl = h1.DocumentNode.SelectNodes("//a[@class='Product-Summary-ImageCarousel-slide slick-slide slick-current slick-active is-active ng-star-inserted']").Select(s => "https:" + s.Attributes["data-image"].Value).Aggregate((a, b) => a + " | " + b).ToString();
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
                    ArrowProduct.category = breadcrum.ToString();
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
                    ArrowProduct.techspec = spec.ToString();

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
                UpdateProductData(ArrowProduct);
                breadcrum = null;
                ArrowProduct = null;
                h2 = null;
                h1 = null;
                spec = null;
                num++;
                DownloadedString = string.Empty;
                ProcessArrowLink();
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
                    sqlCommand.Parameters.AddWithValue("@itemtitle", (ArrowProduct.itemtitle == null) ? "" : ArrowProduct.itemtitle);
                    sqlCommand.Parameters.AddWithValue("@Productdescription", (ArrowProduct.Productdescription == null) ? "" : ArrowProduct.Productdescription);
                    sqlCommand.Parameters.AddWithValue("@MPN", (ArrowProduct.MPN == null) ? "" : ArrowProduct.MPN);
                    sqlCommand.Parameters.AddWithValue("@Manufacturename", (ArrowProduct.Manufacturename == null) ? "" : ArrowProduct.Manufacturename);
                    sqlCommand.Parameters.AddWithValue("@productcategory", (ArrowProduct.productcategory == null) ? "" : ArrowProduct.productcategory);
                    sqlCommand.Parameters.AddWithValue("@UOM", (ArrowProduct.UOM == null) ? "" : ArrowProduct.UOM);
                    sqlCommand.Parameters.AddWithValue("@price", (ArrowProduct.price == null) ? "" : ArrowProduct.price);
                    sqlCommand.Parameters.AddWithValue("@imageurl", (ArrowProduct.imageurl == null) ? "" : ArrowProduct.imageurl);
                    sqlCommand.Parameters.AddWithValue("@category", (ArrowProduct.category == null) ? "" : ArrowProduct.category);
                    sqlCommand.Parameters.AddWithValue("@techspec", (ArrowProduct.techspec == null) ? "" : ArrowProduct.techspec);
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
