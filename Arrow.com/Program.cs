using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace Arrow.com
{
    class Program
    {
        private static string DownloadedString;
        private static readonly string connection = ConfigurationManager.AppSettings["connection"].ToString(),PLink= "https://www.arrow.com/productsearch/productlinesearchresultajax?page=XpageX&q=&prodline=XProductX&perPage=100";
        private static readonly string Name = ConfigurationManager.AppSettings["Name"].ToString();
        private static readonly int start = int.Parse(ConfigurationManager.AppSettings["Start"].ToString());
        private static readonly int end = int.Parse(ConfigurationManager.AppSettings["End"].ToString());
        private static int sourceid, ppage, TotalPage, num=0,ppcount=0,TotalCount=0,CookieCount=0;
        private static string SourceLink, modifiedlink, Looplink = string.Empty,DSourceLink=string.Empty, CookieString=string.Empty,searchquery=string.Empty;
        static HtmlDocument h1, h2, h3, h4, h5, h6;
        private static StringBuilder sb, breadcrum, spec;
        private static List<string> Cookie;
        private static string cookiestr = "";
        private static bool InitilCount = false;
        private static readonly string  Homeurl = "https://www.arrow.com";
        private static ArrowProduct ArrowProduct;
        static void Main(string[] args)
        {
            Console.Title = Name;
            while (true)
            {
                DataSet data = GetData(2);
                if (data != null)
                {
                    TotalCount = data.Tables[0].Rows.Count;
                    num = 1;
                    GetCookie();
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
                            SourceLink = row.ItemArray[1].ToString().Split('\t')[0].Trim();
                            sourceid = int.Parse(row.ItemArray[0].ToString());
                            //TotalPage = int.Parse(row.ItemArray[2].ToString());
                            //ppage = (int.Parse(row.ItemArray[3].ToString()) == 0) ? 1 : int.Parse(row.ItemArray[3].ToString());
                            //searchquery = row.ItemArray[4].ToString();
                            //if (string.IsNullOrEmpty(searchquery))
                            //{
                            //    if (!DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1].Contains("%20"))
                            //    {
                            //        SourceLink = PLink.Replace("XProductX", DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1].Replace("-", "%20")).Replace("XpageX", ppage.ToString());
                            //    }
                            //    else
                            //    {
                            //        SourceLink = PLink.Replace("XProductX", DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1]).Replace("XpageX", ppage.ToString());
                            //    }
                            //}
                            //else
                            //{
                            //    SourceLink = PLink.Replace("XProductX", searchquery).Replace("XpageX", ppage.ToString());
                            //}
                            //ArrowRequest();
                            DownloadString();
                            //ProcessArrowJson();
                            if (DownloadedString!=string.Empty)
                            ProcessArrowHTML();
                            num++;
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        private static void ProcessArrowJson()
        {
            try
            {
                Looplink:
                var Sourcejson = JObject.Parse(DownloadedString);
                var Sourcejson1 = JObject.Parse(Sourcejson["data"].ToString());
                var Sourcejson2 = JArray.Parse(Sourcejson1["results"].ToString());
                Console.WriteLine("Total Link found " + Sourcejson2.Count + " For Page " + ppage);
                ppcount += Sourcejson2.Count;
                for (int k = 0; k < Sourcejson2.Count; k++)
                {
                    InsertSearchLink(Homeurl + Sourcejson2[k]["partUrl"].ToString());
                }
                ppage +=1;
                SourceLink = string.Empty;
                if (string.IsNullOrEmpty(searchquery))
                {
                    if (!DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1].Contains("%20"))
                    {
                        SourceLink = PLink.Replace("XProductX", DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1].Replace("-", "%20")).Replace("XpageX", ppage.ToString());
                    }
                    else
                    {
                        SourceLink = PLink.Replace("XProductX", DSourceLink.Split('/')[DSourceLink.Split('/').Count() - 1]).Replace("XpageX", ppage.ToString());
                    }
                }
                else
                {

                    SourceLink = PLink.Replace("XProductX", searchquery).Replace("XpageX", ppage.ToString());
                }

                if (ppcount < TotalPage)
                {
                    UpdateSearchStatus(0);
                    Console.WriteLine("Processing Page " + ppage);
                    DownloadString();
                    goto Looplink;
                }
                UpdateSearchStatus(1);

            }
            catch
            {
            }
            finally
            {

            }
        }
        private static void UpdateSearchStatus(int completed)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update tbl_Arrow_searchLink set processingpage=@processingpage,iscompleted=@iscompleted where searchid=@id", con);
                    cmd.Parameters.AddWithValue("@iscompleted", completed);
                    cmd.Parameters.AddWithValue("@processingpage", ppage);
                    cmd.Parameters.AddWithValue("@id", sourceid);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while updating status");
            }
        }
        private static void InsertSearchLink(string Productlink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into tbl_Arrow_Product (ProductLink)"
                        + "values(@ProductLink)", con);
                    cmd.Parameters.AddWithValue("@ProductLink", Productlink);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("Error while inserting product");
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

        private static void ArrowRequest()
        {
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            try
            {
                DownloadedString = string.Empty;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                 request = (HttpWebRequest)WebRequest.Create(SourceLink);
                System.Net.HttpWebRequest.DefaultWebProxy = null;
                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Add("Sec-Fetch-Site", @"none");
                request.Headers.Add("Sec-Fetch-Mode", @"navigate");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Headers.Set(HttpRequestHeader.Cookie, CookieString);
                request.Headers.Set(HttpRequestHeader.IfNoneMatch, @"W/""631cce1d-deac-4c83-8f28-d131c171417d-58269""");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"kppid_managed=NDflz1UG; _micpn=esp:-1::1574146830624; _mibhv=anon-1574146830624-5732174659_7368; ASP.NET_SessionId=j120dplyjyr5zylrerfdrmk4; _br_uid_2=uid%3D9068389659008%3Av%3D13.0%3Ats%3D1574146825879%3Ahc%3D8; website#lang=en; utag_main=v_id:016e8276591d006f7aa1b09fa47800059003a0510086e$_sn:4$_ss:0$_st:1574225404718$ses_id:1574223580179%3Bexp-session$_pn:2%3Bexp-session; acceptedCookiePolicy1=true; _abck=DEE6AA76975C8164DA3587205C034231~0~YAAQHZwcuKS19X5uAQAAtVJ2ggIHp/py7FOkVvwlXZLrh/WL5MU1iZsKMtxvZdsh+IFNn6Ams9WCnJPc9F6FQZF9voaOW6OGd8SDjQ3m5ZOaAoGIfbJ1Mirpe/MlX3sD/pewyBx8TvfU3Uesmffcq5m7/3LfJx/erfvmbyyZvAb+5otjlxV3xJ4VPKT3DJqpK+9KhQ/DeJmpV+UeF8A88ubxdryquIeXeBPhjxCLjxCZP3oZNhHWZa6StKL9vKfc/OlW4HJ6kK+o0XUnsx++Jw4vYpOJ5BzJcPEFe0XKTnUozQdm5KUAO26e0/SnfbhkBKDrs6ZK~-1~-1~-1; _gcl_au=1.1.749256560.1574146825; arrowcurrency=isocode=USD&culture=en-US; _gid=GA1.2.1737512912.1574146826; _ga=GA1.2.1518514672.1574146826; _fbp=fb.1.1574146829603.1795488992; IsNewUser=False; ak_bmsc=12984539FEBA3C4BDA16F67EA7829EF9B81C9C1D1C020000D3BED45D39E8F238~plC4qbnMTmjSCGJqKeDXNqmJw77H7aWDmIFni2ea9K90ZIMQu8jo/7slajxsx9ElLNrMxKEWW1Zp89R+h6Q/L229RgbplcTmkV8ZmucIZKSaRCrb+tunCy/SjCG9f22p1ifHGUGbKtvJXGmupxfYL77Iy+xRcKapnTOUggx4kCy3DdO7R0ZEY7FAHplY/6zd1P+euNGB1r5+Ew0Gl5h0ExRBDfu5VTRAPrl9sJi+MywWtS6wjlxmUZPnIT0HEM73jw9LLJb3hr2ltm3fMVJ5bh3Fa3uBrCC0jAF1E+M/61EuWLgaDRC4spTElVG/nWF2JzIAzdHJeBirpuI+znvb6avQ==; bm_sz=00DBB7DC5D63A9FCE14D2EECA9D4BE58~YAAQHZwcuJXPsYRuAQAAdmoJhwVA9w6il8sQ0KoChX9xyj0nToUNZmP7kK0tVfinvZVA9T0gPwBlY7QUWOYGkQknL9SurmXDYPx/hpWZ1JT84Q0DyIJ+86kGCoaTs4T4Psr87TBNZwFUz+7BJXDS52kJtUriqHOEYpPlh2yOz6fhenKyxEnJB5il5ndZXe0=; AKA_A2=A");

                using (response = (HttpWebResponse)request.GetResponse())
                {
                    DownloadedString = ReadResponse(response);
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                {
                    ArrowProduct = new ArrowProduct();
                    Console.WriteLine("Downdstring failed..." + ex.ToString());
                    ArrowProduct.category = ex.ToString();
                    UpdateProductData(ArrowProduct);
                    ArrowProduct = null;
                    GetCookie();
                    CookieCount = 1;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                response.Close();
                response.Dispose();
                request.Abort();
            }
        }
        public static void DownloadString()
        {
            int retryLoop = 1;
            retry:
            try
            {
                
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
                if (CookieCount == 100)
                {
                    GetCookie();
                }
                CookieCount += 1;
                    DownloadedString = string.Empty;
                //ServicePointManager.UseNagleAlgorithm = false;
                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Ssl3;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SourceLink);

                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
                request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                request.Headers.Add("X-Postman-Interceptor-Id", @"05ed533c-11e8-0324-3e8b-0420a4c3a283");
                request.Headers.Add("Postman-Token", @"da51ad3c-1ebf-df26-a103-7a918c99fe57");
                request.Headers.Add("sec-fetch-user", @"?1");
                request.Headers.Add("Sec-Fetch-Site", @"cross-site");
                request.Headers.Add("Sec-Fetch-Mode", @"cors");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                request.Timeout = 5000;
                request.Headers.Set(HttpRequestHeader.Cookie, CookieString);
                //request.Headers.Set(HttpRequestHeader.Cookie, @"ASP.NET_SessionId=nxh2wlglhg0vuzjzophrlyew; arrowcurrency=isocode=USD&culture=en-US; website#lang=en; bm_sz=4E634B357439EF9A393CE9989A8DF093~YAAQmUgRYCgBaS5vAQAAF/svNgaamlwwhnWCmSnGpBmH430JJuHFb+LH4RJyJanwK8D3/YXY4OMVk4dybyo06Ar3M9blRnbYQ1DOMOKhzhsc3jJhRgyDTENTLipBL/5FP/D2X1Q1En8Q9RTscEjP/sNbMOOJHTJYOhaMMq/dvLKdqCF6Vy4WjU+AHLYXeLU=; _gcl_au=1.1.1794065317.1577192752; RedirectFromLogin=0; _abck=0E0C90C0FFE076DF345DC25FA80EABDD~0~YAAQmUgRYC4BaS5vAQAAJAAwNgM6DdLCI/rmnOLKLjdFgYkbu8G/yWKKgu85DftDu9W6i3G37ahH9YJNp/TgFjGP6dkJjnLle8RrO49wEB5u+DBkCTEO0x5V4KBOwh8t73eDZ8TAUQSwPr3BIwovKe9s7COHXp3rpvSm1dUHIkigguMLdWgp9kiTTWWES2GRPeJxwqwQyNE/bxL4HPDDlS1WYaFndJed0NHE+j+SoqjSRCGZRiarPmeaMAiV2PUYinVcN2XB4NE1kpaDi6PQKxOXXK3WyfAWnJ9Wr1ngpqRFpPYiFG1+kZ706qxt36VAcrDGR/zV~-1~-1~-1; _ga=GA1.2.221525952.1577192753; _gid=GA1.2.1553639270.1577192753; _dc_gtm_UA-29564268-22=1; ak_bmsc=B3A574E0AA62640F6116F5D390259A0F60114899F12700007F95015EA98C6D6D~pltYVYfNt45AoQBy6N7zEkEZw45kYBOgMG11IJNJleeAUtN0XZS6aMWtrlJuF/hEHXlt5O1mrYgypEnuKA3TnfxGyZ/zULMk1XecnSy2UoVlHSJ6JmEUXWZQY4HzYrGylqcSXCSfbbeEvOqD21QhhUEnK7Mv7dqe74gxjZjDpT+23irE9XtS+hW6KgVJfGnVtWbw4bDPSWGhdEeTXJ15uhW80d/Z5gOuAftwT4AYxzqZ1UbQs4VItzPDuKlOHIKpOZNBE0HsPAPnILfA9z/p0RoRstWsQ79x/8Fjb9YeiIfBhHRllZdzeD09lIoYWKyU2/hEBRw2Dbp2bSorwfoQE6og==; _gat_UA-29564268-22=1; IsNewUser=False; _br_uid_2=uid%3D9799714168150%3Av%3D13.0%3Ats%3D1577192758750%3Ahc%3D1; utag_main=v_id:016f38039df00017fd5923b4530e03072003a06a0086e$_sn:1$_ss:1$_st:1577194558770$ses_id:1577192758770%3Bexp-session$_pn:1%3Bexp-session; kppid_managed=NH7Q8MTi; RT=""sl=2&ss=1577192751563&tt=2304&obo=1&bcn=%2F%2F684fc53f.akstat.io%2F&sh=1577192759228%3D2%3A1%3A2304%2C1577192755328%3D1%3A1%3A0&dm=arrow.com&si=3c92c004-5fbc-44b0-9771-933a6e04e33a&r=https%3A%2F%2Fwww.arrow.com%2Fen%2Fproducts%2Fbps130-ha030p-1mg%2Fbourns&ul=1577192767558""");

                //Console.WriteLine("response");
                DownloadedString = ReadResponse((HttpWebResponse)request.GetResponse());
                //Console.WriteLine(respons.ToString());

                //DownloadedString = streamReader.ReadToEnd();
                
            }
            catch(WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                {
                    ArrowProduct = new ArrowProduct();
                    Console.WriteLine("Downdstring failed..." + ex.ToString());
                    ArrowProduct.category = ex.ToString();
                    UpdateProductData(ArrowProduct);
                    ArrowProduct = null;
                    GetCookie();
                    CookieCount = 1;
                }
                else
                {
                    retryLoop += 1;
                    Console.WriteLine("Loop Retry "+ retryLoop);
                   //goto retry;
                }
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
                            sqlCommand = new SqlCommand("select searchid,SearchLink,TotalLink,processingpage,Searchquery from tbl_Arrow_searchLink where Iscompleted=0 and searchid between " + start + " and " + end + " ", sqlConnection);
                            break;
                        case 2:
                            sqlCommand = new SqlCommand("select Productid,ProductLink from tbl_Arrow_Product where isnull(category,'')='' and isnull(itemtitle,'')='' and Productid  between " + start + " and " + end + " ", sqlConnection);
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
        private static void ProcessArrowHTML()
        {
            ArrowProduct = new ArrowProduct();
            breadcrum = new StringBuilder();
            try
            {
                h1 = new HtmlDocument();
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

                    try {
                        ArrowProduct.imageurl =  h1.DocumentNode.SelectNodes("//a[@class='Product-Summary-ImageCarousel-slide slick-slide slick-current slick-active is-active ng-star-inserted']").Select(s=> "https:"+s.Attributes["data-image"].Value).Aggregate((a,b)=>  a+" | "+b).ToString();
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
                            h2 = new HtmlDocument();
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
                            h2 = new HtmlDocument();
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
            }
        }

        private static void GetCookie()
        {
            try
            {
                CookieString = string.Empty;
                Console.WriteLine("Fetching Cookie......");
                using (SqlConnection con = new SqlConnection(connection))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("select top 1 cookie from tbl_Arrow_cookie where id=1 ", con);
                    //cmd.CommandTimeout = 0;
                    CookieString = cmd.ExecuteScalar().ToString();
                    //SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    // sda.Fill(ds);
                }
            }
            catch(Exception ex)
            {
            }
        }

        public static void UpdateProductData(ArrowProduct ArrowProduct)
        {
            try
            {
                Console.WriteLine("Update product data !!");
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("update [dbo].[tbl_Arrow_Product] set itemtitle=@itemtitle,Productdescription=@Productdescription,MPN=@MPN,Manufacturename=@Manufacturename,productcategory=@productcategory,UOM=@UOM,price=@price,imageurl=@imageurl,category=@category,techspec=@techspec ,ModifiedDate=@ModifiedDate where Productid=@id", sqlConnection);
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
                    sqlCommand.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateing product data failed!!");
            }
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
