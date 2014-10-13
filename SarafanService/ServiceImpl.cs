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


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                //command.CommandText = @"INSERT INTO db_model_pictures DEFAULT VALUES RETURNING id_model_picture;";
                command.CommandText = @"INSERT INTO db_model_pictures(picture, descr, descr_bw) VALUES (:p, :d, :d2) RETURNING id_model_picture;";

                command.Parameters.Add(new NpgsqlParameter("p", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("d", DbType.Binary));
                command.Parameters.Add(new NpgsqlParameter("d2", DbType.Binary));

                command.Prepare();

                command.Parameters[0].Value = btPic;
                command.Parameters[1].Value = desc;
                command.Parameters[2].Value = desc2;

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
            public double distance;
            public int id_product_photo;
            }


        //+///////////////////////////////////////////////////////////////////////
        public StructFindRequestId FindProducts(StructFindProductsArgs2 args)
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
                        pd.id_product = nId;

                        if (!lstFound.Exists(prData => prData.id_product == nId))
                            lstFound.Add(pd);
                        }
                    }

                if (lstFound.Count > 0)
                    {
                    float[] descrModel = null;

                    NpgsqlCommand commandModel = new NpgsqlCommand();
                    commandModel.Connection = conn;
                    commandModel.CommandText = "SELECT descr_bw FROM db_model_pictures WHERE id_model_picture = :idmp;";
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
                        }

                    if (descrModel == null || descrModel.GetLength(0) == 0)
                        {
                        return id;
                        }

                    string strSQLDesc = "SELECT id_product_picture, id_product, descr_bw FROM db_product_pictures WHERE id_product IN ({0});";

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
                            }
                        }

                    for (int i = 0; i < lstFound.Count; ++i)
                        {
                        double dst = 100.0;
                        int nIdPicBest = 0;

                        for (int j = 0; j < lstFound[i].descriptors.Count; ++j)
                            {
                            double dist = ServUtility.CalcDistance(lstFound[i].descriptors[j].Item1, descrModel);

                            if (dist < dst)
                                {
                                dst = dist;
                                nIdPicBest = lstFound[i].descriptors[j].Item2;
                                }
                            }

                        lstFound[i].distance = dst;
                        lstFound[i].id_product_photo = nIdPicBest;
                        }

                    lstFound.Sort(delegate(StructProductData p1, StructProductData p2)
                        {
                        return p1.distance.CompareTo(p2.distance);
                        }
                        );

                    }


                id.dbg = "products found by filters = " + lstFound.Count.ToString();

                if(lstFound.Count > 1000)
                    {
                    lstFound.RemoveRange(1000, lstFound.Count - 1000);
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


                    string strSQLInsert = @"INSERT INTO db_search_results(id_search_request, id_product, distance, id_product_picture) VALUES ";

                    strSQLInsert += "\n(" + nIdRequest.ToString() + ", " + lstFound[0].id_product.ToString() + ", " + lstFound[0].distance.ToString() + ", " + lstFound[0].id_product_photo.ToString() + ")";

                    for (int i = 1; i < lstFound.Count; ++i)
                        {
                        strSQLInsert += ",\n" + "\n(" + nIdRequest.ToString() + ", " + lstFound[i].id_product.ToString() + ", " + lstFound[i].distance.ToString() + ", " + lstFound[i].id_product_photo.ToString() + ")";
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

                command.CommandText = @"SELECT idp, first(inn.nm), first(price), first(bu), first(descr), first(idr), first(cur), COUNT(ipp), string_agg(CAST (ipp AS text), '#'), string_agg(CAST (width AS text), '#'), string_agg(CAST (height AS text), '#'), string_agg(ff, '#'), first(dist) dist, first(idpp) idpp 
                                        FROM (SELECT p.id_product idp, p.name nm, p.retailprice * 100 price, p.buyurl bu, p.imageurl iu, p.id_retailer idr, pp.id_product_picture ipp, pp.width width, pp.height height, pp.file_format ff, p.currency cur, p.description descr, sr.distance dist, sr.id_product_picture idpp
                                        FROM db_search_results sr
                                        INNER JOIN db_products p ON sr.id_search_request = :isr AND p.id_product = sr.id_product 
                                        LEFT JOIN db_product_pictures pp ON pp.id_product = p.id_product
                                        ORDER BY sr.id_search_result
                                        ) AS inn
                                        GROUP BY inn.idp
                                        ORDER BY dist
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
                            {
                            nMostRelevantPictureId = data.GetInt32(13);
                            }

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

                            if(nIdPicture == nMostRelevantPictureId)
                                {
                                product.picture = picture.url;
                                product.picture_width = picture.width;
                                product.picture_height = picture.height;
                                //product.most_relevant_picture = picture.url;
                                }

                            product.pictures.Add(picture);
                            }

                        //if (product.pictures != null && product.pictures.Count > 0)
                        //    {
                        //    }

                        products.products.Add(product);
                        }
                    }

                products.products.Sort(delegate(StructProduct p1, StructProduct p2)
                    {
                    return p1.distance.CompareTo(p2.distance);
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



        
        }

    }