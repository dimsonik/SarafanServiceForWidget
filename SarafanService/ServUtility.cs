using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Services;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using Npgsql;
using NpgsqlTypes;
using geocode.gapi.cdpisb;
//using Newtonsoft.Json;

using System.Linq;

using OpenCvSharp;

//using MyMediaLite.Data;
//using MyMediaLite.Eval;
//using MyMediaLite.IO;
//using MyMediaLite.ItemRecommendation;

//MemoryStream ms = new MemoryStream(b);
//Image bottom_bar = Image.FromStream(ms);


 //string [] fileEntries = Directory.GetFiles(targetDirectory);

namespace SarafanServiceForWidget
{

	public class ServUtility
	{
		public ServUtility ()
		{
		}


        //////////////////////////////////////////////////////////////////////////
        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	    {
	    return true;
	    }


        ////////////////////////////////////////////////////////////////////////////
        public static string GetRequestBody()
        {
        Stream str = HttpContext.Current.Request.InputStream;
        long nL = str.Length;

        byte[] buff = new byte[nL];

        str.Seek(0, SeekOrigin.Begin);
        str.Read(buff, 0, (int)nL);

        string s = Encoding.UTF8.GetString(buff, 0, buff.Length);

        return s;
        }


        //////////////////////////////////////////////////////////////////////////
        public static bool IsSearchRequestValid(ref NpgsqlConnection conn, int nIdSearchRequest)
        {
        NpgsqlCommand commandExists = new NpgsqlCommand();
        commandExists.Connection = conn;

        commandExists.CommandText = @"SELECT id_search_request FROM db_search_requests WHERE id_search_request = :isr;";

        commandExists.Parameters.Add(new NpgsqlParameter("isr", DbType.Int32));

        commandExists.Prepare();

        commandExists.Parameters[0].Value = nIdSearchRequest;

        NpgsqlDataReader dataExists = commandExists.ExecuteReader();

        if (dataExists.HasRows)
            return true;
        else
            return false;
        }


        //////////////////////////////////////////////////////////////////////////
        public static bool IsModelPictureValid(ref NpgsqlConnection conn, int nIdModelPicture)
        {
        NpgsqlCommand commandExists = new NpgsqlCommand();
        commandExists.Connection = conn;

        commandExists.CommandText = @"SELECT id_model_picture FROM db_model_pictures WHERE id_model_picture = :imp;";

        commandExists.Parameters.Add(new NpgsqlParameter("imp", DbType.Int32));

        commandExists.Prepare();

        commandExists.Parameters[0].Value = nIdModelPicture;

        NpgsqlDataReader dataExists = commandExists.ExecuteReader();

        if (dataExists.HasRows)
            return true;
        else
            return false;
        }



        //////////////////////////////////////////////////////////////////////////
        public static int GetCountryForIPAddress(ref NpgsqlConnection conn, string strIP)
        {
		int nIdCountry = -1;

		int nIP = ServUtility.IPAddressToInt(strIP);


        NpgsqlCommand command = new NpgsqlCommand();
        command.Connection = conn;

        command.CommandText = @"SELECT id_country FROM db_ip_ranges WHERE ip_num_1 = (SELECT MAX(ip_num_1) FROM db_ip_ranges WHERE ip_num_1 <= :ipn);";

        command.Parameters.Add(new NpgsqlParameter("ipn", DbType.Int32));

        command.Prepare();

        command.Parameters[0].Value = nIP;

        NpgsqlDataReader data = command.ExecuteReader();

        if (data.HasRows)
			{
             data.Read();
             nIdCountry = data.GetInt32(0);
			}

		return nIdCountry;
        }




        //////////////////////////////////////////////////////////////////////////
        public static void DeleteOutdatedSearchRequests(ref NpgsqlConnection conn)
        {
        NpgsqlCommand commandDeleteMarked = new NpgsqlCommand();
        commandDeleteMarked.Connection = conn;
        commandDeleteMarked.CommandText = "DELETE FROM db_search_requests WHERE current_timestamp >= (\"timestamp\" + INTERVAL '15' DAY);";
        commandDeleteMarked.Prepare();

        commandDeleteMarked.ExecuteNonQuery();
        }


        //////////////////////////////////////////////////////////////////////////
        public static void DeleteOutdatedModelPictures(ref NpgsqlConnection conn)
        {
        NpgsqlCommand commandDeleteMarked = new NpgsqlCommand();
        commandDeleteMarked.Connection = conn;
        commandDeleteMarked.CommandText = "DELETE FROM db_model_pictures WHERE current_timestamp >= (\"timestamp\" + INTERVAL '15' DAY);";
        commandDeleteMarked.Prepare();

        commandDeleteMarked.ExecuteNonQuery();
        }


		//////////////////////////////////////////////////////////////////////////
		public static string[] SplitAndClearPhrase(string strInput)
		{
		string[] arrWords =	strInput.Split(new char[] {' ', '.','?', ',', '\n', '!', ';', ':'}, StringSplitOptions.RemoveEmptyEntries);
		
		//foreach(string word in arrWords)
		for(int i = 0; i < arrWords.GetLength(0); ++i)
			{
			arrWords[i].Trim();
			arrWords[i] = arrWords[i].Replace("\"", "");
			arrWords[i] = arrWords[i].Replace("'", "");
			arrWords[i] = arrWords[i].Replace("<", "");
			arrWords[i] = arrWords[i].Replace(">", "");
			arrWords[i] = arrWords[i].Replace("«", "");
			arrWords[i] = arrWords[i].Replace("»", "");
			}
		
		return arrWords;
		}


		//////////////////////////////////////////////////////////////////////////
		public static int CountWordsInPhrase(string strInput)
		{
		string[] arrWords =	strInput.Split(new char[] {' ', '.','?', ',', '\n', '!', ';', ':'}, StringSplitOptions.RemoveEmptyEntries);
		
		return arrWords.GetLength(0);
		}

		
		
		//////////////////////////////////////////////////////////////////////////
		public static ArrayList SplitTsVectorResult(string str)
		{
		ArrayList arrRes = new ArrayList();
		
		string pat = @"'([^']+)'";
		
		Regex r = new Regex(pat, RegexOptions.IgnoreCase);
		Match m = r.Match(str);
			
		while (m.Success) 
			{
			arrRes.Add(m.Groups[1].ToString());
			m = m.NextMatch();
			}
		
		return arrRes;	
		}
		
		
		//////////////////////////////////////////////////////////////////////////
		public static int IPAddressToInt(string strIP)
		{
        List<String> listIpElements;
        listIpElements = strIP.Split('.').ToList(); ;

        int ip3 = System.Convert.ToInt32(listIpElements[0]);
        int ip2 = System.Convert.ToInt32(listIpElements[1]);
        int ip1 = System.Convert.ToInt32(listIpElements[2]);
        int ip0 = System.Convert.ToInt32(listIpElements[3]);

        int ip_int = ip3 * (int) Math.Pow(256, 3) + ip2 * (int) Math.Pow(256, 2) + ip1 * (int) Math.Pow(256, 1) + ip0 * (int) Math.Pow(256, 0);

		return ip_int;
		}

		//////////////////////////////////////////////////////////////////////////
		public static string JoinArrayList(ArrayList arr, string strSep)
		{
        string strRes = "";

        if(arr.Count == 0)
            return strRes;
        else if(arr.Count == 1)
            return (string) arr[0];
        else
            {
            strRes = (string) arr[0];

            for(int i = 1; i < arr.Count; ++i)
                strRes += strSep + (string) arr[i];
            }
		
		return strRes;	
		}


        /////////////////////////////////////////////////////////////
        public static byte[] GetFloatArrayAsByteArray(float[] values)
        {
        var result = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);
        return result;
        }


        /////////////////////////////////////////////////////////////
        public static float[] GetByteArrayAsFloatArray(byte[] bytes)
        {
        var result = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        return result;
        }


        /////////////////////////////////////////////////////////////
        public static byte[] GetDoubleArrayAsByteArray(double[] values)
        {
        var result = new byte[values.Length * sizeof(double)];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);
        return result;
        }


        /////////////////////////////////////////////////////////////
        static double[] GetByteArrayAsDoubleArray(byte[] bytes)
        {
        var result = new double[bytes.Length / sizeof(double)];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        return result;
        }


        /////////////////////////////////////////////////////////////
        public static byte[] GetDomColorsAsByteArray(List<KeyValuePair<double, CvScalar>> dom)
            {
            List<double> lst_dom = new List<double>();

            foreach (KeyValuePair<double, CvScalar> pair in dom)
                {
                lst_dom.Add(pair.Key);
                lst_dom.Add(pair.Value.Val0);
                lst_dom.Add(pair.Value.Val1);
                lst_dom.Add(pair.Value.Val2);
                lst_dom.Add(pair.Value.Val3);
                }

            double[] arr_dom = lst_dom.ToArray();

            byte[] byte_dom = GetDoubleArrayAsByteArray(arr_dom);

            return byte_dom;
            }


        /////////////////////////////////////////////////////////////
        public static List<KeyValuePair<double, CvScalar>> GetByteArrayAsDomColors(byte[] bytes)
            {
            double[] dblArr = GetByteArrayAsDoubleArray(bytes);

            int nRows = dblArr.Length / 5;

            List<KeyValuePair<double, CvScalar>> result = new List<KeyValuePair<double, CvScalar>>();

            for (int i = 0; i < nRows; ++i)
                {
                double dblKey = dblArr[i * 5];

                double dbl1 = dblArr[i * 5 + 1];
                double dbl2 = dblArr[i * 5 + 2];
                double dbl3 = dblArr[i * 5 + 3];
                double dbl4 = dblArr[i * 5 + 4];

                CvScalar sc = new CvScalar(dbl1, dbl2, dbl3, dbl4);

                KeyValuePair<double, CvScalar> row = new KeyValuePair<double, CvScalar>(dblKey, sc);

                result.Add(row);
                }

            return result;
            }
        

        /////////////////////////////////////////////////////////////
        public static double rgb_euclidean(CvScalar p1, CvScalar p2)
            {
            double val = (double)Math.Sqrt((p1.Val0 - p2.Val0) * (p1.Val0 - p2.Val0) +
                                            (p1.Val1 - p2.Val1) * (p1.Val1 - p2.Val1) +
                                            (p1.Val2 - p2.Val2) * (p1.Val2 - p2.Val2) +
                                            (p1.Val3 - p2.Val3) * (p1.Val3 - p2.Val3));

            return val;
            }


        /////////////////////////////////////////////////////////////
        public static CvScalar RGB2LAB(CvScalar rgb)
            {
            double r = (rgb.Val0 / 255), g = (rgb.Val1 / 255), b = (rgb.Val2 / 255);

            if (r > 0.04045) r = Math.Pow(((r + 0.055) / 1.055), 2.4); else r = r / 12.92;
            if (g > 0.04045) g = Math.Pow(((g + 0.055) / 1.055), 2.4); else g = g / 12.92;
            if (b > 0.04045) b = Math.Pow(((b + 0.055) / 1.055), 2.4); else b = b / 12.92;

            r = r * 100;
            g = g * 100;
            b = b * 100;

            double x = r * 0.4124 + g * 0.3576 + b * 0.1805;
            double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double z = r * 0.0193 + g * 0.1192 + b * 0.9505;

            x = x / 95.047;
            y = y / 100.000;
            z = z / 108.883;

            if (x > 0.008856) x = Math.Pow(x, (1.0 / 3.0)); else x = (7.787 * x) + (16.0 / 116.0);
            if (y > 0.008856) y = Math.Pow(y, (1.0 / 3.0)); else y = (7.787 * y) + (16.0 / 116.0);
            if (z > 0.008856) z = Math.Pow(z, (1.0 / 3.0)); else z = (7.787 * z) + (16.0 / 116.0);

            double cie_L = (116 * y) - 16;
            double cie_a = 500 * (x - y);
            double cie_b = 200 * (y - z);

            CvScalar res = new CvScalar();
            res.Val0 = cie_L; res.Val1 = cie_a; res.Val2 = cie_b;

            return res;
            }

        /////////////////////////////////////////////////////////////
        public static double colorDistance(CvScalar c1, CvScalar c2)
            {
            double d = 0;

            d += (c1.Val0 - c2.Val0) * (c1.Val0 - c2.Val0);
            d += (c1.Val1 - c2.Val1) * (c1.Val1 - c2.Val1);
            d += (c1.Val2 - c2.Val2) * (c1.Val2 - c2.Val2);

            return Math.Sqrt(d);
            }


        /////////////////////////////////////////////////////////////
        public static uint cvRandInt(CvRNG rng)
            {
            ulong temp = rng.Seed;
            temp = (ulong)(uint)temp * 4164903690U + (temp >> 32);
            rng.Seed = temp;

            return (uint)temp;
            }

        struct ColorCluster
            {
            public CvScalar color;
            public CvScalar new_color;
            public int count;
            };

        public struct CCDist
            {
            public int c1;
            public int c2;
            public double dist;
            }

        public struct StructColors
            {
            public List<KeyValuePair<double, CvScalar>> colors;
            }

        
        /////////////////////////////////////////////////////////////
        public static double calcColorDistance(List<KeyValuePair<double, CvScalar>> colors1_, List<KeyValuePair<double, CvScalar>> colors2_)
            {
            List<KeyValuePair<double, CvScalar>> colors1 = new List<KeyValuePair<double, CvScalar>>();
            List<KeyValuePair<double, CvScalar>> colors2 = new List<KeyValuePair<double, CvScalar>>();

            foreach (KeyValuePair<double, CvScalar> p in colors1_)
                {
                colors1.Add(p);
                }

            foreach (KeyValuePair<double, CvScalar> p in colors2_)
                {
                colors2.Add(p);
                }

            double value = 0;

            List<CCDist> dists = new List<CCDist>();

            for (int i = 0; i < colors1.Count; ++i)
                {
                for (int j = 0; j < colors2.Count; ++j)
                    {
                    CCDist dist = new CCDist();
                    dist.c1 = i;
                    dist.c2 = j;

                    dist.dist = colorDistance(RGB2LAB(colors1[i].Value), RGB2LAB(colors2[j].Value));

                    dists.Add(dist);
                    }
                }

            dists.Sort(delegate(CCDist p1, CCDist p2)
                {
                return p1.dist.CompareTo(p2.dist);
                }
                );


            int c1, c2;

            for (int i = 0; i < dists.Count; ++i)
                {
                c1 = dists[i].c1; c2 = dists[i].c2;

                if ((colors1[c1].Key > 0) && (colors2[c2].Key > 0))
                    {
                    if (colors1[c1].Key > colors2[c2].Key)
                        {
                        KeyValuePair<double, CvScalar> new_pair = new KeyValuePair<double, CvScalar>(colors1[c1].Key - colors2[c2].Key, colors1[c1].Value);
                        colors1[c1] = new_pair;

                        value += dists[i].dist * colors2[c2].Key;

                        KeyValuePair<double, CvScalar> new_pair2 = new KeyValuePair<double, CvScalar>(0, colors2[c2].Value);
                        colors2[c2] = new_pair2;
                        }
                    else
                        {
                        KeyValuePair<double, CvScalar> new_pair = new KeyValuePair<double, CvScalar>(colors2[c2].Key - colors1[c1].Key, colors2[c2].Value);
                        colors2[c2] = new_pair;

                        value += dists[i].dist * colors1[c1].Key;

                        KeyValuePair<double, CvScalar> new_pair2 = new KeyValuePair<double, CvScalar>(0, colors1[c1].Value);
                        colors1[c1] = new_pair2;
                        }
                    }
                }

            return value;
            }


        /////////////////////////////////////////////////////////////
        public static double calcColorDistanceSimplified(List<KeyValuePair<double, CvScalar>> colors1_, List<KeyValuePair<double, CvScalar>> colors2_)
            {
            double value = (colors1_[0].Value.Val0 - colors2_[0].Value.Val0) * (colors1_[0].Value.Val0 - colors2_[0].Value.Val0)
                         + (colors1_[0].Value.Val1 - colors2_[0].Value.Val1) * (colors1_[0].Value.Val1 - colors2_[0].Value.Val1)
                         + (colors1_[0].Value.Val2 - colors2_[0].Value.Val2) * (colors1_[0].Value.Val2 - colors2_[0].Value.Val2);

            value = Math.Sqrt(value);

            return value;
            }


        
        
        /////////////////////////////////////////////////////////////
        public static List<KeyValuePair<double, CvScalar>> GetDomColors(string strFilename, int nX, int nY, int nWidth, int nHeight)
        {
        byte[] fl = File.ReadAllBytes(strFilename);
        MemoryStream ms = new MemoryStream(fl);
        IplImage image = IplImage.FromStream(ms, LoadMode.Color);

        Cv.SetImageROI(image, new CvRect(nX, nY, nWidth, nHeight));

        IplImage tmp_ = new IplImage(Cv.GetSize(image), image.Depth, image.NChannels);

        Cv.Copy(image, tmp_, null);
        Cv.ResetImageROI(image);

        image = Cv.CloneImage(tmp_);
        Cv.ReleaseImage(tmp_);

        tmp_ = new IplImage(new CvSize(228, 270), image.Depth, image.NChannels);

        Cv.Resize(image, tmp_);
        Cv.ReleaseImage(image);

        image = new IplImage(new CvSize(228, 270), tmp_.Depth, tmp_.NChannels);

        image = Cv.CloneImage(tmp_);
        Cv.ReleaseImage(tmp_);



        Cv.SetImageROI(image, new CvRect(image.Width / 4, image.Height / 5, image.Width / 2, image.Height * 7 / 10));

        IplImage tmp = new IplImage(Cv.GetSize(image), image.Depth, image.NChannels);

        Cv.Copy(image, tmp, null);

        Cv.ResetImageROI(image);

        image = Cv.CloneImage(tmp);

        Cv.ReleaseImage(tmp);

        float ratio = image.Width * 1.0f / image.Height;

        IplImage src;

        if (ratio > 1)
            {
            src = new IplImage(new CvSize(100, (int)(100 / ratio)), BitDepth.U8, 3);
            }
        else
            {
            src = new IplImage(new CvSize((int)(100 * ratio), 100), BitDepth.U8, 3);
            }

        Cv.Resize(image, src, Interpolation.Linear);

        IplImage cluster_indexes = new IplImage(Cv.GetSize(src), BitDepth.U8, 1);

        Cv.Zero(cluster_indexes);

        int cluster_count = 10;

        ColorCluster[] clusters = new ColorCluster[cluster_count];

        int i = 0,k = 0, x = 0, y = 0;

        CvRNG rng = Cv.RNG();

        for (k = 0; k < cluster_count; k++)
            {
            uint n1 = cvRandInt(rng);
            uint n2 = cvRandInt(rng);
            uint n3 = cvRandInt(rng);

            clusters[k].new_color = new CvScalar(n3 % 255, n2 % 255, n1 % 255, 0);
            }

        double min_rgb_euclidean = 0, old_rgb_euclidean = 0;

        while (true)
            {
            for (k = 0; k < cluster_count; k++)
                {
                clusters[k].count = 0;
                clusters[k].color = clusters[k].new_color;
                clusters[k].new_color = Cv.ScalarAll(0);
                }

            for (y = 0; y < src.Height; y++)
                {
                for (x = 0; x < src.Width; x++)
                    {
                    CvColor c = src[y, x];

                    byte R = c.R;
                    byte G = c.G;
                    byte B = c.B;

                    min_rgb_euclidean = 255 * 255 * 255;

                    int cluster_index = -1;

                    for (k = 0; k < cluster_count; k++)
                        {
                        double euclid = rgb_euclidean(new CvScalar(B, G, R, 0), clusters[k].color);

                        if (euclid < min_rgb_euclidean)
                            {
                            min_rgb_euclidean = euclid;
                            cluster_index = k;
                            }
                        }

                    CvColor c2 = cluster_indexes[y, x];

                    cluster_indexes[y, x] = new CvColor()
                        {
                        B = (byte)cluster_index,
                        G = (byte)c2.G,
                        R = (byte)c2.R,
                        };

                    clusters[cluster_index].count++;
                    clusters[cluster_index].new_color.Val0 += B;
                    clusters[cluster_index].new_color.Val1 += G;
                    clusters[cluster_index].new_color.Val2 += R;
                    }
                }

            min_rgb_euclidean = 0;

            for (k = 0; k < cluster_count; k++)
                {
                clusters[k].new_color.Val0 /= clusters[k].count;
                clusters[k].new_color.Val1 /= clusters[k].count;
                clusters[k].new_color.Val2 /= clusters[k].count;

                double ecli = rgb_euclidean(clusters[k].new_color, clusters[k].color);

                if (ecli > min_rgb_euclidean)
                    min_rgb_euclidean = ecli;
                }

            if (Math.Abs(min_rgb_euclidean - old_rgb_euclidean) < 1)
                break;

            old_rgb_euclidean = min_rgb_euclidean;
            }

        List<KeyValuePair<int, uint>> colors = new List<KeyValuePair<int, uint>>();
        colors.Capacity = cluster_count;

        int colors_count = 0;

        for (i = 0; i < cluster_count; i++)
            {
            KeyValuePair<int, uint> color = new KeyValuePair<int, uint>(i, (uint)clusters[i].count);

            colors.Add(color);

            if (clusters[i].count > 0)
                colors_count++;
            }


        colors.Sort(delegate(KeyValuePair<int, uint> p1, KeyValuePair<int, uint> p2)
        {
            return p2.Value.CompareTo(p1.Value);
        }
            );

        List<KeyValuePair<double, CvScalar>> result = new List<KeyValuePair<double, CvScalar>>();

        int sum = 0;

        for (int ii = 0; ii < colors.Count; ++ii)
            {
            sum += (int)colors[ii].Value;
            }

        for (int ii = 0; ii < colors.Count; ++ii)
            {
            if (colors[ii].Value > 0)
                {
                KeyValuePair<double, CvScalar> pair = new KeyValuePair<double, CvScalar>(colors[ii].Value * 1.0f / sum, clusters[colors[ii].Key].color);

                result.Add(pair);
                }
            else
                {
                break;
                }
            }

        Cv.ReleaseImage(image);
        Cv.ReleaseImage(src);
        Cv.ReleaseImage(cluster_indexes);
        Cv.ReleaseImage(tmp);


        return result;
        }

        /////////////////////////////////////////////////////////////
        public static List<KeyValuePair<double, CvScalar>> ReduceDomColors(List<KeyValuePair<double, CvScalar>> lstInput)
        {
        List<KeyValuePair<double, CvScalar>> lstReduced = new List<KeyValuePair<double, CvScalar>>();

        double dbl0 = 0.0, dbl1 = 0.0, dbl2 = 0.0, dbl3 = 0.0;

        foreach(KeyValuePair<double, CvScalar> row in lstInput)
            {
            dbl0 += row.Key * row.Value.Val0;
            dbl1 += row.Key * row.Value.Val1;
            dbl2 += row.Key * row.Value.Val2;
            dbl3 += row.Key * row.Value.Val3;
            }

        KeyValuePair<double, CvScalar> rw = new KeyValuePair<double, CvScalar>(1.0, new CvScalar(dbl0, dbl1, dbl2, dbl3));
        
        lstReduced.Add(rw); 

        return lstReduced;
        }
        
        
        
        /////////////////////////////////////////////////////////////
        public static List<KeyValuePair<double, CvScalar>> GetDomColors(byte[] pic)
            {
            //byte[] fl = File.ReadAllBytes(strFilename);
            MemoryStream ms = new MemoryStream(pic);
            IplImage image = IplImage.FromStream(ms, LoadMode.Color);

            //Cv.SetImageROI(image, new CvRect(nX, nY, nWidth, nHeight));

            //IplImage tmp_ = new IplImage(Cv.GetSize(image), image.Depth, image.NChannels);

            //Cv.Copy(image, tmp_, null);
            //Cv.ResetImageROI(image);

            //image = Cv.CloneImage(tmp_);
            //Cv.ReleaseImage(tmp_);

            IplImage tmp_ = new IplImage(new CvSize(228, 270), image.Depth, image.NChannels);

            Cv.Resize(image, tmp_);
            Cv.ReleaseImage(image);

            image = new IplImage(new CvSize(228, 270), tmp_.Depth, tmp_.NChannels);

            image = Cv.CloneImage(tmp_);
            Cv.ReleaseImage(tmp_);



            Cv.SetImageROI(image, new CvRect(image.Width / 4, image.Height / 5, image.Width / 2, image.Height * 7 / 10));

            IplImage tmp = new IplImage(Cv.GetSize(image), image.Depth, image.NChannels);

            Cv.Copy(image, tmp, null);

            Cv.ResetImageROI(image);

            image = Cv.CloneImage(tmp);

            Cv.ReleaseImage(tmp);

            float ratio = image.Width * 1.0f / image.Height;

            IplImage src;

            if (ratio > 1)
                {
                src = new IplImage(new CvSize(100, (int)(100 / ratio)), BitDepth.U8, 3);
                }
            else
                {
                src = new IplImage(new CvSize((int)(100 * ratio), 100), BitDepth.U8, 3);
                }

            Cv.Resize(image, src, Interpolation.Linear);

            IplImage cluster_indexes = new IplImage(Cv.GetSize(src), BitDepth.U8, 1);

            Cv.Zero(cluster_indexes);

            int cluster_count = 10;

            ColorCluster[] clusters = new ColorCluster[cluster_count];

            int i = 0,k = 0, x = 0, y = 0;

            CvRNG rng = Cv.RNG();

            for (k = 0; k < cluster_count; k++)
                {
                uint n1 = cvRandInt(rng);
                uint n2 = cvRandInt(rng);
                uint n3 = cvRandInt(rng);

                clusters[k].new_color = new CvScalar(n3 % 255, n2 % 255, n1 % 255, 0);
                }

            double min_rgb_euclidean = 0, old_rgb_euclidean = 0;

            while (true)
                {
                for (k = 0; k < cluster_count; k++)
                    {
                    clusters[k].count = 0;
                    clusters[k].color = clusters[k].new_color;
                    clusters[k].new_color = Cv.ScalarAll(0);
                    }

                for (y = 0; y < src.Height; y++)
                    {
                    for (x = 0; x < src.Width; x++)
                        {
                        CvColor c = src[y, x];

                        byte R = c.R;
                        byte G = c.G;
                        byte B = c.B;

                        min_rgb_euclidean = 255 * 255 * 255;

                        int cluster_index = -1;

                        for (k = 0; k < cluster_count; k++)
                            {
                            double euclid = rgb_euclidean(new CvScalar(B, G, R, 0), clusters[k].color);

                            if (euclid < min_rgb_euclidean)
                                {
                                min_rgb_euclidean = euclid;
                                cluster_index = k;
                                }
                            }

                        CvColor c2 = cluster_indexes[y, x];

                        cluster_indexes[y, x] = new CvColor()
                        {
                            B = (byte)cluster_index,
                            G = (byte)c2.G,
                            R = (byte)c2.R,
                        };

                        clusters[cluster_index].count++;
                        clusters[cluster_index].new_color.Val0 += B;
                        clusters[cluster_index].new_color.Val1 += G;
                        clusters[cluster_index].new_color.Val2 += R;
                        }
                    }

                min_rgb_euclidean = 0;

                for (k = 0; k < cluster_count; k++)
                    {
                    clusters[k].new_color.Val0 /= clusters[k].count;
                    clusters[k].new_color.Val1 /= clusters[k].count;
                    clusters[k].new_color.Val2 /= clusters[k].count;

                    double ecli = rgb_euclidean(clusters[k].new_color, clusters[k].color);

                    if (ecli > min_rgb_euclidean)
                        min_rgb_euclidean = ecli;
                    }

                if (Math.Abs(min_rgb_euclidean - old_rgb_euclidean) < 1)
                    break;

                old_rgb_euclidean = min_rgb_euclidean;
                }

            List<KeyValuePair<int, uint>> colors = new List<KeyValuePair<int, uint>>();
            colors.Capacity = cluster_count;

            int colors_count = 0;

            for (i = 0; i < cluster_count; i++)
                {
                KeyValuePair<int, uint> color = new KeyValuePair<int, uint>(i, (uint)clusters[i].count);

                colors.Add(color);

                if (clusters[i].count > 0)
                    colors_count++;
                }


            colors.Sort(delegate(KeyValuePair<int, uint> p1, KeyValuePair<int, uint> p2)
            {
                return p2.Value.CompareTo(p1.Value);
            }
                );

            List<KeyValuePair<double, CvScalar>> result = new List<KeyValuePair<double, CvScalar>>();

            int sum = 0;

            for (int ii = 0; ii < colors.Count; ++ii)
                {
                sum += (int)colors[ii].Value;
                }

            for (int ii = 0; ii < colors.Count; ++ii)
                {
                if (colors[ii].Value > 0)
                    {
                    KeyValuePair<double, CvScalar> pair = new KeyValuePair<double, CvScalar>(colors[ii].Value * 1.0f / sum, clusters[colors[ii].Key].color);

                    result.Add(pair);
                    }
                else
                    {
                    break;
                    }
                }

            Cv.ReleaseImage(image);
            Cv.ReleaseImage(src);
            Cv.ReleaseImage(cluster_indexes);
            Cv.ReleaseImage(tmp);


            return result;
            }




        /////////////////////////////////////////////////////////////
        public static StructColorImage LoadImage(Image img_orig)
            {
            Image img = ServUtility.FixedSize(img_orig, 256, 256);

            Bitmap bmp = new Bitmap(img);

            StructColorImage ci = new StructColorImage();

            ci.width = img.Width;
            ci.height = img.Height;

            ci.c1 = new float[ci.width * ci.height];
            ci.c2 = new float[ci.width * ci.height];
            ci.c3 = new float[ci.width * ci.height];

            for (int x = 0; x < ci.width; ++x)
                {
                for (int y = 0; y < ci.width; ++y)
                    {
                    int n = y * ci.width + x;

                    ci.c1[n] = bmp.GetPixel(x, y).R;
                    ci.c2[n] = bmp.GetPixel(x, y).G;
                    ci.c3[n] = bmp.GetPixel(x, y).B;
                    }
                }

            return ci;
            }



        /////////////////////////////////////////////////////////////
        public static StructBWImage LoadBWImage(Image img_orig)
            {
            //Image img = ServUtility.FixedSize(img_orig, 256, 256);
            Image img = ServUtility.FixedSize(img_orig, 228, 270);

            Bitmap bmp = new Bitmap(img);

            StructBWImage ci = new StructBWImage();

            ci.width = img.Width;
            ci.height = img.Height;

            ci.c1 = new float[ci.width * ci.height];

            for (int x = 0; x < ci.width; ++x)
                {
                for (int y = 0; y < ci.width; ++y)
                    {
                    int n = y * ci.width + x;

                    ci.c1[n] = (bmp.GetPixel(x, y).R + bmp.GetPixel(x, y).G + bmp.GetPixel(x, y).B) / 3;
                    }
                }

            return ci;
            }
        
        
        /////////////////////////////////////////////////////////////
        //public static StructBWImage load_bw_image(Image img_orig, int X, int Y, int cropWidth, int cropHeight, int resizeWidth, int resizeHeight)
        //{
        //Bitmap bmp_orig = new Bitmap(img_orig);
        ////Color clr_orig = bmp_orig.GetPixel(100, 100);

        //Image img = CropResize(img_orig, X, Y, cropWidth, cropHeight, resizeWidth, resizeHeight);

        ////img.Save("resized_" + fname);

        //Bitmap bmp = new Bitmap(img);

        //StructBWImage ci = new StructBWImage();

        //ci.width = img.Width;
        //ci.height = img.Height;

        //ci.c1 = new float[ci.width * ci.height];

        //for (int x = 0; x < ci.width; ++x)
        //    {
        //    for (int y = 0; y < ci.width; ++y)
        //        {
        //        int n = y * ci.width + x;

        //        ci.c1[n] = (bmp.GetPixel(x, y).R + bmp.GetPixel(x, y).G + bmp.GetPixel(x, y).B) / 3;
        //        }
        //    }

        //return ci;
        //}
        
        
        /////////////////////////////////////////////////////////////
        public static StructColorImage LoadImage(Image img_orig, out Image img_resized)
        {
        Image img = ServUtility.FixedSize(img_orig, 256, 256);

        img_resized = img;

        Bitmap bmp = new Bitmap(img);

        StructColorImage ci = new StructColorImage();

        ci.width = img.Width;
        ci.height = img.Height;

        ci.c1 = new float[ci.width * ci.height];
        ci.c2 = new float[ci.width * ci.height];
        ci.c3 = new float[ci.width * ci.height];

        for (int x = 0; x < ci.width; ++x)
            {
            for (int y = 0; y < ci.width; ++y)
                {
                int n = y * ci.width + x;

                ci.c1[n] = bmp.GetPixel(x, y).R;
                ci.c2[n] = bmp.GetPixel(x, y).G;
                ci.c3[n] = bmp.GetPixel(x, y).B;
                }
            }

        return ci;
        }        
        


        /////////////////////////////////////////////////////////////
        public static Image FixedSize(Image imgPhoto, int Width, int Height)
        {
        int sourceWidth = imgPhoto.Width;
        int sourceHeight = imgPhoto.Height;

        int sourceX = 0;
        int sourceY = 0;
        int destX = 0;
        int destY = 0;

        float nPercent = 0;
        float nPercentW = 0;
        float nPercentH = 0;

        nPercentW = ((float)Width / (float)sourceWidth);
        nPercentH = ((float)Height / (float)sourceHeight);

        if (nPercentH < nPercentW)
            {
            nPercent = nPercentH;
            destX = System.Convert.ToInt16((Width - (sourceWidth * nPercent)) / 2);
            }
        else
            {
            nPercent = nPercentW;
            destY = System.Convert.ToInt16((Height - (sourceHeight * nPercent)) / 2);
            }

        int destWidth = (int)(sourceWidth * nPercent);
        int destHeight = (int)(sourceHeight * nPercent);

        Bitmap bmPhoto = new Bitmap(Width, Height, imgPhoto.PixelFormat);
        //bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

        Graphics grPhoto = Graphics.FromImage(bmPhoto);
        grPhoto.Clear(Color.Black);
        grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(imgPhoto,
            new Rectangle(destX, destY, destWidth, destHeight),
            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            GraphicsUnit.Pixel);

        grPhoto.Dispose();

        return bmPhoto;
        }


        /////////////////////////////////////////////////////////////
        static Image CropResize(Image imgPhoto, int X, int Y, int cropWidth, int cropHeight, int resizeWidth, int resizeHeight)
        {
        Bitmap bmPhoto = new Bitmap(resizeWidth, resizeHeight, imgPhoto.PixelFormat);
        bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

        Graphics grPhoto = Graphics.FromImage(bmPhoto);
        grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(imgPhoto,
                        new Rectangle(0, 0, resizeWidth, resizeHeight),
                        new Rectangle(X, Y, cropWidth, cropHeight),
                        GraphicsUnit.Pixel);

        grPhoto.Dispose();

        return bmPhoto;
        }

        
        //////////////////////////////////////////////////////////////////////////
        public static double CalcDistance(float[] descs_1, float[] descs_2)
        {
        double sum = 0.0;

        if(descs_1.GetLength(0) != descs_2.GetLength(0))
            return -10.0;

        for(int i = 0; i < descs_1.GetLength(0); ++i)
            {
            sum += Math.Pow(descs_1[i] - descs_2[i], 2);
            }

        sum  = Math.Sqrt(sum);

        return sum;
        }


		
        ////////////////////////////////////////////////////////////////////////////
        //public static ArrayList SplitSearchPhraze(ref NpgsqlConnection conn, string str)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
		
        //command.CommandText = "SELECT to_tsvector('ru', :str);";
		
        //command.Parameters.Add(new NpgsqlParameter("str", DbType.String));
		
        //command.Prepare();

        //command.Parameters[0].Value = str;

        //NpgsqlDataReader data = command.ExecuteReader();
		
        //if (data.HasRows)
        //    {
        //    data.Read();
			
        //    return 	SplitTsVectorResult(data.GetString(0));
        //    }
        //else
        //    return new ArrayList();
        //}
		


        
		//////////////////////////////////////////////////////////////////////////
		public static Byte[] GetBinaryFieldValue(ref NpgsqlDataReader data, int nFieldIndex)
		{
        Byte[] arrRes = null;

        if (!data.IsDBNull(nFieldIndex))
			{
			long len = data.GetBytes(nFieldIndex, 0, null, 0, 0);
			arrRes = new Byte[len];
			data.GetBytes(nFieldIndex, 0, arrRes, 0, (int)len);
			}

        return arrRes;
        }						


	    //////////////////////////////////////////////////////////////////////////
        public static string JoinArray(ref string[] arr, string strSep, int nElemsToUse)
        {
        string strRes = "";

        if(nElemsToUse == 0)
            return strRes;
        else if(nElemsToUse == 1)
            return (string) arr[0];
        else
            {
            strRes = (string) arr[0];

            for(int i = 1; i < nElemsToUse; ++i)
                strRes += strSep + (string) arr[i];
            }

        return strRes;
        }

	    
	    //////////////////////////////////////////////////////////////////////////
        public static string GetImageFormat(ref byte [] arrImage)
        {
        string strFormat = "";

        if(arrImage.GetLength(0) < 10)
            return strFormat;

        if(arrImage[0] == 71 && arrImage[1] == 73 && arrImage[2] == 70)
            strFormat = "gif";
        else if(arrImage[0] == 137 && arrImage[1] == 80 && arrImage[2] == 78 && arrImage[3] == 71)
            strFormat = "png";
        else if(arrImage[0] == 255 && arrImage[1] == 216 && arrImage[2] == 255)
            strFormat = "jpg";
        else
            strFormat = "";

        
        return strFormat;
        }

        
	    //////////////////////////////////////////////////////////////////////////
        public static string GetIniFilePath()
        {
        string strPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
        strPath += "settings.ini";

        return strPath;
        }



	    //////////////////////////////////////////////////////////////////////////
        public static string ReadStringFromConfig(string strKey)
        {
        string strRes = "";
        
        try
        {
        strRes = System.Web.Configuration.WebConfigurationManager.AppSettings.Get(strKey);
        }
        catch
        {
        }

        return strRes;
        }


	    //////////////////////////////////////////////////////////////////////////
        public static int ReadIntFromConfig(string strKey)
        {
        int nRes = 0;
        
        try
        {
        string str = System.Web.Configuration.WebConfigurationManager.AppSettings.Get(strKey);
        
        nRes = Int32.Parse(str);
        }
        catch
        {
        }

        return nRes;
        }


	    //////////////////////////////////////////////////////////////////////////
        public static double ReadDoubleFromConfig(string strKey)
        {
        double dblRes = 0.0;
        
        try
        {
        string str = System.Web.Configuration.WebConfigurationManager.AppSettings.Get(strKey);

        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en-US");
        dblRes = Double.Parse(str, culture);
        }
        catch
        {
        }

        return dblRes;
        }


	    //////////////////////////////////////////////////////////////////////////
        public static bool IdenticalFileExists(string strPath, int nFileSize)
        {
        FileInfo fi = new FileInfo(strPath);

        return (File.Exists(strPath) && fi.Length == nFileSize);
        }

        
	    //////////////////////////////////////////////////////////////////////////
        public static bool IsFileActual(string strPath, DateTime dt)
        {
        FileInfo fi = new FileInfo(strPath);

        if(!File.Exists(strPath))
            return false;
        
        if(fi.LastWriteTime < dt)
            return false;
        else
            return true;
        }



	    //////////////////////////////////////////////////////////////////////////
        public static bool FileExists(string strPathWithoutExt, ref string strExt)
        {
        FileInfo fi;

        string strFileExt = "jpg";
        string strFullPath = strPathWithoutExt + "." + strFileExt;
        
        fi = new FileInfo(strFullPath);

        if(File.Exists(strFullPath))
            {
            strExt = strFileExt;
            return true;
            }

        strFileExt = "gif";
        strFullPath = strPathWithoutExt + "." + strFileExt;

        fi = new FileInfo(strFullPath);

        if(File.Exists(strFullPath))
            {
            strExt = strFileExt;
            return true;
            }

        strFileExt = "png";
        strFullPath = strPathWithoutExt + "." + strFileExt;

        fi = new FileInfo(strFullPath);

        if(File.Exists(strFullPath))
            {
            strExt = strFileExt;
            return true;
            }

        return false;
        }


        //////////////////////////////////////////////////////////////////////////
        public static bool FileExists(string strFullPath)
        {
        FileInfo fi = new FileInfo(strFullPath);

        return (File.Exists(strFullPath));
        }


        
	    //////////////////////////////////////////////////////////////////////////
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {       
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
        
        return dtDateTime;
        }

        
	    //////////////////////////////////////////////////////////////////////////
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
        return (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }


        
        //////////////////////////////////////////////////////////////////////////
        public static string GetKeyFromAuthorizationHeader(string strHeader)
        {
        string strKey = "";


        try
            {
            if (strHeader == null || strHeader.Length == 0)
                return strKey;

            string[] arrSplit = strHeader.Split(new Char[] { ' ' });

            if (arrSplit.GetLength(0) == 2 && arrSplit[0] == "Basic")
                {
                byte[] data = Convert.FromBase64String(arrSplit[1]);
                strKey = Encoding.UTF8.GetString(data);
                }
            }
        catch { }

        return strKey;
        }

			
        
	    //////////////////////////////////////////////////////////////////////////
        public static Rectangle GetImageDimensions(ref byte[] arrImg)
        {
        Rectangle rect = new Rectangle();
        MemoryStream ms = new MemoryStream(arrImg);
        Image img = Image.FromStream(ms);

        

        rect.Width = img.Width;
        rect.Height = img.Height;

        return rect;
        }


	    //////////////////////////////////////////////////////////////////////////
        public static bool ResizeImage(ref byte [] arrImgSrc, out byte [] arrImgDest, int nWidth, int nHeight)
        {
        arrImgDest = new byte[0];

        if(arrImgSrc.GetLength(0) == 0)
            return false;

        string strFormat = GetImageFormat(ref arrImgSrc);

        ImageFormat imgFormat;

        if(strFormat == "gif")
            imgFormat = ImageFormat.Gif;
        else if(strFormat == "png")
            imgFormat = ImageFormat.Png;
        else if(strFormat == "jpg")
            imgFormat = ImageFormat.Jpeg;
        else
            return false;

        MemoryStream ms = new MemoryStream(arrImgSrc);
        Image imgSrc = Image.FromStream(ms);

        Bitmap imgDest = new Bitmap(nWidth, nHeight);
        Graphics newGraphics = Graphics.FromImage(imgDest);

        newGraphics.CompositingQuality = CompositingQuality.HighQuality;
        newGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
        newGraphics.SmoothingMode = SmoothingMode.HighQuality;
        newGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        newGraphics.DrawImage(imgSrc, 0, 0, nWidth, nHeight);

        MemoryStream outStream = new MemoryStream();
        imgDest.Save(outStream, imgFormat);

        long nLen = outStream.Length;
        outStream.Seek(0, SeekOrigin.Begin);

        arrImgDest = new byte[nLen];

        int nL = outStream.Read(arrImgDest, 0, (int)nLen);

        return true;
        }



		//////////////////////////////////////////////////////////////////////////
		public static void LogMethodCall(ref NpgsqlConnection conn, int nIdUser, string strMethodName, string strCallResult, string strParameters, string strComment, string strLocale, string strClientAppVersion, int nTimespan, string strRequestBody)
		{
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
        command.CommandText = @"INSERT INTO db_calls_log(id_user, call_timestamp, method_name, call_result, parameters, client_app_version, locale, timespan, client_platform, comment, request_body) 
                               VALUES (:idu, current_timestamp, :mn, :cr, :pt, :ver, :loc, :ts, :plat, :comm, :rb)";			

		command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
		command.Parameters.Add(new NpgsqlParameter("mn", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("cr", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("pt", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("ver", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("loc", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("ts", DbType.Int32));
		command.Parameters.Add(new NpgsqlParameter("plat", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("comm", DbType.String));
        command.Parameters.Add(new NpgsqlParameter("rb", DbType.String));
			
		command.Prepare();

		command.Parameters[0].Value = nIdUser;
		command.Parameters[1].Value = strMethodName;
		command.Parameters[2].Value = strCallResult;

        if(strParameters != null && strParameters.Length > 0)
		    command.Parameters[3].Value = strParameters;
        else
		    command.Parameters[3].Value = null;

		command.Parameters[4].Value = strClientAppVersion;
		command.Parameters[5].Value = strLocale;
		command.Parameters[6].Value = nTimespan;
		//command.Parameters[7].Value = nIdRecord;

        string strPlatform = "";
        string strVersion = "";
        SplitClientAppVersionString(strClientAppVersion, ref strPlatform, ref strVersion);

        if(strPlatform != null && strPlatform.Length > 0)
		    command.Parameters[7].Value = strPlatform;
        else
		    command.Parameters[7].Value = null;

        if(strComment != null && strComment.Length > 0)
		    command.Parameters[8].Value = strComment;
        else
		    command.Parameters[8].Value = null;

        command.Parameters[9].Value = strRequestBody;
        
        command.ExecuteNonQuery();
		}


        
        //////////////////////////////////////////////////////////////////////////
        public static void UpdateStatData(ref NpgsqlConnection conn, int nIdProduct, int nIdRetailer, string strActivity, string strLocale, string strInputData)
        {
        NpgsqlCommand command = new NpgsqlCommand();
        command.Connection = conn;

        command.CommandText = @"INSERT INTO db_stat_data(activity, id_product, id_retailer, input_data, locale) VALUES (:act, :idp, :idr, :inpd, :loc);";

        command.Parameters.Add(new NpgsqlParameter("act", DbType.String));
        command.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
        command.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
        command.Parameters.Add(new NpgsqlParameter("inpd", DbType.String));
        command.Parameters.Add(new NpgsqlParameter("loc", DbType.String));

        command.Prepare();

        command.Parameters[0].Value = strActivity;
        command.Parameters[1].Value = nIdProduct;
        command.Parameters[2].Value = nIdRetailer;
        command.Parameters[3].Value = strInputData;
        command.Parameters[4].Value = strLocale;

        command.ExecuteNonQuery();
        }



        //////////////////////////////////////////////////////////////////////////
        public static void UpdateStatData(ref NpgsqlConnection conn, StructProducts prod, string strActivity, string strLocale, string strInputData)
        {
        NpgsqlCommand command = new NpgsqlCommand();
        command.Connection = conn;

        string strSQL = "INSERT INTO db_stat_data(activity, id_product, id_retailer, input_data, locale) VALUES;";

        string strSQL2 = "('{0}', {1}, {2}, '{3}', '{4}'),";

        foreach(StructProduct pr in prod.products)
            {
            strSQL += "\n";
            strSQL += String.Format(strSQL2, "view", pr.id, pr.retailer_id, strInputData, strLocale);
            }

        strSQL = strSQL.Substring(0, strSQL.Length - 1);
        strSQL += ";";

        command.CommandText = strSQL;

        command.Prepare();

        command.ExecuteNonQuery();
        }




		//////////////////////////////////////////////////////////////////////////
        public static bool IsServiceAvailable(ref StructResult result, string strLocale)
        {
        int nUnavailable = ReadIntFromConfig("service_temporarily_unavailable");

        if(nUnavailable == 1)
            {
            result.result_code = ResultCode.Failure_InternalServiceError;

            if(strLocale == "ru")
                result.message = "Сервис временно недоступен";
            else
                result.message = "The service is temporarily unavailable";

            return false;
            }

        return true;
        }

		

        
		//////////////////////////////////////////////////////////////////////////
        public static bool SplitClientAppVersionString(string strClientAppVersionString, ref string strPlatform, ref string strVersion)
        {
        string[] arrComponents = strClientAppVersionString.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

        if(arrComponents == null || arrComponents.GetLength(0) < 2)
            return false;

        strPlatform = arrComponents[0].ToLower();
        strVersion = arrComponents[1];

        return true;
        }


        ////////////////////////////////////////////////////////////////////////////
        //public static bool AllAccountSymbolsValid(string strAccountName)
        //{
        //string pat = @"[^1234567890abcdefghijklmnopqrstuvwxyz_]";
		
        //Regex r = new Regex(pat, RegexOptions.IgnoreCase);
        //bool bRes = r.IsMatch(strAccountName);

        //return !bRes;
        //}



	    //////////////////////////////////////////////////////////////////////////
        public static string GetMd5Hash(MD5 md5Hash, string input)
        {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        StringBuilder sBuilder = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
            {
            sBuilder.Append(data[i].ToString("x2"));
            }

        return sBuilder.ToString();
        }


        ////////////////////////////////////////////////////////////////////////////
        //public static bool StringContainsLetters(string str)
        //{
        //string pat = @".*\w.*";
		
        //Regex rx = new Regex(pat, RegexOptions.IgnoreCase);
		
        //bool bRes =  rx.IsMatch(str);

        //rx = null;

        //return bRes;
        //}


        ////////////////////////////////////////////////////////////////////////////
        //public static bool IsAccountNameReserved(string strName)
        //{
        //string pat = @"^register$|^logout$|^authvk$|^authfb$|^about$|^legal$|^list$|^nearby$|^liked$|^add$|^profile$|^interests$|^howitworks$|^business$|^topic\d+$";
		
        //Regex rx = new Regex(pat, RegexOptions.IgnoreCase);
		
        //bool bRes =  rx.IsMatch(strName);

        //rx = null;

        //return bRes;
        //}

		
        
        //////////////////////////////////////////////////////////////////////////
        public static Image ResizeImg(Image image, int nWidth, int nHeight)
        {
        Image result = new Bitmap(nWidth, nHeight);

        using (var g = Graphics.FromImage(result))
            {
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(image, 0, 0, nWidth, nHeight);
            g.Dispose();
            }

        return result;
        }



        //////////////////////////////////////////////////////////////////////////
        public static string TransliterateString(string str)
        {
        string strOut = ServUtility.Transliteration.Front(str);

        strOut = strOut.Replace(" ", "-");
        strOut = strOut.Replace("\"", "");
        strOut = strOut.Replace("'", "");
        strOut = strOut.Replace("`", "");
        strOut = strOut.Replace(",", "-");
        strOut = strOut.Replace(".", "-");
        strOut = strOut.Replace(";", "-");
        strOut = strOut.Replace("?", "-");
        strOut = strOut.Replace("!", "-");
        strOut = strOut.Replace("~", "-");
        strOut = strOut.Replace("@", "-");
        strOut = strOut.Replace("#", "-");
        strOut = strOut.Replace("$", "-");
        strOut = strOut.Replace("%", "-");
        strOut = strOut.Replace("^", "-");
        strOut = strOut.Replace("&", "-");
        strOut = strOut.Replace("*", "-");
        strOut = strOut.Replace("+", "-");
        strOut = strOut.Replace("=", "-");
        strOut = strOut.Replace("/", "-");
        strOut = strOut.Replace("\\", "-");
        strOut = strOut.Replace("|", "-");
        strOut = strOut.Replace("<", "-");
        strOut = strOut.Replace(">", "-");
        strOut = strOut.Replace("(", "-");
        strOut = strOut.Replace(")", "-");
        strOut = strOut.Replace("[", "-");
        strOut = strOut.Replace("]", "-");
        strOut = strOut.Replace("{", "-");
        strOut = strOut.Replace("}", "-");

        return strOut;
        }



        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public enum TransliterationType
            {
            Gost,
            ISO
            }
        public static class Transliteration
            {
            private static Dictionary<string, string> gost = new Dictionary<string, string>(); //ГОСТ 16876-71
            private static Dictionary<string, string> iso = new Dictionary<string, string>(); //ISO 9-95

            public static string Front(string text)
                {
                return Front(text, TransliterationType.ISO);
                }
            public static string Front(string text, TransliterationType type)
                {
                string output = text;
                Dictionary<string, string> tdict = GetDictionaryByType(type);

                foreach (KeyValuePair<string, string> key in tdict)
                    {
                    output = output.Replace(key.Key, key.Value);
                    }
                return output;
                }
            public static string Back(string text)
                {
                return Back(text, TransliterationType.ISO);
                }
            public static string Back(string text, TransliterationType type)
                {
                string output = text;
                Dictionary<string, string> tdict = GetDictionaryByType(type);

                foreach (KeyValuePair<string, string> key in tdict)
                    {
                    output = output.Replace(key.Value, key.Key);
                    }
                return output;
                }

            private static Dictionary<string, string> GetDictionaryByType(TransliterationType type)
                {
                Dictionary<string, string> tdict = iso;
                if (type == TransliterationType.Gost) tdict = gost;
                return tdict;
                }

            static Transliteration()
                {
                gost.Add("Є", "EH");
                gost.Add("І", "I");
                gost.Add("і", "i");
                gost.Add("№", "#");
                gost.Add("є", "eh");
                gost.Add("А", "A");
                gost.Add("Б", "B");
                gost.Add("В", "V");
                gost.Add("Г", "G");
                gost.Add("Д", "D");
                gost.Add("Е", "E");
                gost.Add("Ё", "JO");
                gost.Add("Ж", "ZH");
                gost.Add("З", "Z");
                gost.Add("И", "I");
                gost.Add("Й", "JJ");
                gost.Add("К", "K");
                gost.Add("Л", "L");
                gost.Add("М", "M");
                gost.Add("Н", "N");
                gost.Add("О", "O");
                gost.Add("П", "P");
                gost.Add("Р", "R");
                gost.Add("С", "S");
                gost.Add("Т", "T");
                gost.Add("У", "U");
                gost.Add("Ф", "F");
                gost.Add("Х", "KH");
                gost.Add("Ц", "C");
                gost.Add("Ч", "CH");
                gost.Add("Ш", "SH");
                gost.Add("Щ", "SHH");
                gost.Add("Ъ", "'");
                gost.Add("Ы", "Y");
                gost.Add("Ь", "");
                gost.Add("Э", "EH");
                gost.Add("Ю", "YU");
                gost.Add("Я", "YA");
                gost.Add("а", "a");
                gost.Add("б", "b");
                gost.Add("в", "v");
                gost.Add("г", "g");
                gost.Add("д", "d");
                gost.Add("е", "e");
                gost.Add("ё", "jo");
                gost.Add("ж", "zh");
                gost.Add("з", "z");
                gost.Add("и", "i");
                gost.Add("й", "jj");
                gost.Add("к", "k");
                gost.Add("л", "l");
                gost.Add("м", "m");
                gost.Add("н", "n");
                gost.Add("о", "o");
                gost.Add("п", "p");
                gost.Add("р", "r");
                gost.Add("с", "s");
                gost.Add("т", "t");
                gost.Add("у", "u");

                gost.Add("ф", "f");
                gost.Add("х", "kh");
                gost.Add("ц", "c");
                gost.Add("ч", "ch");
                gost.Add("ш", "sh");
                gost.Add("щ", "shh");
                gost.Add("ъ", "");
                gost.Add("ы", "y");
                gost.Add("ь", "");
                gost.Add("э", "eh");
                gost.Add("ю", "yu");
                gost.Add("я", "ya");
                gost.Add("«", "");
                gost.Add("»", "");
                gost.Add("—", "-");

                iso.Add("Є", "YE");
                iso.Add("І", "I");
                iso.Add("Ѓ", "G");
                iso.Add("і", "i");
                iso.Add("№", "#");
                iso.Add("є", "ye");
                iso.Add("ѓ", "g");
                iso.Add("А", "A");
                iso.Add("Б", "B");
                iso.Add("В", "V");
                iso.Add("Г", "G");
                iso.Add("Д", "D");
                iso.Add("Е", "E");
                iso.Add("Ё", "YO");
                iso.Add("Ж", "ZH");
                iso.Add("З", "Z");
                iso.Add("И", "I");
                iso.Add("Й", "J");
                iso.Add("К", "K");
                iso.Add("Л", "L");
                iso.Add("М", "M");
                iso.Add("Н", "N");
                iso.Add("О", "O");
                iso.Add("П", "P");
                iso.Add("Р", "R");
                iso.Add("С", "S");
                iso.Add("Т", "T");
                iso.Add("У", "U");
                iso.Add("Ф", "F");
                iso.Add("Х", "X");
                iso.Add("Ц", "C");
                iso.Add("Ч", "CH");
                iso.Add("Ш", "SH");
                iso.Add("Щ", "SHH");
                iso.Add("Ъ", "'");
                iso.Add("Ы", "Y");
                iso.Add("Ь", "");
                iso.Add("Э", "E");
                iso.Add("Ю", "YU");
                iso.Add("Я", "YA");
                iso.Add("а", "a");
                iso.Add("б", "b");
                iso.Add("в", "v");
                iso.Add("г", "g");
                iso.Add("д", "d");
                iso.Add("е", "e");
                iso.Add("ё", "yo");
                iso.Add("ж", "zh");
                iso.Add("з", "z");
                iso.Add("и", "i");
                iso.Add("й", "j");
                iso.Add("к", "k");
                iso.Add("л", "l");
                iso.Add("м", "m");
                iso.Add("н", "n");
                iso.Add("о", "o");
                iso.Add("п", "p");
                iso.Add("р", "r");
                iso.Add("с", "s");
                iso.Add("т", "t");
                iso.Add("у", "u");
                iso.Add("ф", "f");
                iso.Add("х", "x");
                iso.Add("ц", "c");
                iso.Add("ч", "ch");
                iso.Add("ш", "sh");
                iso.Add("щ", "shh");
                iso.Add("ъ", "");
                iso.Add("ы", "y");
                iso.Add("ь", "");
                iso.Add("э", "e");
                iso.Add("ю", "yu");
                iso.Add("я", "ya");
                iso.Add("«", "");
                iso.Add("»", "");
                iso.Add("—", "-");
                }
            }


    
    }
}

