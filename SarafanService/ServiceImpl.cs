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

namespace SarafanService
    {
    public partial class ServiceModule
        {


        //+///////////////////////////////////////////////////////////////////////
        public StructPicturesCompared ComparePictures(StructComparePicturesArgs args)
            {
            StructPicturesCompared rets = new StructPicturesCompared();
            rets.result = new StructResult();

            Stopwatch sw = new Stopwatch();
            sw.Start();


            try
                {
                byte[] btPic = Convert.FromBase64String(args.picture1);

                MemoryStream ms = new MemoryStream(btPic);

                Image imgPhoto = Image.FromStream(ms);

                StructBWImage im = ServUtility.LoadBWImage(imgPhoto);

                rets.descs_1 = fftwf_bw_gist_scaletab_(im.width, im.height, im.c1);



                btPic = Convert.FromBase64String(args.picture2);

                ms = new MemoryStream(btPic);

                imgPhoto = Image.FromStream(ms);

                im = ServUtility.LoadBWImage(imgPhoto);

                rets.descs_2 = fftwf_bw_gist_scaletab_(im.width, im.height, im.c1);


                rets.distance = ServUtility.CalcDistance(rets.descs_1, rets.descs_2);

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
                }
            }




        //+///////////////////////////////////////////////////////////////////////
        public StructResult ResetDescriptors()
        {
        StructResult res = new StructResult();

        try
            {
            Thread newThread = new Thread(ServiceModule.CalcDescriptorsThread);
            newThread.Start(this);

            return res;
            }
        catch (Exception e)
            {
            res.message = e.Message;
            res.result_code = ResultCode.Failure_InternalServiceError;

            return res;
            }
        finally
            {
            }
        }

        
        
        //+///////////////////////////////////////////////////////////////////////
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void CalcDescriptorsThread(Object obj)
        {
        StructResult result = new StructResult();
        NpgsqlConnection conn = null;

        try
            {
            ServiceModule impl = (ServiceModule)obj;

            if (!impl.Connect(out conn, ref result))
                {
                return;
                }

            NpgsqlCommand command = new NpgsqlCommand();
            command.Connection = conn;

            command.CommandText = @"SELECT id_product_picture FROM db_product_pictures WHERE id_product IN (SELECT id_product FROM db_products WHERE id_retailer > 0 ) AND renewed = FALSE ORDER BY id_product_picture;";
            command.Prepare();

            NpgsqlCommand command2 = new NpgsqlCommand();
            command2.Connection = conn;

            command2.CommandText = @"SELECT picture FROM db_product_pictures WHERE id_product_picture = :idpp;";
            command2.Parameters.Add(new NpgsqlParameter("idpp", DbType.Int32));
            command2.Prepare();


            NpgsqlCommand commandUpd = new NpgsqlCommand();
            commandUpd.Connection = conn;

            commandUpd.CommandText = @"UPDATE db_product_pictures SET descr_bw = :d, renewed = TRUE WHERE id_product_picture = :idpp;";
            commandUpd.Parameters.Add(new NpgsqlParameter("d", DbType.Binary));
            commandUpd.Parameters.Add(new NpgsqlParameter("idpp", DbType.Int32));
            commandUpd.Prepare();


            NpgsqlDataReader data = command.ExecuteReader();

            if (data.HasRows)
                {
                while (data.Read())
                    {
                    int nIdPicture = data.GetInt32(0);

                    command2.Parameters[0].Value = nIdPicture;

                    NpgsqlDataReader data2 = command2.ExecuteReader();

                    if (data2.HasRows)
                        {
                        data2.Read();

                        if (!data2.IsDBNull(0))
                            {
                            long len = data2.GetBytes(0, 0, null, 0, 0);
                            byte[] arrPhoto = new Byte[len];

                            data2.GetBytes(0, 0, arrPhoto, 0, (int)len);

                            MemoryStream ms = new MemoryStream(arrPhoto);
                            Image imgPhoto = Image.FromStream(ms);


                            StructBWImage im = ServUtility.LoadBWImage(imgPhoto);

                            float[] descs = fftwf_bw_gist_scaletab_(im.width, im.height, im.c1);

                            byte[] descs_byte = ServUtility.GetFloatArrayAsByteArray(descs);


                            commandUpd.Parameters[0].Value = descs_byte;
                            commandUpd.Parameters[1].Value = nIdPicture;

                            commandUpd.ExecuteNonQuery();
                            }
                        }
                    }
                }

            }
        catch
            {
            }
        finally
            {
            if (conn != null)
                {
                conn.Close();
                }
            }
        }


        //+///////////////////////////////////////////////////////////////////////
        private bool Connect(out NpgsqlConnection conn, ref StructResult result)
            {
            conn = null;

            string strConnString = ServUtility.ReadStringFromConfig("connection_string");

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
        public StructFilters GetFilters(StructGetFiltersArgs args)
            {
            StructFilters filters = new StructFilters();
            filters.result = new StructResult();
            filters.filters = new List<StructFilter>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            if (!ServUtility.IsServiceAvailable(ref filters.result, args.locale))
                {
                return filters;
                }


            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref filters.result))
                    {
                    return filters;
                    }


                string strSQL = "";

                if (args.retailer_id_list == null || args.retailer_id_list.Count == 0)
                    {
                    strSQL = @"SELECT f.id_filter, f.name, f.description, f.type, f.multiselect, f.required, fi.id_filter_item, fi.item_value
                               FROM db_filters f
                               INNER JOIN db_filter_items fi ON f.id_filter = fi.id_filter
                               ORDER BY id_filter, id_filter_item;";
                    }
                else if (args.retailer_id_list.Count == 1)
                    {
                    strSQL = @"SELECT inn.*, fi.id_filter_item, fi.item_value FROM (
                                SELECT f.id_filter, f.name, f.description, f.type, f.multiselect, f.required
                                FROM db_filters f
                                INNER JOIN dbl_retailer_filters rf ON rf.id_filter = f.id_filter AND rf.id_retailer = {0}
                                ) AS inn
                                INNER JOIN db_filter_items fi ON inn.id_filter = fi.id_filter
                                ORDER BY inn.id_filter;";

                    strSQL = String.Format(strSQL, args.retailer_id_list[0]);
                    }
                else
                    {
                    strSQL = @"SELECT inn.*, fi.id_filter_item, fi.item_value FROM (
                                SELECT f.id_filter, f.name, f.description, f.type, f.multiselect, f.required
                                FROM db_filters f
                                INNER JOIN dbl_retailer_filters rf ON rf.id_filter = f.id_filter AND rf.id_retailer = {0}";

                    strSQL = String.Format(strSQL, args.retailer_id_list[0]);

                    string strSQL2 = @"INTERSECT
                                        SELECT f.id_filter, f.name, f.description, f.type, f.multiselect, f.required
                                        FROM db_filters f
                                        INNER JOIN dbl_retailer_filters rf ON rf.id_filter = f.id_filter AND rf.id_retailer = {0}";

                    for (int i = 1; i < args.retailer_id_list.Count; ++i)
                        {
                        strSQL += "\r\n" + String.Format(strSQL2, args.retailer_id_list[i]);
                        }

                    strSQL += "\r\n";

                    strSQL += @") AS inn
                                INNER JOIN db_filter_items fi ON inn.id_filter = fi.id_filter
                                ORDER BY inn.id_filter;";

                    }



                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = strSQL;

                command.Prepare();

                NpgsqlDataReader data = command.ExecuteReader();

                if (data.HasRows)
                    {
                    int nIdFilterPrev = -1;

                    StructFilter filter = new StructFilter();
                    filter.items = new List<StructFilterItem>();

                    while (data.Read())
                        {
                        int nFilterId = data.GetInt32(0);

                        if (nFilterId != nIdFilterPrev)
                            {
                            if (nIdFilterPrev != -1)
                                {
                                filters.filters.Add(filter);
                                }

                            nIdFilterPrev = nFilterId;

                            filter = new StructFilter();
                            filter.items = new List<StructFilterItem>();

                            filter.id = nFilterId;
                            filter.name = data.GetString(1);
                            filter.type = data.GetString(3);
                            filter.multiselect = data.GetBoolean(4);
                            filter.required = data.GetBoolean(5);

                            StructFilterItem item = new StructFilterItem();
                            item.item_id = data.GetInt32(6);
                            item.item_name = data.GetString(7);

                            filter.items.Add(item);
                            }
                        else
                            {
                            StructFilterItem item = new StructFilterItem();
                            item.item_id = data.GetInt32(6);
                            item.item_name = data.GetString(7);

                            filter.items.Add(item);
                            }
                        }

                    filters.filters.Add(filter);
                    }

                return filters;
                }
            catch (Exception e)
                {
                filters.result.message = e.Message;
                filters.result.result_code = ResultCode.Failure_InternalServiceError;

                return filters;
                }
            finally
                {
                sw.Stop();

                if (conn != null)
                    {
                    string strBody = ServUtility.GetRequestBody();

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, filters.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }



        //+///////////////////////////////////////////////////////////////////////
        public StructModelPictureId SetModelPicture(StructPutModelPictureArgs args)
            {
            StructModelPictureId id = new StructModelPictureId();
            id.result = new StructResult();
            id.model_picture_id = -1;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            if (!ServUtility.IsServiceAvailable(ref id.result, args.locale))
                {
                return id;
                }


            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref id.result))
                    {
                    return id;
                    }


                ServUtility.DeleteOutdatedSearchRequests(ref conn);
                ServUtility.DeleteOutdatedModelPictures(ref conn);

                byte[] btPic = Convert.FromBase64String(args.picture);

                MemoryStream ms = new MemoryStream(btPic);

                Image imgPhoto = Image.FromStream(ms);

                StructColorImage im = ServUtility.LoadImage(imgPhoto);

                float[] fDesc = fftwf_color_gist_scaletab_(im.width, im.height, im.c1, im.c2, im.c3);

                byte[] desc = ServUtility.GetFloatArrayAsByteArray(fDesc);


                StructBWImage im2 = ServUtility.LoadBWImage(imgPhoto);

                float[] fDesc2 = fftwf_bw_gist_scaletab_(im2.width, im2.height, im2.c1);

                byte[] desc2 = ServUtility.GetFloatArrayAsByteArray(fDesc2);


                List<KeyValuePair<double, CvScalar>> dom = ServUtility.GetDomColors(btPic);
                byte[] byte_dom = ServUtility.GetDomColorsAsByteArray(dom);


                float[] fDescBOW = calcBOWDescriptors_(btPic, btPic.Count(), @"/var/www/main_sarafan/vocabulary.yml");
                byte[] desc_bow = ServUtility.GetFloatArrayAsByteArray(fDescBOW);


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                //command.CommandText = @"INSERT INTO db_model_pictures DEFAULT VALUES RETURNING id_model_picture;";
                command.CommandText = @"INSERT INTO db_model_pictures(picture, descr, descr_bw, dom_colors, descr_bow) VALUES (:p, :d, :d2, :dom, :dbow) RETURNING id_model_picture;";

                command.Parameters.Add(new NpgsqlParameter("p", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("d", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("d2", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("dom", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("dbow", DbType.Binary));

                command.Prepare();

                command.Parameters[0].Value = btPic;
                command.Parameters[1].Value = desc;
                command.Parameters[2].Value = desc2;
                command.Parameters[3].Value = byte_dom;
                command.Parameters[4].Value = desc_bow;

                Object obj = command.ExecuteScalar();
                id.model_picture_id = (int)obj;


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

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }





        class StructProductData
            {
            public int id_product;
            public List<Tuple<float[], int>> descriptors;
            public List<Tuple<List<KeyValuePair<double, CvScalar>>, int>> dom;
            public List<Tuple<float[], int>> descriptorsBOW;
            public double distance_gist;
            public double distance_dom;
            public double distance_dom_reduced;
            public double distance_bow;
            public int rank_gist;
            public int rank_dom;
            public int rank_bow;
            public int rank_sum;
            public int id_product_photo;

            public string name;
            public string imageurl;
            public string buyurl;
            public string offer_id;
            }


        //+///////////////////////////////////////////////////////////////////////
        public StructFindRequestId FindProductsOld(StructFindProductsArgs2 args)
            {
            StructFindRequestId id = new StructFindRequestId();
            id.result = new StructResult();
            id.find_request_id = -1;
            id.num_products_found = 0;

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

                ServUtility.DeleteOutdatedSearchRequests(ref conn);
                ServUtility.DeleteOutdatedModelPictures(ref conn);

                if (!ServUtility.IsModelPictureValid(ref conn, args.picture_id))
                    {
                    id.result.result_code = ResultCode.Failure_SessionExpired;
                    id.result.message = "Session expired";

                    return id;
                    }

                Dictionary<int, List<int>> dictFilterItems = new Dictionary<int, List<int>>();

                foreach (StructFilterItemPair2 pair in args.filter_item_list)
                    {
                    if (dictFilterItems.ContainsKey(pair.id_filter))
                        {
                        dictFilterItems[pair.id_filter].Add(pair.id_item);
                        }
                    else
                        {
                        dictFilterItems.Add(pair.id_filter, new List<int> { pair.id_item });
                        }
                    }


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                string strSQL = "";

                if (dictFilterItems.Count > 0)
                    {
                    strSQL = @"SELECT id_product FROM db_products WHERE id_product IN(
                            SELECT inn.id_product FROM 
                                    (
                                    ";

                    string strSQL2 = @"
                                SELECT DISTINCT id_product FROM dbl_product_filter_items
                                WHERE id_filter_item IN 
                                (SELECT id_filter_item FROM db_filter_items WHERE 
                                (id_filter = (SELECT id_filter FROM db_filters WHERE id_filter = {0}) AND ({1})))";

                    string strSQL3 = @"id_filter_item = {0}";

                    string strSQL4 = @") AS inn)
                                ";

                    foreach (var filter in dictFilterItems)
                        {
                        string strTmp = "";

                        foreach (int item in filter.Value)
                            {
                            strTmp += String.Format(strSQL3, item) + " OR ";
                            }

                        strTmp = strTmp.Substring(0, strTmp.Length - 4);

                        strTmp = String.Format(strSQL2, filter.Key, strTmp);

                        strSQL += "\n" + strTmp + "\n INTERSECT" + "\n";
                        }

                    strSQL = strSQL.Substring(0, strSQL.Length - 10) + strSQL4;

                    if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                        {
                        string strSQL6 = @" AND id_retailer IN ({0})
                                    ORDER BY id_product
                                    LIMIT 500000";

                        string strSQL7 = "";

                        strSQL7 = args.retailer_id_list[0].ToString();

                        for (int i = 1; i < args.retailer_id_list.Count; ++i)
                            {
                            strSQL7 += ", " + args.retailer_id_list[i].ToString();
                            }

                        strSQL6 = String.Format(strSQL6, strSQL7);

                        strSQL += "\n" + strSQL6 + "\n";
                        }
                    }
                else
                    {
                    if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                        {
                        strSQL = @"SELECT id_product FROM db_products WHERE id_retailer IN ({0}) ORDER BY id_product LIMIT 500000;";

                        if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                            {
                            string strList = args.retailer_id_list[0].ToString();

                            for (int i = 1; i < args.retailer_id_list.Count; ++i)
                                {
                                strList += ", " + args.retailer_id_list[i].ToString();
                                }

                            strSQL = String.Format(strSQL, strList);
                            }
                        }
                    else
                        {
                        strSQL = @"SELECT id_product FROM db_products ORDER BY id_product LIMIT 500000;";
                        }
                    }


                //id.dbg = strSQL;

                command.CommandText = strSQL;

                command.Prepare();

                NpgsqlDataReader data = command.ExecuteReader();

                List<StructProductData> lstFound = new List<StructProductData>();

                if (data.HasRows)
                    {
                    while (data.Read())
                        {
                        int nId = data.GetInt32(0);

                        StructProductData pd = new StructProductData();
                        pd.descriptors = new List<Tuple<float[], int>>();
                        pd.dom = new List<Tuple<List<KeyValuePair<double,CvScalar>>,int>>();
                        pd.id_product = nId;

                        if (!lstFound.Exists(prData => prData.id_product == nId))
                            lstFound.Add(pd);
                        }
                    }

                if (lstFound.Count > 0)
                    {
                    float[] descrModel = null;
                    List<KeyValuePair<double, CvScalar>> domModel = null;

                    NpgsqlCommand commandModel = new NpgsqlCommand();
                    commandModel.Connection = conn;
                    commandModel.CommandText = "SELECT descr_bw, dom_colors FROM db_model_pictures WHERE id_model_picture = :idmp;";
                    commandModel.Parameters.Add(new NpgsqlParameter("idmp", DbType.Int32));

                    commandModel.Prepare();

                    commandModel.Parameters[0].Value = args.picture_id;

                    NpgsqlDataReader dataModel = commandModel.ExecuteReader();

                    if (dataModel.HasRows)
                        {
                        dataModel.Read();

                        if (!dataModel.IsDBNull(0))
                            {
                            long len = dataModel.GetBytes(0, 0, null, 0, 0);
                            byte[] d = new Byte[len];

                            dataModel.GetBytes(0, 0, d, 0, (int)len);

                            descrModel = ServUtility.GetByteArrayAsFloatArray(d);
                            }

                        if (!dataModel.IsDBNull(1))
                            {
                            long len = dataModel.GetBytes(1, 0, null, 0, 0);
                            byte[] d = new Byte[len];

                            dataModel.GetBytes(1, 0, d, 0, (int)len);

                            domModel = ServUtility.GetByteArrayAsDomColors(d);
                            }
                        }

                    if (descrModel == null || descrModel.GetLength(0) == 0)
                        {
                        return id;
                        }

                    if (domModel == null || domModel.Count == 0)
                        {
                        return id;
                        }

                    string strSQLDesc = "SELECT id_product_picture, id_product, descr_bw, dom_colors FROM db_product_pictures WHERE id_product IN ({0});";

                    string strIdList = lstFound[0].id_product.ToString();

                    for (int j = 1; j < lstFound.Count; ++j)
                        {
                        strIdList += "," + lstFound[j].id_product.ToString();
                        }

                    strSQLDesc = String.Format(strSQLDesc, strIdList);

                    //id.dbg += ";   " + strSQLDesc;

                    NpgsqlCommand commandSelectDesc = new NpgsqlCommand();
                    commandSelectDesc.Connection = conn;

                    commandSelectDesc.CommandText = strSQLDesc;

                    commandSelectDesc.Prepare();


                    NpgsqlDataReader dataSelectDesc = commandSelectDesc.ExecuteReader();

                    if (dataSelectDesc.HasRows)
                        {
                        while (dataSelectDesc.Read())
                            {
                            int nIdPP = dataSelectDesc.GetInt32(0);
                            int nIdP = dataSelectDesc.GetInt32(1);

                            if (!dataSelectDesc.IsDBNull(2))
                                {
                                long len = dataSelectDesc.GetBytes(2, 0, null, 0, 0);
                                byte[] d = new Byte[len];

                                dataSelectDesc.GetBytes(2, 0, d, 0, (int)len);

                                float[] descr = ServUtility.GetByteArrayAsFloatArray(d);

                                int nIndex = lstFound.FindIndex(
                                    delegate(StructProductData pd)
                                        {
                                        return pd.id_product == nIdP;
                                        }
                                    );

                                if (nIndex >= 0)
                                    lstFound[nIndex].descriptors.Add(new Tuple<float[], int> (descr, nIdPP));
                                }

                            if (!dataSelectDesc.IsDBNull(3))
                                {
                                long len = dataSelectDesc.GetBytes(3, 0, null, 0, 0);
                                byte[] d = new Byte[len];

                                dataSelectDesc.GetBytes(3, 0, d, 0, (int)len);

                                List<KeyValuePair<double, CvScalar>> dom = ServUtility.GetByteArrayAsDomColors(d);

                                int nIndex = lstFound.FindIndex(
                                    delegate(StructProductData pd)
                                        {
                                        return pd.id_product == nIdP;
                                        }
                                    );

                                if (nIndex >= 0)
                                    lstFound[nIndex].dom.Add(new Tuple<List<KeyValuePair<double, CvScalar>>, int>(dom, nIdPP));
                                }
                            }
                        }

                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        double dst = 100.0;
                        int nIdPicBest = 0;

                        double dst_dom = 10000.0;

                        for (int j = 0; j < lstFound[i].descriptors.Count; ++j)
                            {
                            double dist = ServUtility.CalcDistance(lstFound[i].descriptors[j].Item1, descrModel);

                            if (dist < dst)
                                {
                                dst = dist;
                                nIdPicBest = lstFound[i].descriptors[j].Item2;
                                }

                            double dist_dom = ServUtility.calcColorDistance(lstFound[i].dom[j].Item1, domModel);

                            if (dist_dom < dst_dom)
                                {
                                dst_dom = dist_dom;
                                //nIdPicBest = lstFound[i].descriptors[j].Item2;
                                }
                            
                            }

                        lstFound[i].distance_gist = dst;
                        lstFound[i].id_product_photo = nIdPicBest;

                        lstFound[i].distance_dom = dst_dom;
                        }

                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                        {
                        return p1.distance_gist.CompareTo(p2.distance_gist);
                        }
                        );

                    for(int i = 0; i < lstFound.Count; ++i)
                        {
                        lstFound[i].rank_gist = i;
                        }



                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                        {
                        return p1.distance_dom.CompareTo(p2.distance_dom);
                        }
                        );

                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        lstFound[i].rank_dom = i;
                        }




                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        lstFound[i].rank_sum = /*lstFound[i].rank_gist +*/ lstFound[i].rank_dom;
                        }


                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                        {
                        return p1.rank_sum.CompareTo(p2.rank_sum);
                        }
                        );
                    }


                id.dbg2 = "products found by filters = " + lstFound.Count.ToString();

                const int nProductLimit = 500;

                
                if (lstFound.Count > nProductLimit)
                    {
                    lstFound.RemoveRange(nProductLimit, lstFound.Count - nProductLimit);
                    }


                id.num_products_found = lstFound.Count;

                if (lstFound.Count > 0)
                    {
                    NpgsqlCommand commandInsert1 = new NpgsqlCommand();
                    commandInsert1.Connection = conn;

                    commandInsert1.CommandText = @"INSERT INTO db_search_requests DEFAULT VALUES RETURNING id_search_request;";

                    commandInsert1.Prepare();

                    Object obj = commandInsert1.ExecuteScalar();

                    int nIdRequest = (int)obj;
                    id.find_request_id = nIdRequest;


                    string strSQLInsert = @"INSERT INTO db_search_results(id_search_request, id_product, distance, distance_dom, id_product_picture) VALUES ";

                    strSQLInsert += "\n(" + nIdRequest.ToString() + ", " + lstFound[0].id_product.ToString() + ", " + lstFound[0].distance_gist.ToString() + ", " + lstFound[0].distance_dom.ToString() + ", " + lstFound[0].id_product_photo.ToString() + ")";

                    for (int i = 1; i < lstFound.Count; ++i)
                        {
                        strSQLInsert += ",\n" + "\n(" + nIdRequest.ToString() + ", " + lstFound[i].id_product.ToString() + ", " + lstFound[i].distance_gist.ToString() + ", " + lstFound[i].distance_dom.ToString() + ", " + lstFound[i].id_product_photo.ToString() + ")";
                        }

                    //id.dbg += ";   " + strSQLInsert; 

                    commandInsert1.CommandText = strSQLInsert;

                    commandInsert1.Prepare();

                    commandInsert1.ExecuteNonQuery();
                    }

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

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }







        //+///////////////////////////////////////////////////////////////////////
        public StructFindRequestId FindProductsNew(StructFindProductsArgs2 args)
            {
            StructFindRequestId id = new StructFindRequestId();
            id.result = new StructResult();
            id.find_request_id = -1;
            id.num_products_found = 0;

            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            Stopwatch sw3 = new Stopwatch();
            Stopwatch sw4 = new Stopwatch();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            sw1.Start();



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

                ServUtility.DeleteOutdatedSearchRequests(ref conn);
                ServUtility.DeleteOutdatedModelPictures(ref conn);

                if (!ServUtility.IsModelPictureValid(ref conn, args.picture_id))
                    {
                    id.result.result_code = ResultCode.Failure_SessionExpired;
                    id.result.message = "Session expired";

                    return id;
                    }

                Dictionary<int, List<int>> dictFilterItems = new Dictionary<int, List<int>>();

                foreach (StructFilterItemPair2 pair in args.filter_item_list)
                    {
                    if (dictFilterItems.ContainsKey(pair.id_filter))
                        {
                        dictFilterItems[pair.id_filter].Add(pair.id_item);
                        }
                    else
                        {
                        dictFilterItems.Add(pair.id_filter, new List<int> { pair.id_item });
                        }
                    }


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                string strSQL = "";

                if (dictFilterItems.Count > 0)
                    {
                    strSQL = @"SELECT id_product FROM db_products WHERE id_product IN(
                            SELECT inn.id_product FROM 
                                    (
                                    ";

                    string strSQL2 = @"
                                SELECT DISTINCT id_product FROM dbl_product_filter_items
                                WHERE id_filter_item IN 
                                (SELECT id_filter_item FROM db_filter_items WHERE 
                                (id_filter = (SELECT id_filter FROM db_filters WHERE id_filter = {0}) AND ({1})))";

                    string strSQL3 = @"id_filter_item = {0}";

                    string strSQL4 = @") AS inn)
                                ";

                    foreach (var filter in dictFilterItems)
                        {
                        string strTmp = "";

                        foreach (int item in filter.Value)
                            {
                            strTmp += String.Format(strSQL3, item) + " OR ";
                            }

                        strTmp = strTmp.Substring(0, strTmp.Length - 4);

                        strTmp = String.Format(strSQL2, filter.Key, strTmp);

                        strSQL += "\n" + strTmp + "\n INTERSECT" + "\n";
                        }

                    strSQL = strSQL.Substring(0, strSQL.Length - 10) + strSQL4;

                    if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                        {
                        string strSQL6 = @" AND id_retailer IN ({0})
                                    ORDER BY id_product
                                    LIMIT 500000";

                        string strSQL7 = "";

                        strSQL7 = args.retailer_id_list[0].ToString();

                        for (int i = 1; i < args.retailer_id_list.Count; ++i)
                            {
                            strSQL7 += ", " + args.retailer_id_list[i].ToString();
                            }

                        strSQL6 = String.Format(strSQL6, strSQL7);

                        strSQL += "\n" + strSQL6 + "\n";
                        }
                    }
                else
                    {
                    if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                        {
                        strSQL = @"SELECT id_product FROM db_products WHERE id_retailer IN ({0}) ORDER BY id_product LIMIT 500000;";

                        if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
                            {
                            string strList = args.retailer_id_list[0].ToString();

                            for (int i = 1; i < args.retailer_id_list.Count; ++i)
                                {
                                strList += ", " + args.retailer_id_list[i].ToString();
                                }

                            strSQL = String.Format(strSQL, strList);
                            }
                        }
                    else
                        {
                        strSQL = @"SELECT id_product FROM db_products ORDER BY id_product LIMIT 500000;";
                        }
                    }


                id.dbg1 = strSQL;

                command.CommandText = strSQL;

                command.Prepare();

                NpgsqlDataReader data = command.ExecuteReader();

                List<StructProductData> lstFound = new List<StructProductData>();
                Dictionary<int, StructProductData> dictFound = new Dictionary<int, StructProductData>();

                if (data.HasRows)
                    {
                    while (data.Read())
                        {
                        int nId = data.GetInt32(0);

                        StructProductData pd = new StructProductData();
                        pd.descriptors = new List<Tuple<float[], int>>();
                        pd.dom = new List<Tuple<List<KeyValuePair<double, CvScalar>>, int>>();
                        pd.descriptorsBOW = new List<Tuple<float[], int>>();
                        pd.id_product = nId;

                        if (!dictFound.ContainsKey(nId))
                            {
                            dictFound.Add(nId, pd);
                            }

                        //if (!lstFound.Exists(prData => prData.id_product == nId))
                        //    {
                        //    lstFound.Add(pd);
                        //    }
                        }
                    }

                sw1.Stop();

                //if (lstFound.Count > 0)
                if(dictFound.Count > 0)
                    {
                    float[] descrModel = null;
                    List<KeyValuePair<double, CvScalar>> domModel = null;
                    List<KeyValuePair<double, CvScalar>> domModelReduced = null;
                    float[] descrBOWModel = null;

                    NpgsqlCommand commandModel = new NpgsqlCommand();
                    commandModel.Connection = conn;
                    commandModel.CommandText = "SELECT descr_bw, dom_colors, descr_bow FROM db_model_pictures WHERE id_model_picture = :idmp;";
                    commandModel.Parameters.Add(new NpgsqlParameter("idmp", DbType.Int32));

                    commandModel.Prepare();

                    commandModel.Parameters[0].Value = args.picture_id;

                    NpgsqlDataReader dataModel = commandModel.ExecuteReader();

                    if (dataModel.HasRows)
                        {
                        dataModel.Read();

                        if (!dataModel.IsDBNull(0))
                            {
                            long len = dataModel.GetBytes(0, 0, null, 0, 0);
                            byte[] d = new Byte[len];

                            dataModel.GetBytes(0, 0, d, 0, (int)len);

                            descrModel = ServUtility.GetByteArrayAsFloatArray(d);
                            }

                        if (!dataModel.IsDBNull(1))
                            {
                            long len = dataModel.GetBytes(1, 0, null, 0, 0);
                            byte[] d = new Byte[len];

                            dataModel.GetBytes(1, 0, d, 0, (int)len);

                            domModel = ServUtility.GetByteArrayAsDomColors(d);

                            domModelReduced = ServUtility.ReduceDomColors(domModel);
                            }
                        
                        if (!dataModel.IsDBNull(2))
                            {
                            long len = dataModel.GetBytes(2, 0, null, 0, 0);
                            byte[] d = new Byte[len];

                            dataModel.GetBytes(2, 0, d, 0, (int)len);

                            descrBOWModel = ServUtility.GetByteArrayAsFloatArray(d);
                            }
                        }

                    //if (descrModel == null || descrModel.GetLength(0) == 0)
                    //    {
                    //    return id;
                    //    }

                    if (domModel == null || domModel.Count == 0)
                        {
                        return id;
                        }

                    //if (descrBOWModel == null || descrBOWModel.GetLength(0) == 0)
                    //    {
                    //    return id;
                    //    }

                    sw2.Start();

                    //string strSQLDesc = "SELECT id_product_picture, id_product, descr_bw, dom_colors, descr_bow FROM db_product_pictures WHERE id_product IN ({0});";
                    string strSQLDesc = "SELECT id_product_picture, id_product, dom_colors FROM db_product_pictures WHERE id_product IN ({0});";

                    StringBuilder bld = new StringBuilder();

                    //bld.Append(lstFound[0].id_product.ToString());
                    //string strIdList = lstFound[0].id_product.ToString();

                    //for (int j = 1; j < lstFound.Count; ++j)
                    //    {
                    //    bld.Append("," + lstFound[j].id_product.ToString());
                    //    //strIdList += "," + lstFound[j].id_product.ToString();
                    //    }

                    //bld.Append(lstFound[0].id_product.ToString());

                    int nCnt = 0;

                    foreach(StructProductData dt in dictFound.Values)
                        {
                        if(nCnt == 0)
                            bld.Append(dt.id_product.ToString());
                        else
                            bld.Append("," + dt.id_product.ToString());

                        nCnt++;
                        }

                    string strIdList = bld.ToString();

                    strSQLDesc = String.Format(strSQLDesc, strIdList);

                    id.dbg1 += ";   " + strSQLDesc;

                    NpgsqlCommand commandSelectDesc = new NpgsqlCommand();
                    commandSelectDesc.Connection = conn;

                    commandSelectDesc.CommandText = strSQLDesc;

                    commandSelectDesc.Prepare();


                    NpgsqlDataReader dataSelectDesc = commandSelectDesc.ExecuteReader();

                    if (dataSelectDesc.HasRows)
                        {
                        while (dataSelectDesc.Read())
                            {
                            int nIdPP = dataSelectDesc.GetInt32(0);
                            int nIdP = dataSelectDesc.GetInt32(1);

                            //if (!dataSelectDesc.IsDBNull(2))
                            //    {
                            //    long len = dataSelectDesc.GetBytes(2, 0, null, 0, 0);
                            //    byte[] d = new Byte[len];

                            //    dataSelectDesc.GetBytes(2, 0, d, 0, (int)len);

                            //    float[] descr = ServUtility.GetByteArrayAsFloatArray(d);

                            //    int nIndex = lstFound.FindIndex(
                            //        delegate(StructProductData pd)
                            //            {
                            //            return pd.id_product == nIdP;
                            //            }
                            //        );

                            //    if (nIndex >= 0)
                            //        lstFound[nIndex].descriptors.Add(new Tuple<float[], int>(descr, nIdPP));
                            //    }

                            if (!dataSelectDesc.IsDBNull(2))
                                {
                                long len = dataSelectDesc.GetBytes(2, 0, null, 0, 0);
                                byte[] d = new Byte[len];

                                dataSelectDesc.GetBytes(2, 0, d, 0, (int)len);

                                List<KeyValuePair<double, CvScalar>> dom = ServUtility.GetByteArrayAsDomColors(d);

                                dictFound[nIdP].dom.Add(new Tuple<List<KeyValuePair<double, CvScalar>>, int>(dom, nIdPP));

                                //int nIndex = lstFound.FindIndex(
                                //    delegate(StructProductData pd)
                                //        {
                                //        return pd.id_product == nIdP;
                                //        }
                                //    );

                                //if (nIndex >= 0)
                                //    lstFound[nIndex].dom.Add(new Tuple<List<KeyValuePair<double, CvScalar>>, int>(dom, nIdPP));
                                }
                            
                            //if (!dataSelectDesc.IsDBNull(4))
                            //    {
                            //    long len = dataSelectDesc.GetBytes(4, 0, null, 0, 0);
                            //    byte[] d = new Byte[len];

                            //    dataSelectDesc.GetBytes(4, 0, d, 0, (int)len);

                            //    float[] descr = ServUtility.GetByteArrayAsFloatArray(d);

                            //    int nIndex = lstFound.FindIndex(
                            //        delegate(StructProductData pd)
                            //            {
                            //            return pd.id_product == nIdP;
                            //            }
                            //        );

                            //    if (nIndex >= 0)
                            //        lstFound[nIndex].descriptorsBOW.Add(new Tuple<float[], int>(descr, nIdPP));
                            //    }
                            }
                        }

                    lstFound.Clear();

                    foreach(StructProductData st in dictFound.Values)
                        {
                        lstFound.Add(st);
                        }

                    //id.dbg3 = lstFound[0].dom[0].Item1..ToString() + "; " + lstFound[0].dom[0].Item2.

                    sw2.Stop();
                    sw3.Start();

                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        double dst = 100.0;
                        double dst_dom = 10000.0;
                        double dst_bow = 10000.0;
                        int nIdPicBest = 0;


                        for (int j = 0; j < lstFound[i].dom.Count; ++j)
                            {
                            //double dist = ServUtility.CalcDistance(lstFound[i].descriptors[j].Item1, descrModel);

                            //if (dist < dst)
                            //    {
                            //    dst = dist;
                            //    nIdPicBest = lstFound[i].descriptors[j].Item2;
                            //    }

                            List<KeyValuePair<double, CvScalar>> domReduced = ServUtility.ReduceDomColors(lstFound[i].dom[j].Item1);

                            double dist_dom_reduced = 1000.0;


                            //{
                            //KeyValuePair<double, CvScalar> domReducedLab = new KeyValuePair<double, CvScalar>(1.0, ServUtility.RGB2LAB(domReduced[0].Value));
                            //KeyValuePair<double, CvScalar> domModelReducedLab = new KeyValuePair<double, CvScalar>(1.0, ServUtility.RGB2LAB(domModelReduced[0].Value));

                            //List<KeyValuePair<double, CvScalar>> lstReducedLab = new List<KeyValuePair<double, CvScalar>>();
                            //lstReducedLab.Add(domReducedLab);

                            //List<KeyValuePair<double, CvScalar>> lstModelReducedLab = new List<KeyValuePair<double, CvScalar>>();
                            //lstModelReducedLab.Add(domModelReducedLab);

                            //dist_dom_reduced = ServUtility.calcColorDistanceSimplified(lstReducedLab, lstModelReducedLab);
                            //}

                            dist_dom_reduced = ServUtility.calcColorDistance(domReduced, domModelReduced);



                            //if(j == 0)
                            //    {
                            //    double dist_dom_reduced2 = ServUtility.calcColorDistanceSimplified(domReduced, domModelReduced);

                            //    id.dbg2 = "var1 = " + dist_dom_reduced.ToString() + "; var2= " + dist_dom_reduced2.ToString() + "; var1_lab = " + dist_dom_reduced_lab.ToString() + "; ";
                            //    }

                            //d=sqrt((r2-r1)^2+(g2-g1)^2+(b2-b1)^2)

                            if (dist_dom_reduced > 15.0)
                                continue;

                            double dist_dom = ServUtility.calcColorDistance(lstFound[i].dom[j].Item1, domModel);

                            if (dist_dom < dst_dom)
                                {
                                dst_dom = dist_dom;
                                nIdPicBest = lstFound[i].dom[j].Item2;
                                }



                            //double dist_bow = ServUtility.CalcDistance(lstFound[i].descriptorsBOW[j].Item1, descrBOWModel);

                            //if (dist_bow < dst_bow)
                            //    {
                            //    dst_bow = dist_bow;
                            //    //nIdPicBest = lstFound[i].descriptors[j].Item2;
                            //    }
                            }

                        lstFound[i].distance_gist = dst;
                        lstFound[i].id_product_photo = nIdPicBest;

                        lstFound[i].distance_dom = dst_dom;
                        lstFound[i].distance_bow = dst_bow;
                        }

                    //lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                    //{
                    //    return p1.distance_gist.CompareTo(p2.distance_gist);
                    //}
                    //    );

                    //for (int i = 0; i < lstFound.Count; ++i)
                    //    {
                    //    lstFound[i].rank_gist = i;
                    //    }



                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                    {
                        return p1.distance_dom.CompareTo(p2.distance_dom);
                    }
                        );

                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        lstFound[i].rank_dom = i;
                        }


                    //lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                    //{
                    //    return p1.distance_bow.CompareTo(p2.distance_bow);
                    //}
                    //    );

                    //for (int i = 0; i < lstFound.Count; ++i)
                    //    {
                    //    lstFound[i].rank_bow = i;
                    //    }




                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        //lstFound[i].rank_sum = /*lstFound[i].rank_gist + lstFound[i].rank_dom +*/ lstFound[i].rank_bow;
                        lstFound[i].rank_sum = lstFound[i].rank_dom;
                        }


                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                    {
                        return p1.rank_sum.CompareTo(p2.rank_sum);
                    }
                        );
                    }


                id.dbg2 += "products found by filters = " + lstFound.Count.ToString();

                const int nProductLimit = 200;

                if (lstFound.Count > nProductLimit)
                    {
                    lstFound.RemoveRange(nProductLimit, lstFound.Count - nProductLimit);
                    }

                lstFound.RemoveAll(delegate(StructProductData p)
                    {
                    return p.distance_dom >= 15;
                    }
                    );

                //string strOrder = "";

                //foreach (StructProductData pr in lstFound)
                //    {
                //    strOrder += "id_product = " + pr.id_product.ToString() + "; " + "rank = " + pr.rank_sum.ToString() + "; " + "rank_gist = " + pr.rank_gist.ToString() + "; " + "rank_dom = " + pr.rank_dom.ToString() + "; " + "dist_gist = " + pr.distance_gist.ToString() + "; " + "dist_dom = " + pr.distance_dom.ToString() + "; " + "dist_bow = " + pr.distance_bow.ToString()  + " ### ";
                //    }

                //id.dbg += "; " + strOrder;

                sw3.Stop();
                sw4.Start();

                id.num_products_found = lstFound.Count;

                if (lstFound.Count > 0)
                    {
                    NpgsqlCommand commandInsert1 = new NpgsqlCommand();
                    commandInsert1.Connection = conn;

                    commandInsert1.CommandText = @"INSERT INTO db_search_requests DEFAULT VALUES RETURNING id_search_request;";

                    commandInsert1.Prepare();

                    Object obj = commandInsert1.ExecuteScalar();

                    int nIdRequest = (int)obj;
                    id.find_request_id = nIdRequest;

                    StringBuilder bld2 = new StringBuilder();

                    bld2.Append(@"INSERT INTO db_search_results(id_search_request, id_product, distance, distance_dom, distance_bow, id_product_picture) VALUES ");

                    bld2.Append("\n(" + nIdRequest.ToString() + ", " + lstFound[0].id_product.ToString() + ", " + lstFound[0].distance_gist.ToString() + ", " + lstFound[0].distance_dom.ToString() + ", " + lstFound[0].distance_bow.ToString() + ", " + lstFound[0].id_product_photo.ToString() + ")");

                    for (int i = 1; i < lstFound.Count; ++i)
                        {
                        bld2.Append(",\n" + "\n(" + nIdRequest.ToString() + ", " + lstFound[i].id_product.ToString() + ", " + lstFound[i].distance_gist.ToString() + ", " + lstFound[i].distance_dom.ToString() + ", " + lstFound[i].distance_bow.ToString() + ", " + lstFound[i].id_product_photo.ToString() + ")");
                        }

                    //id.dbg += ";   " + strSQLInsert; 

                    commandInsert1.CommandText = bld2.ToString();

                    commandInsert1.Prepare();

                    commandInsert1.ExecuteNonQuery();
                    }

                sw4.Stop();

                id.dbg3 = sw1.ElapsedMilliseconds.ToString() + "; " + sw2.ElapsedMilliseconds.ToString() + "; " + sw3.ElapsedMilliseconds.ToString() + "; " + sw4.ElapsedMilliseconds.ToString();

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

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, id.dbg2, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }







        //+///////////////////////////////////////////////////////////////////////
        public StructProducts GetProductsInfo(StructGetProductInfoArgs args)
            {
            StructProducts products = new StructProducts();
            products.result = new StructResult();
            products.products = new List<StructProduct>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            if (!ServUtility.IsServiceAvailable(ref products.result, args.locale))
                {
                return products;
                }


            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref products.result))
                    {
                    return products;
                    }

                ServUtility.DeleteOutdatedSearchRequests(ref conn);
                ServUtility.DeleteOutdatedModelPictures(ref conn);

                if (!ServUtility.IsSearchRequestValid(ref conn, args.find_request_id))
                    {
                    products.result.result_code = ResultCode.Failure_SessionExpired;
                    products.result.message = "Session expired";

                    return products;
                    }

                string strPicturesBaseUrl = ServUtility.ReadStringFromConfig("pictures_url");
                string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");


                NpgsqlCommand command2 = new NpgsqlCommand();
                command2.Connection = conn;
                command2.CommandText = "SELECT picture FROM db_product_pictures WHERE id_product_picture = :idpp;";
                command2.Parameters.Add(new NpgsqlParameter("idpp", DbType.Int32));
                command2.Prepare();


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = @"SELECT idp, first(inn.nm), first(price), first(bu), first(descr), first(idr), first(cur), COUNT(ipp), string_agg(CAST (ipp AS text), '#'), string_agg(CAST (width AS text), '#'), string_agg(CAST (height AS text), '#'), string_agg(ff, '#'), first(dist) dist, first(dist_dom) dist_dom, first(dist_bow) dist_bow, first(idpp) idpp, first(isr) isr 
                                        FROM (SELECT p.id_product idp, p.name nm, p.retailprice * 100 price, p.buyurl bu, p.imageurl iu, p.id_retailer idr, pp.id_product_picture ipp, pp.width width, pp.height height, pp.file_format ff, p.currency cur, p.description descr, sr.distance dist, sr.distance_dom dist_dom, sr.distance_bow dist_bow, sr.id_product_picture idpp, sr.id_search_result isr
                                        FROM db_search_results sr
                                        INNER JOIN db_products p ON sr.id_search_request = :isr AND p.id_product = sr.id_product 
                                        LEFT JOIN db_product_pictures pp ON pp.id_product = p.id_product
                                        ORDER BY sr.id_search_result
                                        ) AS inn
                                        GROUP BY inn.idp
                                        ORDER BY isr
                                        LIMIT :lmt
                                        OFFSET :off;";

                command.Parameters.Add(new NpgsqlParameter("isr", DbType.Int32));
                command.Parameters.Add(new NpgsqlParameter("lmt", DbType.Int32));
                command.Parameters.Add(new NpgsqlParameter("off", DbType.Int32));

                command.Prepare();

                command.Parameters[0].Value = args.find_request_id;
                command.Parameters[1].Value = args.max_items;
                command.Parameters[2].Value = args.offset;

                NpgsqlDataReader data = command.ExecuteReader();

                if (data.HasRows)
                    {
                    while (data.Read())
                        {
                        int nMostRelevantPictureId = 0;

                        StructProduct product = new StructProduct();
                        product.pictures = new List<StructPicture>();

                        product.id = data.GetInt32(0);
                        product.name = data.GetString(1);
                        product.price = data.GetDouble(2);
                        product.buyurl = data.GetString(3);
                        product.description = data.GetString(4);
                        product.retailer_id = data.GetInt32(5);
                        product.currency = data.GetString(6);

                        int nNumPictures = (int)data.GetInt64(7);

                        string strIdPicture = "";
                        string strWidth = "";
                        string strHeight = "";
                        string strFFormat = "";

                        if (!data.IsDBNull(8))
                            strIdPicture = data.GetString(8);

                        if (!data.IsDBNull(9))
                            strWidth = data.GetString(9);

                        if (!data.IsDBNull(10))
                            strHeight = data.GetString(10);

                        if (!data.IsDBNull(11))
                            strFFormat = data.GetString(11);

                        string[] arrIdPicture = strIdPicture.Split(new Char[] { '#' });
                        string[] arrWidth = strWidth.Split(new Char[] { '#' });
                        string[] arrHeight = strHeight.Split(new Char[] { '#' });
                        string[] arrFFormat = strFFormat.Split(new Char[] { '#' });

                        if (!data.IsDBNull(12))
                            product.distance = data.GetDouble(12);

                        if (!data.IsDBNull(13))
                            product.distance_dom = data.GetDouble(13);

                        if (!data.IsDBNull(14))
                            product.distance_bow = data.GetDouble(14);

                        if (!data.IsDBNull(15))
                            {
                            nMostRelevantPictureId = data.GetInt32(15);
                            }

                        product.id_search_result = data.GetInt32(16);

                        if (!(nNumPictures == arrIdPicture.GetLength(0) && nNumPictures == arrWidth.GetLength(0) && nNumPictures == arrHeight.GetLength(0) && nNumPictures == arrFFormat.GetLength(0)))
                            continue;



                        for (int i = 0; i < arrIdPicture.GetLength(0); ++i)
                            {
                            StructPicture picture = new StructPicture();

                            int nIdPicture = Convert.ToInt32(arrIdPicture[i]);
                            int nWidth = Convert.ToInt32(arrWidth[i]);
                            int nHeight = Convert.ToInt32(arrHeight[i]);
                            string strFileFormat = arrFFormat[i];

                            picture.id = nIdPicture;
                            picture.width = nWidth;
                            picture.height = nHeight;

                            string strFileName = "photo_" + nIdPicture.ToString() + "_1." + strFileFormat;

                            if (ServUtility.FileExists(strPicturesFolder + strFileName))
                                {
                                picture.url = strPicturesBaseUrl + strFileName;
                                }

                            else
                                {
                                command2.Parameters[0].Value = nIdPicture;

                                NpgsqlDataReader data2 = command2.ExecuteReader();

                                if (data2.HasRows)
                                    {
                                    data2.Read();

                                    byte[] arrPhoto = ServUtility.GetBinaryFieldValue(ref data2, 0);

                                    if (arrPhoto != null && arrPhoto.GetLength(0) > 0)
                                        {
                                        string strPhotoUrl = strPicturesBaseUrl + strFileName;
                                        string strPhotoPath = strPicturesFolder + strFileName;

                                        if (!ServUtility.IdenticalFileExists(strPhotoPath, arrPhoto.GetLength(0)))
                                            File.WriteAllBytes(strPhotoPath, arrPhoto);

                                        picture.url = strPhotoUrl;
                                        }
                                    }
                                }

                            //if(nIdPicture == nMostRelevantPictureId)
                            //    {
                            //    product.picture = picture.url;
                            //    product.picture_width = picture.width;
                            //    product.picture_height = picture.height;
                            //    //product.most_relevant_picture = picture.url;
                            //    }

                            product.pictures.Add(picture);
                            }

                        product.pictures.Sort(delegate(StructPicture p1, StructPicture p2)
                        {
                            return p1.id.CompareTo(p2.id);
                        }
                            );

                        if (product.pictures.Count > 0)
                            {
                            product.picture = product.pictures[0].url;
                            product.picture_width = product.pictures[0].width;
                            product.picture_height = product.pictures[0].height;
                            }

                        //if (product.pictures != null && product.pictures.Count > 0)
                        //    {
                        //    }

                        products.products.Add(product);
                        }
                    }

                products.products.Sort(delegate(StructProduct p1, StructProduct p2)
                    {
                    return p1.id_search_result.CompareTo(p2.id_search_result);
                    }
                    );


                return products;
                }
            catch (Exception e)
                {
                products.result.message = e.Message;
                products.result.result_code = ResultCode.Failure_InternalServiceError;

                return products;
                }
            finally
                {
                sw.Stop();

                if (conn != null)
                    {
                    string strBody = ServUtility.GetRequestBody();

                    try
                        {
                        ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, products.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);
                        }
                    catch
                        {
                        }

                    //if (products.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, products, "view", args.locale, strBody);

                    conn.Close();
                    }
                }
            }


        //+///////////////////////////////////////////////////////////////////////
        public StructModelPicture GetModelPicture(StructGetModelPictureArgs args)
            {
            StructModelPicture pic = new StructModelPicture();
            pic.result = new StructResult();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (args.locale == null || args.locale.Length == 0)
                args.locale = "en";

            if (!ServUtility.IsServiceAvailable(ref pic.result, args.locale))
                {
                return pic;
                }


            NpgsqlConnection conn = null;


            try
                {
                if (!Connect(out conn, ref pic.result))
                    {
                    return pic;
                    }

                string strPicturesBaseUrl = ServUtility.ReadStringFromConfig("pictures_url");
                string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");


                NpgsqlCommand command2 = new NpgsqlCommand();
                command2.Connection = conn;
                command2.CommandText = "SELECT picture FROM db_model_pictures WHERE id_model_picture = :idmp;";
                command2.Parameters.Add(new NpgsqlParameter("idmp", DbType.Int32));
                command2.Prepare();

                command2.Parameters[0].Value = args.id_model_picture;

                NpgsqlDataReader data2 = command2.ExecuteReader();

                if (data2.HasRows)
                    {
                    data2.Read();

                    byte[] arrPhoto = ServUtility.GetBinaryFieldValue(ref data2, 0);

                    if (arrPhoto != null && arrPhoto.GetLength(0) > 0)
                        {
                        string strFileFormat = ServUtility.GetImageFormat(ref arrPhoto);

                        if(strFileFormat.Length > 0)
                            {
                            string strFileName = "model_" + args.id_model_picture.ToString() + "_1." + strFileFormat;


                            string strPhotoUrl = strPicturesBaseUrl + strFileName;
                            string strPhotoPath = strPicturesFolder + strFileName;

                            if (!ServUtility.IdenticalFileExists(strPhotoPath, arrPhoto.GetLength(0)))
                                File.WriteAllBytes(strPhotoPath, arrPhoto);

                            pic.url = strPhotoUrl;
                            }

                        }
                    }

                return pic;
                }
            catch (Exception e)
                {
                pic.result.message = e.Message;
                pic.result.result_code = ResultCode.Failure_InternalServiceError;

                return pic;
                }
            finally
                {
                sw.Stop();

                if (conn != null)
                    {
                    string strBody = ServUtility.GetRequestBody();

                    try
                        {
                        ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, pic.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);
                        }
                    catch
                        {
                        }

                    //if (products.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, products, "view", args.locale, strBody);

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


                string strPicturesBaseUrl = ServUtility.ReadStringFromConfig("pictures_url");
                string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");

                NpgsqlCommand command2 = new NpgsqlCommand();
                command2.Connection = conn;
                command2.CommandText = "SELECT picture FROM db_product_pictures LIMIT 1;";
                command2.Prepare();

                NpgsqlDataReader data2 = command2.ExecuteReader();

                if (data2.HasRows)
                    {
                    data2.Read();

                    byte[] pic = ServUtility.GetBinaryFieldValue(ref data2, 0);

                    //List<KeyValuePair<double, CvScalar>> dom1 = ServUtility.GetDomColors(pic);
                    //double dblDist = ServUtility.calcColorDistance(dom1, dom1);

                    //res.message = dom1[0].Key.ToString() + "; " + dblDist.ToString();

                    float[] fDescBOW = calcBOWDescriptors_(pic, pic.Count(), @"/var/www/main_sarafan/vocabulary.yml");
                    byte[] desc_bow = ServUtility.GetFloatArrayAsByteArray(fDescBOW);

                    res.message = desc_bow.Count().ToString();
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
        public StructFindSimiliarResult FindSimiliarProducts(StructFindSimiliarProductsArgs args)
            {
            StructFindSimiliarResult id = new StructFindSimiliarResult();
            id.result = new StructResult();
            id.offers = new List<StructSimiliarProduct>();

            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            Stopwatch sw3 = new Stopwatch();
            Stopwatch sw4 = new Stopwatch();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            sw1.Start();



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

                NpgsqlCommand commandModel = new NpgsqlCommand();
                commandModel.Connection = conn;
                commandModel.CommandText = @"SELECT dbp.retailer_category, dbpp.dom_colors
                                            FROM db_products dbp, db_product_pictures dbpp
                                            WHERE dbp.id_product = dbpp.id_product AND dbp.product_code = :pc AND dbp.id_retailer = :idr;";
                commandModel.Parameters.Add(new NpgsqlParameter("pc", DbType.String));
                commandModel.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
                commandModel.Prepare();

                commandModel.Parameters[0].Value = args.offer_id;
                commandModel.Parameters[1].Value = args.id_retailer;

                NpgsqlDataReader dataModel = commandModel.ExecuteReader();

                if (dataModel.HasRows)
                    {
                    dataModel.Read();

                    int nRetailCategory = dataModel.GetInt32(0);

                    List<KeyValuePair<double, CvScalar>> domModel = null;
                    List<KeyValuePair<double, CvScalar>> domModelReduced = null;

                    long len = dataModel.GetBytes(1, 0, null, 0, 0);
                    byte[] d = new Byte[len];

                    dataModel.GetBytes(1, 0, d, 0, (int)len);

                    domModel = ServUtility.GetByteArrayAsDomColors(d);

                    domModelReduced = ServUtility.ReduceDomColors(domModel);

                    NpgsqlCommand commandProducts = new NpgsqlCommand();
                    commandProducts.Connection = conn;
                    commandProducts.CommandText = "SELECT id_product, name, imageurl, buyurl, product_code FROM db_products WHERE id_retailer = :idr AND retailer_category = :rt ORDER BY id_product LIMIT 500000;";
                    commandProducts.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
                    commandProducts.Parameters.Add(new NpgsqlParameter("rt", DbType.Int32));

                    commandProducts.Prepare();

                    commandProducts.Parameters[0].Value = 8;
                    commandProducts.Parameters[1].Value = nRetailCategory;

                    NpgsqlDataReader dataProducts = commandProducts.ExecuteReader();

                    List<StructProductData> lstFound = new List<StructProductData>();
                    Dictionary<int, StructProductData> dictFound = new Dictionary<int, StructProductData>();

                    if (dataProducts.HasRows)
                        {
                        while (dataProducts.Read())
                            {
                            int nId = dataProducts.GetInt32(0);

                            string strName = dataProducts.GetString(1);
                            string strImageurl = dataProducts.GetString(2);
                            string strBuyurl = dataProducts.GetString(3);
                            string strProductCode = dataProducts.GetString(4);

                            if(strProductCode == args.offer_id)
                                continue;

                            StructProductData pd = new StructProductData();
                            pd.descriptors = new List<Tuple<float[], int>>();
                            pd.dom = new List<Tuple<List<KeyValuePair<double, CvScalar>>, int>>();
                            pd.descriptorsBOW = new List<Tuple<float[], int>>();
                            pd.id_product = nId;
                            pd.name = strName;
                            pd.imageurl = strImageurl;
                            pd.buyurl = strBuyurl;
                            pd.offer_id = strProductCode;

                            if (!dictFound.ContainsKey(nId))
                                {
                                dictFound.Add(nId, pd);
                                }
                            }


                        string strSQLDesc = "SELECT id_product_picture, id_product, dom_colors FROM db_product_pictures WHERE id_product IN ({0});";

                        StringBuilder bld = new StringBuilder();

                        int nCnt = 0;

                        foreach (StructProductData dt in dictFound.Values)
                            {
                            if (nCnt == 0)
                                bld.Append(dt.id_product.ToString());
                            else
                                bld.Append("," + dt.id_product.ToString());

                            nCnt++;
                            }

                        string strIdList = bld.ToString();

                        strSQLDesc = String.Format(strSQLDesc, strIdList);

                        id.dbg1 += ";   " + strSQLDesc;

                        NpgsqlCommand commandSelectDesc = new NpgsqlCommand();
                        commandSelectDesc.Connection = conn;

                        commandSelectDesc.CommandText = strSQLDesc;

                        commandSelectDesc.Prepare();

                        NpgsqlDataReader dataSelectDesc = commandSelectDesc.ExecuteReader();

                        if (dataSelectDesc.HasRows)
                            {
                            while (dataSelectDesc.Read())
                                {
                                int nIdPP = dataSelectDesc.GetInt32(0);
                                int nIdP = dataSelectDesc.GetInt32(1);

                                if (!dataSelectDesc.IsDBNull(2))
                                    {
                                    long len_ = dataSelectDesc.GetBytes(2, 0, null, 0, 0);
                                    byte[] d_ = new Byte[len_];

                                    dataSelectDesc.GetBytes(2, 0, d_, 0, (int)len_);

                                    List<KeyValuePair<double, CvScalar>> dom = ServUtility.GetByteArrayAsDomColors(d_);

                                    dictFound[nIdP].dom.Add(new Tuple<List<KeyValuePair<double, CvScalar>>, int>(dom, nIdPP));
                                    }
                                }
                            }

                        lstFound.Clear();

                        foreach (StructProductData st in dictFound.Values)
                            {
                            lstFound.Add(st);
                            }

                        for (int i = 0; i < lstFound.Count; ++i)
                            {
                            double dst = 100.0;
                            double dst_dom = 10000.0;
                            double dst_bow = 10000.0;
                            int nIdPicBest = 0;


                            for (int j = 0; j < lstFound[i].dom.Count; ++j)
                                {
                                List<KeyValuePair<double, CvScalar>> domReduced = ServUtility.ReduceDomColors(lstFound[i].dom[j].Item1);

                                double dist_dom_reduced = 1000.0;

                                dist_dom_reduced = ServUtility.calcColorDistance(domReduced, domModelReduced);

                                if (dist_dom_reduced > 15.0)
                                    continue;

                                double dist_dom = ServUtility.calcColorDistance(lstFound[i].dom[j].Item1, domModel);

                                if (dist_dom < dst_dom)
                                    {
                                    dst_dom = dist_dom;
                                    nIdPicBest = lstFound[i].dom[j].Item2;
                                    }
                                }

                            lstFound[i].distance_gist = dst;
                            lstFound[i].id_product_photo = nIdPicBest;

                            lstFound[i].distance_dom = dst_dom;
                            lstFound[i].distance_bow = dst_bow;
                            }

                        lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                            {
                            return p1.distance_dom.CompareTo(p2.distance_dom);
                            }
                            );

                        for (int i = 0; i < lstFound.Count; ++i)
                            {
                            lstFound[i].rank_dom = i;
                            }

                        for (int i = 0; i < lstFound.Count; ++i)
                            {
                            //lstFound[i].rank_sum = /*lstFound[i].rank_gist + lstFound[i].rank_dom +*/ lstFound[i].rank_bow;
                            lstFound[i].rank_sum = lstFound[i].rank_dom;
                            }


                        lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                            {
                            return p1.rank_sum.CompareTo(p2.rank_sum);
                            }
                            );
                        }


                    id.dbg2 += "products found by filters = " + lstFound.Count.ToString();

                    const int nProductLimit = 10;

                    if (lstFound.Count > nProductLimit)
                        {
                        lstFound.RemoveRange(nProductLimit, lstFound.Count - nProductLimit);
                        }

                    lstFound.RemoveAll(delegate(StructProductData p)
                        {
                        return p.distance_dom >= 15;
                        }
                        );

                    foreach(StructProductData dt in lstFound)
                        {
                        StructSimiliarProduct prd = new StructSimiliarProduct();
                        prd.offer_id = dt.offer_id;
                        prd.name = dt.name;
                        prd.image_url = dt.imageurl;
                        prd.buy_url = dt.buyurl;
                        prd.dom_dist = dt.distance_dom;

                        id.offers.Add(prd);
                        }   
                        
                    }

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

                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, id.dbg2, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }

        
        }

    }