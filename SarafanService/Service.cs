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

    public class MyBootstrapper : Nancy.DefaultNancyBootstrapper
        {
        protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
            {
            base.ApplicationStartup(container, pipelines);
            
            Nancy.Json.JsonSettings.MaxJsonLength = int.MaxValue;

            //pipelines.AfterRequest += AddHeader;
            }

        //private void AddHeader(NancyContext ctx)
        //    {
        //    }
        }


    public partial class ServiceModule : NancyModule
        {
        [DllImport("libfftwf", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr fftwf_color_gist_scaletab(int width, int height, float[] r, float[] g, float[] b);
        public static float[] fftwf_color_gist_scaletab_(int width, int height, float[] r, float[] g, float[] b)
            {
            float[] ReturnArray = new float[960];
            Marshal.Copy(fftwf_color_gist_scaletab(width, height, r, g, b), ReturnArray, 0, 960);
            return ReturnArray;
            }

        [DllImport("libfftwf", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr fftwf_bw_gist_scaletab(int width, int height, float[] c);
        public static float[] fftwf_bw_gist_scaletab_(int width, int height, float[] c)
            {
            float[] ReturnArray = new float[320];
            Marshal.Copy(fftwf_bw_gist_scaletab(width, height, c), ReturnArray, 0, 320);
            return ReturnArray;
            }

        [DllImport("libopencv_nonfree", EntryPoint = "calcBOWDescriptors", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr calcBOWDescriptors(byte[] buffer, int nBuffSize, string vocabFilePath);
        public static float[] calcBOWDescriptors_(byte[] buffer, int nBuffSize, string vocabFilePath)
            {
            float[] ReturnArray = new float[2000];
            Marshal.Copy(calcBOWDescriptors(buffer, nBuffSize, vocabFilePath), ReturnArray, 0, 2000);
            return ReturnArray;
            }

        //[DllImport("libopencv_core", CallingConvention = CallingConvention.Cdecl)]
        //public static extern void cvReleaseImage(ref IntPtr image);


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
            Post["/v1/opencv/test"] = x =>
            {
            StructResult res = new StructResult();

            res = TestOpenCV();

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(res)
                .WithContentType("application/json");
            };


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
            Post["/v1/descriptors/reset"] = x =>
            {
            StructResult res = new StructResult();

            res = ResetDescriptors();

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(res)
                .WithContentType("application/json");
            };


            /////////////////////////////////
            Post["/v1/pics/compare"] = x =>
            {
            StructComparePicturesArgs args = new StructComparePicturesArgs();

            StructPicturesCompared res = new StructPicturesCompared();
            res.result = new StructResult();

            try
                {
                args = this.Bind<StructComparePicturesArgs>();
                }
            catch
                {
                res.result.result_code = ResultCode.Failure_InvalidInputJson;
                res.result.message = "Invalid input JSON";

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(res)
                    .WithContentType("application/json");
                }

            res = ComparePictures(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(res)
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


            /////////////////////////////////
            Post["/v1/model_picture/get"] = x =>
            {
            StructGetModelPictureArgs args = new StructGetModelPictureArgs();

            StructModelPicture id = new StructModelPicture();
            id.result = new StructResult();

            try
                {
                args = this.Bind<StructGetModelPictureArgs>();
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

            id = GetModelPicture(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(id)
                .WithContentType("application/json");
            };


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

            // временная затычка (в клиенте жестко прписан магазин)
            if (args.retailer_id_list != null && args.retailer_id_list.Count == 1 && args.retailer_id_list[0] == 6)
                args.retailer_id_list[0] = 7;

            id = FindProductsNew(args);
            //id = FindProducts(args);

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(id)
                .WithContentType("application/json");
            };



            /////////////////////////////////
            Post["/v1/products/find_test"] = x =>
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

                // временная затычка (в клиенте жестко прписан магазин)
                if (args.retailer_id_list != null && args.retailer_id_list.Count == 1 && args.retailer_id_list[0] == 6)
                    args.retailer_id_list[0] = 7;

                id = FindProductsNew(args);
                //id = FindProducts2(args);

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
            products.products = new List<StructProduct>();
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
            
            
            /////////////////////////////////
            Post["/v1/similiar_products/find"] = x =>
            {
            StructFindSimiliarProductsArgs args = new StructFindSimiliarProductsArgs();

            StructFindSimiliarResult id = new StructFindSimiliarResult();
                id.result = new StructResult();

                try
                    {
                    args = this.Bind<StructFindSimiliarProductsArgs>();
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

                args.id_retailer = 8; // временно

                id = FindSimiliarProducts(args);

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(id)
                    .WithContentType("application/json");
            };

        }







        }
    }