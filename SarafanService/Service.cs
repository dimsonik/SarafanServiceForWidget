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

namespace SarafanServiceForWidget
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
            Post["/v1/math/test"] = x =>
            {
            string res = TestMath();

            return Negotiate
                .WithStatusCode(HttpStatusCode.OK)
                .WithModel(res)
                .WithContentType("application/json");
            };


            /////////////////////////////////
            Post["/v1/db/test"] = x =>
            {
                string res = TestDB();

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
            Post["/v1/offers/get"] = x =>
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

                //args.id_retailer = 8; // временно

                id = FindSimiliarProducts(args);

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(id)
                    .WithContentType("application/json");
            };




        }


        }
    }