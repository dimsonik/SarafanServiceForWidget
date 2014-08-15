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

    public class MyBootstrapper : Nancy.DefaultNancyBootstrapper
        {
        protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
            {
            base.ApplicationStartup(container, pipelines);

            pipelines.AfterRequest += AddHeader;
            }

        private void AddHeader(NancyContext ctx)
            {
            }
        }


    public class ServiceModule : NancyModule
        {

        public ServiceModule()
            {
            HttpApplicationState app = System.Web.HttpContext.Current.Application;

            //string strH = HttpContext.Current.Request.Headers["Accept"];


            //if(app["fav_nancy"] == null)
            //    app["fav_nancy"] = 0;
            //else
            //    app["fav_nancy"] = (int)app["fav_nancy"] + 1;



            Before += ctx =>
            {
                //string strH = "";

                //foreach (var head in ctx.Request.Headers)
                //    {
                //    strH += "; " + head.Key + " : " + head.Value.ToString();
                //    }

                //StreamWriter writer = new StreamWriter(@"/var/www/main_sarafan/req_headers_log.txt", true);
                //writer.WriteLine(strH);
                //writer.Close();

            return null;
            };


            //After += ctx =>
            //{
            //var h = ctx.Response.Headers;

            //ctx.Response.Headers.Remove("Link");
            //ctx.Response.Headers.Remove("Vary");


            //var h2 = h;
            //};



            /////////////////////////////////
            Get["/status"] = _ => "OK";


            /////////////////////////////////
            Get["/v1/status"] = _ => "OK";


            /////////////////////////////////
            Post["/v1/retailers/get"] = x =>
            {
            StructGetRetailersArgs args = new StructGetRetailersArgs();
            
            StructRetailers rets = new StructRetailers();
            rets.result = new StructResult();

            try
                {
                args = this.Bind<StructGetRetailersArgs>();
                }
            catch
                {
                rets.result.result_code = ResultCode.Failure_InvalidInputJson;
                rets.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(rets)
                    .WithContentType("application/json");
                }

            rets = GetRetailers(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(rets)
                .WithContentType("application/json");
            };



            /////////////////////////////////
            Post["/v1/filters/get"] = x =>
            {
            StructGetFiltersArgs args = new StructGetFiltersArgs();

            StructFilters filters = new StructFilters();
            filters.result = new StructResult();

            try
                {
                args = this.Bind<StructGetFiltersArgs>();
                }
            catch
                {
                filters.result.result_code = ResultCode.Failure_InvalidInputJson;
                filters.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(filters)
                    .WithContentType("application/json");
                }

            filters = GetFilters(args);

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(filters)
                    .WithContentType("application/json");
            };


            /////////////////////////////////
            Post["/v1/model_picture/put"] = x =>
            {
            StructPutModelPictureArgs args = new StructPutModelPictureArgs();
            
            StructModelPictureId id = new StructModelPictureId();
            id.result = new StructResult();

            try
                {
                args = this.Bind<StructPutModelPictureArgs>();
                }
            catch
                {
                id.result.result_code = ResultCode.Failure_InvalidInputJson;
                id.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(id)
                    .WithContentType("application/json");
                }

            id = SetModelPicture(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(id)
                .WithContentType("application/json");
            };


            ///////////////////////////////////
            //Post["/products/find"] = x =>
            //{
            //StructFindProductsArgs args = this.Bind<StructFindProductsArgs>();

            //StructFindRequestId id = FindProducts(args);

            //return Negotiate
            //    .WithStatusCode(HttpStatusCode.OK)
            //    .WithModel(id)
            //    .WithContentType("application/json");
            //};


            /////////////////////////////////
            Post["/v1/products/find"] = x =>
            {
            StructFindProductsArgs2 args = new StructFindProductsArgs2();

            StructFindRequestId id = new StructFindRequestId();
            id.result = new StructResult();

            try
                {
                args = this.Bind<StructFindProductsArgs2>();
                }
            catch
                {
                id.result.result_code = ResultCode.Failure_InvalidInputJson;
                id.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(id)
                    .WithContentType("application/json");
                }

            id = FindProducts(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(id)
                .WithContentType("application/json");
            };



            /////////////////////////////////
            Post["/v1/product_info/get"] = x =>
            {
            StructGetProductInfoArgs args = new StructGetProductInfoArgs();

            StructProducts products = new StructProducts();
            products.result = new StructResult();

            try
                {
                args = this.Bind<StructGetProductInfoArgs>();
                }
            catch
                {
                products.result.result_code = ResultCode.Failure_InvalidInputJson;
                products.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(products)
                    .WithContentType("application/json");
                }

            products = GetProductsInfo(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(products)
                .WithContentType("application/json");
            };




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

            conn = new NpgsqlConnection(strConnString);

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

            if(args.locale == null || args.locale.Length == 0)
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

                if(args.retailer_id_list == null || args.retailer_id_list.Count == 0)
                    {
                    strSQL = @"SELECT f.id_filter, f.name, f.description, f.type, f.multiselect, f.required, fi.id_filter_item, fi.item_value
                               FROM db_filters f
                               INNER JOIN db_filter_items fi ON f.id_filter = fi.id_filter
                               ORDER BY id_filter, id_filter_item;";
                    }
                else if(args.retailer_id_list.Count == 1)
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

                    for(int i = 1; i < args.retailer_id_list.Count; ++i)
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

                        if(nFilterId != nIdFilterPrev)
                            {
                            if(nIdFilterPrev != -1)
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

                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = @"INSERT INTO db_model_pictures DEFAULT VALUES RETURNING id_model_picture;";

                command.Prepare();

                Object obj = command.ExecuteScalar();

                id.model_picture_id = (int) obj;


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



//        //+///////////////////////////////////////////////////////////////////////
//        public StructFindRequestId FindProducts(StructFindProductsArgs args)
//            {
//            StructFindRequestId id = new StructFindRequestId();
//            id.result = new StructResult();
//            id.find_request_id = -1;
//            id.num_products_found = 0;

//            Stopwatch sw = new Stopwatch();
//            sw.Start();


//            if (args.locale == null || args.locale.Length == 0)
//                args.locale = "en";

//            if (!ServUtility.IsServiceAvailable(ref id.result, args.locale))
//                {
//                return id;
//                }


//            //int nIdUser = -1;
//            NpgsqlConnection conn = null;


//            try
//                {
//                if (!Connect(out conn, ref id.result))
//                    {
//                    return id;
//                    }

//                Dictionary<string, List<string>> dictFilterItems = new Dictionary<string, List<string>>();

//                foreach (StructFilterItemPair pair in args.filter_item_list)
//                    {
//                    if (dictFilterItems.ContainsKey(pair.name))
//                        {
//                        dictFilterItems[pair.name].Add(pair.value);
//                        }
//                    else
//                        {
//                        dictFilterItems.Add(pair.name, new List<string> { pair.value });
//                        }
//                    }


//                NpgsqlCommand command = new NpgsqlCommand();
//                command.Connection = conn;

//                string strSQL = @"SELECT id_product FROM db_products WHERE id_product IN(
//                SELECT inn.id_product FROM 
//                                        (
//                                        ";

//                string strSQL2 = @"
//                                   SELECT DISTINCT id_product FROM dbl_product_filter_items
//                                   WHERE id_filter_item IN 
//                                   (SELECT id_filter_item FROM db_filter_items WHERE 
//                                   (id_filter = (SELECT id_filter FROM db_filters WHERE ""name"" = '{0}') AND ({1})))";

//                string strSQL3 = @"item_value = '{0}'";

//                string strSQL4 = @") AS inn)
//                                   ";

//                foreach (var filter in dictFilterItems)
//                    {
//                    string strTmp = "";

//                    foreach (string item in filter.Value)
//                        {
//                        strTmp += String.Format(strSQL3, item) + " OR ";
//                        }

//                    strTmp = strTmp.Substring(0, strTmp.Length - 4);

//                    strTmp = String.Format(strSQL2, filter.Key, strTmp);

//                    strSQL += "\n" + strTmp + "\n INTERSECT" + "\n";
//                    }

//                strSQL = strSQL.Substring(0, strSQL.Length - 10) + strSQL4;

//                if (args.retailer_id_list != null && args.retailer_id_list.Count > 0)
//                    {
//                    string strSQL6 = @" AND id_retailer IN ({0})
//                                        ORDER BY id_product
//                                        LIMIT 200";

//                    string strSQL7 = "";

//                    strSQL7 = args.retailer_id_list[0].ToString();

//                    for (int i = 1; i < args.retailer_id_list.Count; ++i)
//                        {
//                        strSQL7 += ", " + args.retailer_id_list[i].ToString();
//                        }

//                    strSQL6 = String.Format(strSQL6, strSQL7);

//                    strSQL += "\n" + strSQL6 + "\n";
//                    }

//                command.CommandText = strSQL;

//                //id.sql = strSQL;

//                command.Prepare();

//                NpgsqlDataReader data = command.ExecuteReader();

//                List<int> lstFound = new List<int>();

//                if (data.HasRows)
//                    {
//                    while (data.Read())
//                        {
//                        int nId = data.GetInt32(0);

//                        lstFound.Add(nId);
//                        }
//                    }

//                id.num_products_found = lstFound.Count;

//                if(lstFound.Count > 0)
//                    {
//                    NpgsqlCommand commandInsert1 = new NpgsqlCommand();
//                    commandInsert1.Connection = conn;

//                    commandInsert1.CommandText = @"INSERT INTO db_search_requests DEFAULT VALUES RETURNING id_search_request;";

//                    commandInsert1.Prepare();

//                    Object obj = commandInsert1.ExecuteScalar();

//                    int nIdRequest = (int)obj;
//                    id.find_request_id = nIdRequest;


//                    string strSQLInsert = @"INSERT INTO db_search_results(id_search_request, id_product) VALUES ";

//                    strSQLInsert += "\n(" + nIdRequest.ToString() + ", " + lstFound[0].ToString() + ")";

//                    for(int i = 1; i < lstFound.Count; ++i)
//                        {
//                        strSQLInsert += ",\n" + "\n(" + nIdRequest.ToString() + ", " + lstFound[i].ToString() + ")";
//                        }

//                    commandInsert1.CommandText = strSQLInsert;

//                    commandInsert1.Prepare();

//                    commandInsert1.ExecuteNonQuery();
//                    }

//                return id;
//                }
//            catch (Exception e)
//                {
//                id.result.message = e.Message;
//                id.result.result_code = ResultCode.Failure_InternalServiceError;

//                return id;
//                }
//            finally
//                {
//                sw.Stop();

//                if (conn != null)
//                    {
//                    string strBody = ServUtility.GetRequestBody(); 
                    
//                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, id.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

//                    //if (events.result.result_code == ResultCode.Success)
//                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

//                    conn.Close();
//                    }
//                }
//            }


        
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

                if(!ServUtility.IsModelPictureValid(ref conn, args.picture_id))
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

                if(dictFilterItems.Count > 0)
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
                                        LIMIT 200";

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
                        strSQL = @"SELECT id_product FROM db_products WHERE id_retailer IN ({0}) ORDER BY id_product LIMIT 200;";

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
                        strSQL = @"SELECT id_product FROM db_products ORDER BY id_product LIMIT 200;";
                        }
                    }


                command.CommandText = strSQL;

                //id.sql = strSQL;

                command.Prepare();

                NpgsqlDataReader data = command.ExecuteReader();

                List<int> lstFound = new List<int>();

                if (data.HasRows)
                    {
                    while (data.Read())
                        {
                        int nId = data.GetInt32(0);

                        lstFound.Add(nId);
                        }
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


                    string strSQLInsert = @"INSERT INTO db_search_results(id_search_request, id_product) VALUES ";

                    strSQLInsert += "\n(" + nIdRequest.ToString() + ", " + lstFound[0].ToString() + ")";

                    for (int i = 1; i < lstFound.Count; ++i)
                        {
                        strSQLInsert += ",\n" + "\n(" + nIdRequest.ToString() + ", " + lstFound[i].ToString() + ")";
                        }

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
                command2.CommandText = "SELECT picture FROM db_product_pictures WHERE id_product = :idp;";
                command2.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
                command2.Prepare();


                NpgsqlCommand command = new NpgsqlCommand();
                command.Connection = conn;

                command.CommandText = @"SELECT p.id_product, p.name, p.retailprice * 100, p.buyurl, p.imageurl, p.id_retailer, pp.width, pp.height, pp.file_format, p.currency
                                    FROM db_search_results sr
                                    INNER JOIN db_products p ON sr.id_search_request = :isr AND p.id_product = sr.id_product 
                                    LEFT JOIN db_product_pictures pp ON pp.id_product = p.id_product
                                    ORDER BY sr.id_search_result
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

                        StructProduct product = new StructProduct();

                        product.id = data.GetInt32(0);
                        product.name = data.GetString(1);
                        product.price = data.GetDouble(2);
                        product.buyurl = data.GetString(3);
                        product.picture_remote = data.GetString(4);
                        product.retailer_id = data.GetInt32(5);
                        
                        if(!data.IsDBNull(6))
                            product.picture_width = data.GetInt32(6);

                        if (!data.IsDBNull(7))
                            product.picture_height = data.GetInt32(7);

                        string strFileFormat = "";
                        
                        if (!data.IsDBNull(8))
                            strFileFormat = data.GetString(8);

                        product.currency = data.GetString(9);


                        if (ServUtility.FileExists(strPicturesFolder + "photo_" + product.id.ToString() + "_1." + strFileFormat))
                            {
                            product.picture = strPicturesBaseUrl + "photo_" + product.id.ToString() + "_1." + strFileFormat;
                            }
                        else
                            {
                            command2.Parameters[0].Value = product.id;

                            NpgsqlDataReader data2 = command2.ExecuteReader();

                            if (data2.HasRows)
                                {
                                data2.Read();

                                byte[] arrPhoto = ServUtility.GetBinaryFieldValue(ref data2, 0);

                                if (arrPhoto != null && arrPhoto.GetLength(0) > 0)
                                    {
                                    string strPhotoUrl = strPicturesBaseUrl + "photo_" + product.id.ToString() + "_1." + strFileFormat;
                                    string strPhotoPath = strPicturesFolder + "photo_" + product.id.ToString() + "_1." + strFileFormat;

                                    if (!ServUtility.IdenticalFileExists(strPhotoPath, arrPhoto.GetLength(0)))
                                        File.WriteAllBytes(strPhotoPath, arrPhoto);

                                    product.picture = strPhotoUrl;
                                    }
                                }
                            }



                        products.products.Add(product);
                        }
                    }



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
                    
                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, products.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

                    //if (events.result.result_code == ResultCode.Success)
                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

                    conn.Close();
                    }
                }
            }





//        //+///////////////////////////////////////////////////////////////////////
//        public StructProducts GetProducts(StructGetProductsArgs args, string strBody)
//            {
//            StructProducts products = new StructProducts();
//            products.result = new StructResult();
//            products.products = new List<StructProduct>();

//            Stopwatch sw = new Stopwatch();
//            sw.Start();


//            if (!ServUtility.IsServiceAvailable(ref products.result, args.locale))
//                {
//                return products;
//                }


//            //int nIdUser = -1;
//            NpgsqlConnection conn = null;


//            try
//                {
//                if (!Connect(out conn, ref products.result))
//                    {
//                    return products;
//                    }

//                Dictionary<string, List<string>> dictFilterItems = new Dictionary<string, List<string>>();

//                foreach (StructFilterItemPair pair in args.filter_item_list)
//                    {
//                    if (dictFilterItems.ContainsKey(pair.name))
//                        {
//                        dictFilterItems[pair.name].Add(pair.value);
//                        }
//                    else
//                        {
//                        dictFilterItems.Add(pair.name, new List<string> { pair.value });
//                        }
//                    }


//                NpgsqlCommand command = new NpgsqlCommand();
//                command.Connection = conn;

//                string strSQL = @"SELECT p.id_product, p.name, p.retailprice, p.buyurl, p.imageurl, p.id_retailer FROM 
//                                        (
//                                        ";

//                string strSQL2 = @"
//                                   SELECT DISTINCT id_product FROM dbl_product_filter_items
//                                   WHERE id_filter_item IN 
//                                   (SELECT id_filter_item FROM db_filter_items WHERE 
//                                   (id_filter = (SELECT id_filter FROM db_filters WHERE ""name"" = '{0}') AND ({1})))";

//                string strSQL3 = @"item_value = '{0}'";

//                string strSQL4 = @") AS inn
//                                   LEFT JOIN db_products p ON p.id_product = inn.id_product
//                                   ";

//                string strSQL5 = @"
//                                  LIMIT 100;";

//                foreach(var filter in dictFilterItems)
//                    {
//                    string strTmp = "";

//                    foreach(string item in filter.Value)
//                        {
//                        strTmp += String.Format(strSQL3, item) + " OR ";
//                        }

//                    strTmp = strTmp.Substring(0, strTmp.Length - 4);

//                    strTmp = String.Format(strSQL2, filter.Key, strTmp);

//                    strSQL += "\n" + strTmp + "\n INTERSECT" + "\n";
//                    }

//                strSQL = strSQL.Substring(0, strSQL.Length - 10) + strSQL4;

//                if(args.retailer_id_list != null && args.retailer_id_list.Count > 0)
//                    {
//                    string strSQL6 = " AND p.id_retailer IN ({0})";

//                    string strSQL7 = "";

//                    strSQL7 = args.retailer_id_list[0].ToString();

//                    for(int i = 1; i < args.retailer_id_list.Count; ++i)
//                        {
//                        strSQL7 += ", " + args.retailer_id_list[i].ToString();
//                        }

//                    strSQL6 = String.Format(strSQL6, strSQL7);

//                    strSQL += "\n" + strSQL6 + "\n";
//                    }

//                strSQL += strSQL5;

//                command.CommandText = strSQL;

//                //products.sql = strSQL;

//                command.Prepare();

//                NpgsqlDataReader data = command.ExecuteReader();

//                if (data.HasRows)
//                    {
//                    while (data.Read())
//                        {

//                        StructProduct product = new StructProduct();

//                        product.id = data.GetInt32(0);
//                        product.name = data.GetString(1);
//                        product.price = data.GetDouble(2);
//                        product.buyurl = data.GetString(3);
//                        product.picture = data.GetString(4);
//                        product.retailer_id = data.GetInt32(5);

//                        products.products.Add(product);

//                        }
//                    }



//                return products;
//                }
//            catch (Exception e)
//                {
//                products.result.message = e.Message;
//                products.result.result_code = ResultCode.Failure_InternalServiceError;

//                return products;
//                }
//            finally
//                {
//                sw.Stop();

//                if (conn != null)
//                    {
//                    ServUtility.LogMethodCall(ref conn, -1, MethodBase.GetCurrentMethod().Name, products.result.ToString(), null, null, args.locale, "", (int)sw.ElapsedMilliseconds, strBody);

//                    //if (events.result.result_code == ResultCode.Success)
//                    //    ServUtility.UpdateStatData(ref conn, -2, "consume", strLocale, "", id_, null);

//                    conn.Close();
//                    }
//                }
//            }




        }
    }