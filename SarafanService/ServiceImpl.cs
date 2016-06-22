using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Threading;
using System.Runtime.CompilerServices;

using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

using System.Runtime.InteropServices;

using Nancy;
using Nancy.Hosting.Aspnet;
using Nancy.ModelBinding;
using Nancy.Json;
using Nancy.Routing;
using Nancy.Bootstrapper;
using Nancy.IO;
using Nancy.TinyIoc;


using Npgsql;
using NpgsqlTypes;

using OpenCvSharp;

namespace SarafanServiceForWidget
    {
    public partial class ServiceModule
        {



        //+///////////////////////////////////////////////////////////////////////
        private bool Connect(out NpgsqlConnection conn, ref StructResult result)
            {
            conn = null;

            string strConnString = ServUtility.ReadStringFromConfig("connection_string_2");

            if (strConnString.Length == 0)
                {
                result.message = "Config file corrupted";
                result.result_code = ResultCode.Failure_InternalServiceError;

                return false;
                }

            try
                {
                conn = new NpgsqlConnection(strConnString);
                }
            catch (Exception e)
                {
                result.message = e.Message;
                result.result_code = ResultCode.Failure_InternalServiceError;

                return false;
                }

            if (conn == null)
                {
                result.message = "NpgsqlConnection object is null";
                result.result_code = ResultCode.Failure_InternalServiceError;

                return false;
                }

            try
                {
                conn.Open();
                }
            catch (Exception e)
                {
                result.message = e.Message;
                result.result_code = ResultCode.Failure_InternalServiceError;

                return false;
                }

            return true;
            }


        //+///////////////////////////////////////////////////////////////////////
        public StructRetailers GetRetailers(StructGetRetailersArgs args)
            {
            string strIP = HttpContext.Current.Request.UserHostAddress;


            StructRetailers rets = new StructRetailers();
            rets.result = new StructResult();
            rets.retailers = new List<StructRetailer>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (!ServUtility.IsServiceAvailable(ref rets.result, args.locale))
                {
                return rets;
                }

            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref rets.result))
                    {
                    return rets;
                    }


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = @"SELECT id_retailer, name, description, picture FROM db_retailers;";

                command.Prepare();

                NpgsqlDataReader data = command.ExecuteReader();

                if (data.HasRows)
                    {
                    while (data.Read())
                        {
                        StructRetailer ret = new StructRetailer();

                        ret.id = data.GetInt32(0);
                        ret.name = data.GetString(1);

                        if (!data.IsDBNull(2))
                            ret.description = data.GetString(2);

                        if (!data.IsDBNull(3))
                            ret.picture = data.GetString(3);

                        rets.retailers.Add(ret);
                        }
                    }



                return rets;
                }
            catch (Exception e)
                {
                rets.result.message = e.Message;
                rets.result.result_code = ResultCode.Failure_InternalServiceError;

                return rets;
                }
            finally
                {
                sw.Stop();
				
                if (conn != null)
                    {
                    string strBody = ServUtility.GetRequestBody();

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, rets.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }











        //+///////////////////////////////////////////////////////////////////////
        public StructResult TestOpenCV()
            {
            StructResult res = new StructResult();

            NpgsqlConnection conn = null;

            try
                {
                if (!Connect(out conn, ref res))
                    {
                    return res;
                    }

                //IntPtr zero = IntPtr.Zero;
                //cvReleaseImage(ref zero);


                //string strPicturesBaseUrl = ServUtility.ReadStringFromConfig("pictures_url");
                //string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");

                NpgsqlCommand command2 = new NpgsqlCommand();
                command2.Connection = conn;
                command2.CommandText = "SELECT picture FROM db_product_pictures LIMIT 1;";
                command2.Prepare();

                NpgsqlDataReader data2 = command2.ExecuteReader();

                if (data2.HasRows)
                    {
                    data2.Read();

                    byte[] pic = ServUtility.GetBinaryFieldValue(ref data2, 0);

                    List<KeyValuePair<double, CvScalar>> dom1 = ServUtility.GetDomColors(pic);
                    double dblDist = ServUtility.calcColorDistance(dom1, dom1);


                    MemoryStream ms = new MemoryStream(pic);

                    Image imgPhoto = Image.FromStream(ms);

                    //StructColorImage im = ServUtility.LoadImage(imgPhoto);
                    //float[] fDesc = fftwf_color_gist_scaletab_(im.width, im.height, im.c1, im.c2, im.c3);

                    StructBWImage im2 = ServUtility.LoadBWImage(imgPhoto);
                    //float[] fDesc2 = fftwf_bw_gist_scaletab_(im2.width, im2.height, im2.c1);

                    //float[] fDescBOW = calcBOWDescriptors_(pic, pic.Count(), @"/var/www/main_sarafan/vocabulary.yml");


                    res.message = dom1[0].Key.ToString() + "; " + dblDist.ToString();
                    }
                
                return res;
                }
            catch (Exception e)
                {
                res.message = e.Message + "; " + e.InnerException.Message;
                res.result_code = ResultCode.Failure_InternalServiceError;

                return res;
                }
            finally
                {
                conn.Close();
                }
            }



        //+///////////////////////////////////////////////////////////////////////
        public string TestMath()
            {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            double dblSum = 0.0;

            for(int i = 0; i < 100000000; ++i)
                {
                dblSum += (i * 3.14);
                }

            sw.Stop();

            return sw.ElapsedMilliseconds.ToString();
            }


        //+///////////////////////////////////////////////////////////////////////
        public string TestDB()
            {
            StructResult result = new StructResult();

            NpgsqlConnection conn = null;

            if (!Connect(out conn, ref result))
                {
                return "";
                }


            Stopwatch sw = new Stopwatch();
            sw.Start();

                NpgsqlCommand commandTest = new NpgsqlCommand();
                commandTest.Connection = conn;
                commandTest.CommandText = "SELECT id_product_picture, id_product, dom_colors FROM db_product_pictures LIMIT 50000;";
                commandTest.Prepare();
                NpgsqlDataReader dataTest = commandTest.ExecuteReader();

            sw.Stop();

            return sw.ElapsedMilliseconds.ToString();
            }

        
        
        //+///////////////////////////////////////////////////////////////////////
        public StructFindSimiliarResult FindSimiliarProducts(StructFindSimiliarProductsArgs args)
            {
            StructFindSimiliarResult id = new StructFindSimiliarResult();
            id.result = new StructResult();
            id.offers = new List<StructSimiliarProduct>();

            Stopwatch sw = new Stopwatch();
            sw.Start();


            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            if (!ServUtility.IsServiceAvailable(ref id.result, args.locale))
                {
                return id;
                }


            //int nIdUser = -1;
            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref id.result))
                    {
                    return id;
                    }

                NpgsqlCommand commandProducts = new NpgsqlCommand();
                commandProducts.Connection = conn;
                commandProducts.CommandText = @"SELECT dp.product_code, dp.name, dp.imageurl, dp.buyurl, dpd.distance FROM dbl_product_distance dpd, db_products dp 
												WHERE dp.id_product = dpd.id_product_2 AND dpd.id_product_1 IN 
												(SELECT id_product FROM db_products WHERE id_retailer = :idr AND product_code = :pc) ORDER BY dpd.distance LIMIT 5;";
                commandProducts.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
                commandProducts.Parameters.Add(new NpgsqlParameter("pc", DbType.String));
                commandProducts.Prepare();

                commandProducts.Parameters[0].Value = args.id_retailer;
                commandProducts.Parameters[1].Value = args.id_offer;

                NpgsqlDataReader dataProducts = commandProducts.ExecuteReader();

                if (dataProducts.HasRows)
                    {
                    while(dataProducts.Read())
						{
						string strProductCode = dataProducts.GetString(0);

                        string strName = dataProducts.GetString(1);
                        string strImageurl = dataProducts.GetString(2);
                        string strBuyurl = dataProducts.GetString(3);
                        double dblDistance = dataProducts.GetDouble(4);

						StructSimiliarProduct pr = new StructSimiliarProduct();
						pr.id_offer = strProductCode;
						pr.name = strName;
						pr.image_url = strImageurl;
						pr.buy_url = strBuyurl;
						pr.dom_dist = dblDistance;

						id.offers.Add(pr);
						}
                        
                    }

				NpgsqlCommand commandHist = new NpgsqlCommand();
                commandHist.Connection = conn;
                commandHist.CommandText = "INSERT INTO db_widget_search_history(id_retailer, product_code, found_count) VALUES (:idr, :pc, :fc);";
                commandHist.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
                commandHist.Parameters.Add(new NpgsqlParameter("pc", DbType.String));
                commandHist.Parameters.Add(new NpgsqlParameter("fc", DbType.Int32));

                commandHist.Prepare();

                commandHist.Parameters[0].Value = args.id_retailer;
                commandHist.Parameters[1].Value = args.id_offer;
                commandHist.Parameters[2].Value = id.offers.Count;

                commandHist.ExecuteNonQuery();

                return id;
                }
            catch (Exception e)
                {
                id.result.message = e.Message;
                id.result.result_code = ResultCode.Failure_InternalServiceError;

                return id;
                }
            finally
                {
                sw.Stop();

                if (conn != null)
                    {
                    string strBody = ServUtility.GetRequestBody();

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, "found: " + id.offers.Count.ToString(), 
												args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }

        
        }

    }