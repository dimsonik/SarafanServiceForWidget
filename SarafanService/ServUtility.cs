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

//using MyMediaLite.Data;
//using MyMediaLite.Eval;
//using MyMediaLite.IO;
//using MyMediaLite.ItemRecommendation;

//MemoryStream ms = new MemoryStream(b);
//Image bottom_bar = Image.FromStream(ms);


 //string [] fileEntries = Directory.GetFiles(targetDirectory);

namespace SarafanService
{



    [DataContract]
    public class GeoIPResponse
        {
        [DataMember(Name = "country")]
        public GeoIPCountry country { get; set; }
        [DataMember(Name = "location")]
        public GeoIPLocation location { get; set; }
        [DataMember(Name = "city")]
        public GeoIPCity city { get; set; }
        [DataMember(Name = "subdivisions")]
        public GeoIPSubdivision[] subdivisions { get; set; }
        [DataMember(Name = "maxmind")]
        public GeoIPMaxmind maxmind { get; set; }
        }


    [DataContract]
    public class GeoIPCountry
        {
        [DataMember(Name = "iso_code")]
        public string iso_code { get; set; }
        [DataMember(Name = "names")]
        public GeoIPNames names { get; set; }
        [DataMember(Name = "geoname_id")]
        public int geoname_id { get; set; }
        }

    [DataContract]
    public class GeoIPSubdivision
        {
        [DataMember(Name = "iso_code")]
        public string iso_code { get; set; }
        [DataMember(Name = "names")]
        public GeoIPNames names { get; set; }
        [DataMember(Name = "geoname_id")]
        public int geoname_id { get; set; }
        }


    [DataContract]
    public class GeoIPCity
        {
        [DataMember(Name = "iso_code")]
        public string iso_code { get; set; }
        [DataMember(Name = "names")]
        public GeoIPNames names { get; set; }
        [DataMember(Name = "geoname_id")]
        public int geoname_id { get; set; }
        }


    [DataContract]
    public class GeoIPLocation
        {
        [DataMember(Name = "longitude")]
        public double longitude { get; set; }
        [DataMember(Name = "latitude")]
        public double latitude { get; set; }
        [DataMember(Name = "time_zone")]
        public string time_zone { get; set; }
        }

    [DataContract]
    public class GeoIPMaxmind
        {
        [DataMember(Name = "queries_remaining")]
        public int queries_remaining { get; set; }
        }


    [DataContract]
    public class GeoIPNames
        {
        [DataMember(Name = "ru")]
        public string ru { get; set; }
        [DataMember(Name = "en")]
        public string en { get; set; }
        }






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
        //////////////////////////////////////////////////////////////////////////
        public static void DeleteOutdatedSearchRequests(ref NpgsqlConnection conn)
        {
        NpgsqlCommand commandDeleteMarked = new NpgsqlCommand();
        commandDeleteMarked.Connection = conn;
        commandDeleteMarked.CommandText = "DELETE FROM db_search_requests WHERE current_timestamp >= (\"timestamp\" + INTERVAL '2' DAY);";
        commandDeleteMarked.Prepare();

        commandDeleteMarked.ExecuteNonQuery();
        }


        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static void DeleteOutdatedModelPictures(ref NpgsqlConnection conn)
        {
        NpgsqlCommand commandDeleteMarked = new NpgsqlCommand();
        commandDeleteMarked.Connection = conn;
        commandDeleteMarked.CommandText = "DELETE FROM db_model_pictures WHERE current_timestamp >= (\"timestamp\" + INTERVAL '2' DAY);";
        commandDeleteMarked.Prepare();

        commandDeleteMarked.ExecuteNonQuery();
        }


		//////////////////////////////////////////////////////////////////////////
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
		//////////////////////////////////////////////////////////////////////////
		public static int CountWordsInPhrase(string strInput)
		{
		string[] arrWords =	strInput.Split(new char[] {' ', '.','?', ',', '\n', '!', ';', ':'}, StringSplitOptions.RemoveEmptyEntries);
		
		return arrWords.GetLength(0);
		}

		
		
		//////////////////////////////////////////////////////////////////////////
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

		
		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
		public static ArrayList SplitSearchPhraze(ref NpgsqlConnection conn, string str)
		{
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
		
		command.CommandText = "SELECT to_tsvector('ru', :str);";
		
        command.Parameters.Add(new NpgsqlParameter("str", DbType.String));
		
		command.Prepare();

		command.Parameters[0].Value = str;

		NpgsqlDataReader data = command.ExecuteReader();
		
		if (data.HasRows)
			{
			data.Read();
			
			return 	SplitTsVectorResult(data.GetString(0));
			}
		else
			return new ArrayList();
		}
		

		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
		public static void FilterSearchLexems(ref NpgsqlConnection conn, ArrayList listAll, out ArrayList listValid, out ArrayList listNotInDict)
		{
		listValid = new ArrayList();
		listNotInDict = new ArrayList();

        if(listAll.Count == 0)
            return;
			
		ArrayList listVerbs = new ArrayList();
			
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
		command.CommandText = "SELECT word, flags FROM db_main_wordforms WHERE lower(word) IN (";
		command.CommandText += "'" + listAll[0] + "'";
			
		for(int i = 1; i < listAll.Count; ++i)
			{
			command.CommandText += ", '" + listAll[i].ToString().ToLower() + "'";
			}
			
		command.CommandText += ");";
			
		command.Prepare();
			
		NpgsqlDataReader data = command.ExecuteReader();
			
		if (data.HasRows)
		    {
			while(data.Read())
				{
				string strWord = data.GetString(0);
				string strFlag = data.GetString(1);
				
				if(strFlag == "V")
					listVerbs.Add(strWord);
                //else
                //    listValid.Add (strWord);
				}

		    }

		foreach(string strW in listAll)
			{
			if(!listVerbs.Contains(strW))
				listValid.Add(strW);
			} 
			
		foreach(string strW in listAll)
			{
			if(!listValid.Contains(strW) && !listVerbs.Contains (strW))
				listNotInDict.Add (strW);
			} 

		}

		
		
			
		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
		public static void LogLexemsNotInDict(ref NpgsqlConnection conn, ArrayList listNotInDict)
		{
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
		command.CommandText = "SELECT favoram_update_dictionary_faults_word(:w);";
			
		command.Parameters.Add(new NpgsqlParameter("w", DbType.String));
			
		command.Prepare();

        Regex rx = new Regex(@"^-?\d+$", RegexOptions.IgnoreCase);
			
		foreach(string strW in listNotInDict)
			{
            if(strW.Length == 1 || rx.IsMatch(strW))
                continue;

			command.Parameters[0].Value = strW;
			command.ExecuteNonQuery();
			} 
		}




		
        
        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
		public static int GetRecordAuthor(ref NpgsqlConnection conn, int nRecordId)
		{
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;

        int nAuthorId = -1;
			
		command.CommandText = "SELECT id_author FROM db_records WHERE id_record = :idr;";
			
		command.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
			
		command.Prepare();

		command.Parameters[0].Value = nRecordId;
			
		NpgsqlDataReader data = command.ExecuteReader();
			
		if (data.HasRows)
		    {
			data.Read();
			nAuthorId = data.GetInt32(0);
            }
            
        return nAuthorId;	 
		}


		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
		public static void SaveSearchString(ref NpgsqlConnection conn, int nIdUser, string strText)
		{
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
		command.CommandText = "INSERT INTO db_search_history (id_user, search_string) VALUES(:idu, :txt);";
			
		command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
		command.Parameters.Add(new NpgsqlParameter("txt", DbType.String));
			
		command.Prepare();

		command.Parameters[0].Value = nIdUser;
		command.Parameters[1].Value = strText;

		command.ExecuteNonQuery();
		}



		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static void SaveTopicPhraze(ref NpgsqlConnection conn, int nIdUser, ref StructUserTopicPhraze topicPhraze)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
			
        //command.CommandText = "INSERT INTO db_topic_phrazes(phraze, id_user) VALUES (:phraze, :idu) RETURNING id_phraze;";
			
        //command.Parameters.Add(new NpgsqlParameter("phraze", DbType.String));
        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
			
        //command.Prepare();

        //command.Parameters[0].Value = topicPhraze.strPhraze;
        //command.Parameters[1].Value = nIdUser;

        //Object obj = command.ExecuteScalar();

        //topicPhraze.nIdPhraze = (int)obj;

        //if(topicPhraze.listTopics.Count == 0)
        //    return;

        //string strList = topicPhraze.listTopics[0].nIdTopic.ToString();

        //for(int i = 1; i < topicPhraze.listTopics.Count; ++i)
        //    {
        //    strList += ", " + topicPhraze.listTopics[i].nIdTopic.ToString();
        //    }


        //command.CommandText = "UPDATE dbl_user_topics SET id_phraze = :idph WHERE id_user = :idu AND id_topic IN ( " + strList + ");";
        
        //command.Parameters.Clear();			
        //command.Parameters.Add(new NpgsqlParameter("idph", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
			
        //command.Prepare();

        //command.Parameters[0].Value = topicPhraze.nIdPhraze;
        //command.Parameters[1].Value = nIdUser;

        //command.ExecuteNonQuery();
        //}


        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        //public static void SaveTopicPhraze(ref NpgsqlConnection conn, int nIdUser, string strPhraze, List<int> lstIdTopics, bool bIsVirtual = false)
        //    {
        //    NpgsqlCommand command = new NpgsqlCommand();
        //    command.Connection = conn;

        //    command.CommandText = "INSERT INTO db_topic_phrazes(phraze, id_user, virtual) VALUES (:phraze, :idu, :virt) RETURNING id_phraze;";

        //    command.Parameters.Add(new NpgsqlParameter("phraze", DbType.String));
        //    command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //    command.Parameters.Add(new NpgsqlParameter("virt", DbType.Boolean));

        //    command.Prepare();

        //    command.Parameters[0].Value = strPhraze;
        //    command.Parameters[1].Value = nIdUser;
        //    command.Parameters[2].Value = bIsVirtual;

        //    Object obj = command.ExecuteScalar();

        //    int nIdPhraze = (int)obj;

        //    string strList = lstIdTopics[0].ToString();

        //    for (int i = 1; i < lstIdTopics.Count; ++i)
        //        {
        //        strList += ", " + lstIdTopics[i].ToString();
        //        }


        //    command.CommandText = "UPDATE dbl_user_topics SET id_phraze = :idph WHERE id_user = :idu AND id_topic IN ( " + strList + ");";

        //    command.Parameters.Clear();
        //    command.Parameters.Add(new NpgsqlParameter("idph", DbType.Int32));
        //    command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));

        //    command.Prepare();

        //    command.Parameters[0].Value = nIdPhraze;
        //    command.Parameters[1].Value = nIdUser;

        //    command.ExecuteNonQuery();
        //    }

        
        
        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        //public static bool TopicPhrazeExists(ref NpgsqlConnection conn, int nIdUser, string strPhraze)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
			
        //command.CommandText = "SELECT id_phraze FROM db_topic_phrazes WHERE id_user = :idu AND quote_literal(lower(phraze)) = lower(:phraze) AND deleted = FALSE;";
			
        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("phraze", DbType.String));
			
        //command.Prepare();

        //command.Parameters[0].Value = nIdUser;
        //command.Parameters[1].Value = strPhraze;

        //NpgsqlDataReader data = command.ExecuteReader();
			
        //if (data.HasRows)
        //    {
        //    return true;
        //    }

        //return false;
        //}

		
        
        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        public static bool KeywordStringExists(ref NpgsqlConnection conn, int nIdRecord)
        {
		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
		command.CommandText = "SELECT id_keyword_string FROM db_keyword_strings WHERE id_record = :idr;";
			
		command.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
			
		command.Prepare();

		command.Parameters[0].Value = nIdRecord;

		NpgsqlDataReader data = command.ExecuteReader();
			
		if (data.HasRows)
    		{
            return true;
            }

        return false;
        }


        

        
		//////////////////////////////////////////////////////////////////////////
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
		//////////////////////////////////////////////////////////////////////////
        //public static void AddLinkedTopics(ref NpgsqlConnection conn, int nIdUser, List<StructUserTopic> listTopics, double dblBasicWeightDelta, bool bNegativeAllowed)
        //{
        ////Stopwatch sw = new Stopwatch();
        ////Stopwatch sw2 = new Stopwatch();

        //double dblSameGroupWeightFactor = ServUtility.ReadDoubleFromConfig("same_group_weight_factor");
        //double dblSiblingGroupWeightFactor = ServUtility.ReadDoubleFromConfig("sibling_group_weight_factor");

        //List<int> listIds = ServUtility.GetIdListFromTopicList(listTopics);

        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT id_topic, id_parent, id_label FROM db_topic_groups WHERE id_topic = :idt;";
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Prepare();

        //NpgsqlCommand command_2 = new NpgsqlCommand();
        //command_2.Connection = conn;
        //command_2.CommandText = "SELECT id_topic, id_label FROM db_topic_groups WHERE id_parent = :idp;";
        //command_2.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
        //command_2.Prepare();

        ////sw.Start();

        //List<KeyValuePair<int, double>> listNewTopics = new List<KeyValuePair<int, double>>();

        //foreach(StructUserTopic topic in listTopics)
        //    {
        //    command.Parameters[0].Value = topic.nIdTopic;

        //    NpgsqlDataReader data = command.ExecuteReader();
			
        //    if (data.HasRows)
        //        {
        //        while(data.Read())
        //            {
        //            int nIdTopic = data.GetInt32(0);
        //            int nIdParent = data.GetInt32(1);

        //            int nIdOriginalTopicGroup = -1;

        //            if(!data.IsDBNull(2))
        //                nIdOriginalTopicGroup = data.GetInt32(2);

        //            if(nIdParent == 0)
        //                {
        //                command_2.Parameters[0].Value = nIdTopic;
        //                NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //                if (data_2.HasRows)
        //                    {
        //                    while(data_2.Read())
        //                        {
        //                        int nIdTopic_2 = data_2.GetInt32(0);
        //                        int nIdGroup = -1;

        //                        if(!data_2.IsDBNull(1))
        //                            nIdGroup = data_2.GetInt32(1);
                                
        //                        if(!listIds.Contains(nIdTopic_2))
        //                            listNewTopics.Add(new KeyValuePair<int, double>(nIdTopic_2, dblBasicWeightDelta * dblSiblingGroupWeightFactor));
        //                        }
        //                    }
        //                }
        //            else
        //                {
        //                command_2.Parameters[0].Value = nIdParent;
        //                NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //                if (data_2.HasRows)
        //                    {
        //                    while(data_2.Read())
        //                        {
        //                        int nIdTopic_2 = data_2.GetInt32(0);
        //                        int nIdGroup = -1;

        //                        if(!data_2.IsDBNull(1))
        //                            nIdGroup = data_2.GetInt32(1);

        //                        double dblWeightDelta = dblBasicWeightDelta;

        //                        if(nIdGroup == nIdOriginalTopicGroup)
        //                            dblWeightDelta *= dblSameGroupWeightFactor;
        //                        else
        //                            dblWeightDelta *= dblSiblingGroupWeightFactor;

        //                        if(!listIds.Contains(nIdTopic_2))
        //                            listNewTopics.Add(new KeyValuePair<int, double>(nIdTopic_2, dblWeightDelta));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        ////sw.Stop();
        ////sw2.Start();

        //command.CommandText = "SELECT favoram_update_topic_weight(:idu, :idt, :wt, :ex, :fc, :na);";
			
        //command.Parameters.Clear();

        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("wt", DbType.Double));
        //command.Parameters.Add(new NpgsqlParameter("ex", DbType.Boolean));
        //command.Parameters.Add(new NpgsqlParameter("fc", DbType.Boolean));
        //command.Parameters.Add(new NpgsqlParameter("na", DbType.Boolean));

        //command.Prepare();

        //command.Parameters[0].Value = nIdUser;
        //command.Parameters[3].Value = false;
        //command.Parameters[4].Value = false;
        //command.Parameters[5].Value = bNegativeAllowed;

        //foreach(KeyValuePair<int, double> pair in listNewTopics)
        //    {
        //    command.Parameters[1].Value = pair.Key;
        //    command.Parameters[2].Value = pair.Value;

        //    command.ExecuteNonQuery();
        //    }


        ////int[] idt = new Int32[listNewTopics.Count];
        ////double[] wt = new double[listNewTopics.Count];

        ////for(int i= 0; i < listNewTopics.Count; ++i)
        ////    {
        ////    idt[i] = listNewTopics[i].Key;
        ////    wt[i] = listNewTopics[i].Value;
        ////    }

        ////command.CommandText = "SELECT favoram_update_topics_weight(:idu, :idt, :wt, :ex, :fc, :na);";
			
        ////command.Parameters.Clear();

        ////command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        ////command.Parameters.Add(new NpgsqlParameter("idt", NpgsqlDbType.Array | NpgsqlDbType.Integer));
        ////command.Parameters.Add(new NpgsqlParameter("wt", NpgsqlDbType.Array | NpgsqlDbType.Double));
        ////command.Parameters.Add(new NpgsqlParameter("ex", DbType.Boolean));
        ////command.Parameters.Add(new NpgsqlParameter("fc", DbType.Boolean));
        ////command.Parameters.Add(new NpgsqlParameter("na", DbType.Boolean));

        ////command.Prepare();

        ////command.Parameters[0].Value = nIdUser;
        ////command.Parameters[0].Value = idt;
        ////command.Parameters[0].Value = wt;
        ////command.Parameters[3].Value = false;
        ////command.Parameters[4].Value = false;
        ////command.Parameters[5].Value = bNegativeAllowed;

        ////command.ExecuteNonQuery();
       
       
        ////sw2.Stop();
        ////ServUtility.LogMethodCall(ref conn, nIdUser, -1, MethodBase.GetCurrentMethod().Name, "", null, sw.ElapsedMilliseconds.ToString() + " __ " + sw2.ElapsedMilliseconds.ToString() + " ----- " + listNewTopics.Count.ToString(), "", "", -1);
        //}


		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static void AddLinkedTopics(ref NpgsqlConnection conn, int nIdUser, List<int> listIdTopics, double dblBasicWeightDelta, bool bNegativeAllowed)
        //{
        //double dblSameGroupWeightFactor = ServUtility.ReadDoubleFromConfig("same_group_weight_factor");
        //double dblSiblingGroupWeightFactor = ServUtility.ReadDoubleFromConfig("sibling_group_weight_factor");

        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT id_topic, id_parent, id_label FROM db_topic_groups WHERE id_topic = :idt;";
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Prepare();

        //NpgsqlCommand command_2 = new NpgsqlCommand();
        //command_2.Connection = conn;
        //command_2.CommandText = "SELECT id_topic, id_label FROM db_topic_groups WHERE id_parent = :idp;";
        //command_2.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
        //command_2.Prepare();

        //List<KeyValuePair<int, double>> listNewTopics = new List<KeyValuePair<int, double>>();

        //foreach(int nId in listIdTopics)
        //    {
        //    command.Parameters[0].Value = nId;

        //    NpgsqlDataReader data = command.ExecuteReader();
			
        //    if (data.HasRows)
        //        {
        //        while(data.Read())
        //            {
        //            int nIdTopic = data.GetInt32(0);
        //            int nIdParent = data.GetInt32(1);

        //            int nIdOriginalTopicGroup = -1;

        //            if(!data.IsDBNull(2))
        //                nIdOriginalTopicGroup = data.GetInt32(2);

        //            if(nIdParent == 0)
        //                {
        //                command_2.Parameters[0].Value = nIdTopic;
        //                NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //                if (data_2.HasRows)
        //                    {
        //                    while(data_2.Read())
        //                        {
        //                        int nIdTopic_2 = data_2.GetInt32(0);
        //                        int nIdGroup = -1;

        //                        if(!data_2.IsDBNull(1))
        //                            nIdGroup = data_2.GetInt32(1);
                                
        //                        if(!listIdTopics.Contains(nIdTopic_2))
        //                            listNewTopics.Add(new KeyValuePair<int, double>(nIdTopic_2, dblBasicWeightDelta * dblSiblingGroupWeightFactor));
        //                        }
        //                    }
        //                }
        //            else
        //                {
        //                command_2.Parameters[0].Value = nIdParent;
        //                NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //                if (data_2.HasRows)
        //                    {
        //                    while(data_2.Read())
        //                        {
        //                        int nIdTopic_2 = data_2.GetInt32(0);
        //                        int nIdGroup = -1;

        //                        if(!data_2.IsDBNull(1))
        //                            nIdGroup = data_2.GetInt32(1);

        //                        double dblWeightDelta = dblBasicWeightDelta;

        //                        if(nIdGroup == nIdOriginalTopicGroup)
        //                            dblWeightDelta *= dblSameGroupWeightFactor;
        //                        else
        //                            dblWeightDelta *= dblSiblingGroupWeightFactor;

        //                        if(!listIdTopics.Contains(nIdTopic_2))
        //                            listNewTopics.Add(new KeyValuePair<int, double>(nIdTopic_2, dblWeightDelta));
        //                        }
        //                    }
        //                }
        //            }

        //        }

        //    }


        //command.CommandText = "SELECT favoram_update_topic_weight(:idu, :idt, :wt, :ex, :fc, :na);";
			
        //command.Parameters.Clear();

        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("wt", DbType.Double));
        //command.Parameters.Add(new NpgsqlParameter("ex", DbType.Boolean));
        //command.Parameters.Add(new NpgsqlParameter("fc", DbType.Boolean));
        //command.Parameters.Add(new NpgsqlParameter("na", DbType.Boolean));

        //command.Prepare();

        //command.Parameters[0].Value = nIdUser;
        //command.Parameters[3].Value = false;
        //command.Parameters[4].Value = false;
        //command.Parameters[5].Value = bNegativeAllowed;

        //foreach(KeyValuePair<int, double> pair in listNewTopics)
        //    {
        //    command.Parameters[1].Value = pair.Key;
        //    command.Parameters[2].Value = pair.Value;

        //    command.ExecuteNonQuery();
        //    }
        //}


		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static List<int> GetLinkedTopics(ref NpgsqlConnection conn, int nIdTopic)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT id_topic, id_parent FROM db_topic_groups WHERE id_topic = :idt;";
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Prepare();

        //NpgsqlCommand command_2 = new NpgsqlCommand();
        //command_2.Connection = conn;
        //command_2.CommandText = "SELECT id_topic FROM db_topic_groups WHERE id_parent = :idp;";
        //command_2.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
        //command_2.Prepare();

        //List<int> listNewTopics = new List<int>();

        //command.Parameters[0].Value = nIdTopic;

        //NpgsqlDataReader data = command.ExecuteReader();
			
        //if(data.HasRows)
        //    {
        //    while(data.Read())
        //        {
        //        int nIdT = data.GetInt32(0);
        //        int nIdParent = data.GetInt32(1);

        //        if(nIdParent == 0)
        //            {
        //            command_2.Parameters[0].Value = nIdTopic;
        //            NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //            if (data_2.HasRows)
        //                {
        //                while(data_2.Read())
        //                    {
        //                    int nIdTopic_2 = data_2.GetInt32(0);
                                
        //                    if(nIdTopic != nIdTopic_2)
        //                        listNewTopics.Add(nIdTopic_2);
        //                    }
        //                }
        //            }
        //        else
        //            {
        //            command_2.Parameters[0].Value = nIdParent;
        //            NpgsqlDataReader data_2 = command_2.ExecuteReader();
			
        //            if (data_2.HasRows)
        //                {
        //                while(data_2.Read())
        //                    {
        //                    int nIdTopic_2 = data_2.GetInt32(0);

        //                    if(nIdTopic != nIdTopic_2)
        //                        listNewTopics.Add(nIdTopic_2);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //return listNewTopics;
        //}


		//////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        public static double GetAverageWeightThreshold(ref NpgsqlConnection conn)
        {
        double dblThreshold = 0.6;

		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
        command.CommandText = "SELECT favoram_get_average_weight_threshold();";
		command.Prepare();


        try
        {
        dblThreshold = (double) command.ExecuteScalar();
        }
        catch {  }

        return dblThreshold;
        }


    


	    //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        //public static List<StructUserTopic> GetStringTopics(ref NpgsqlConnection conn, string strPhraze)
        //{
        //List<StructUserTopic> listTopics = new List<StructUserTopic>();

        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT DISTINCT id_topic, variant FROM db_topic_variants WHERE to_tsvector('ru', quote_literal(:txt)) @@ to_tsquery('ru', '''' || rtrim(ltrim(quote_literal(db_topic_variants.variant), ''''), '''') || '''');";
        //command.Parameters.Add(new NpgsqlParameter("txt", DbType.String));
        //command.Prepare();
        //command.Parameters[0].Value = strPhraze;

        //NpgsqlDataReader data = command.ExecuteReader();
			
        //if (data.HasRows)
        //    {
        //    while(data.Read())
        //        {
        //        StructUserTopic topic = new StructUserTopic();

        //        topic.nIdTopic = data.GetInt32(0);
        //        topic.strTopic = data.GetString(1);

        //        listTopics.Add(topic);
        //        }
        //    }

        //return listTopics;
        //}





        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static List<int> GetStringTopics3(ref NpgsqlConnection conn, string strPhraze)
            {
            List<int> listTopics = new List<int>();

            NpgsqlCommand command = new NpgsqlCommand();
            command.Connection = conn;
            command.CommandText = @"SELECT inn.t, first(inn.v) FROM (SELECT t, tv.variant v FROM favoram_get_user_interests_in_string(:txt) t
                                    INNER JOIN db_topic_variants tv ON tv.id_topic = t AND tv.id_locale = 1
                                    ORDER BY tv.id_variant) AS inn
                                    GROUP BY inn.t;";
            command.Parameters.Add(new NpgsqlParameter("txt", DbType.String));
            command.Prepare();
            command.Parameters[0].Value = strPhraze;

            NpgsqlDataReader data = command.ExecuteReader();

            if (data.HasRows)
                {
                while (data.Read())
                    {
                    listTopics.Add(data.GetInt32(0));
                    }
                }

            return listTopics;
            }

	    
        //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        //public static List<int> GetIdListFromTopicList(List<StructUserTopic> listTopics)
        //{
        //List<int> listIds = new List<int>();

        //foreach(StructUserTopic topic in listTopics)
        //    {
        //    listIds.Add(topic.nIdTopic);
        //    }
        
        //return listIds;
        //}




	    //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        //public static string GetCoordsByAddress(string strAddress)
        //{
        //string strGeo = "";

        //try
        //{
        //GeoAdapter geo = new GeoAdapter();
        //geocode.gapi.cdpisb.Data.GeoPositioningData data = geo.GeoEncode(strAddress);

        ////if(data.ResultType == geocode.gapi.cdpisb.Data.GeoPositioningData.GeoPositionResultType.OK && data.IsPartial == false 
        ////                      && (data.GeoCodeType == geocode.gapi.cdpisb.Data.AddressComponent.GeoCodeType.street_address 
        ////                      || data.GeoCodeType == geocode.gapi.cdpisb.Data.AddressComponent.GeoCodeType.locality  
        ////                      || data.GeoCodeType == geocode.gapi.cdpisb.Data.AddressComponent.GeoCodeType.political
        ////                      || data.GeoCodeType == geocode.gapi.cdpisb.Data.AddressComponent.GeoCodeType.street_number
        ////                      || data.GeoCodeType == geocode.gapi.cdpisb.Data.AddressComponent.GeoCodeType.route))

        //if(data.ResultType == geocode.gapi.cdpisb.Data.GeoPositioningData.GeoPositionResultType.OK && data.IsPartial == false)
        //    {
        //    if(data.Location.Longitude > 200 || data.Location.Longitude < -200)
        //        {
        //        data.Location.Longitude /= 10000000;
        //        data.Location.Latitude /= 10000000;
        //        }

        //    strGeo = data.Location.Longitude.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " " + data.Location.Latitude.ToString(CultureInfo.CreateSpecificCulture("en-US"));
            
        //    strGeo = strGeo.Replace(',', '.');
        //    }
        //}
        //catch
        //{
        //}
        
        //return strGeo;
        //}


	    //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        public static string GetCoordsByAddress(string strAddress)
        {
        string strGeo = "";

        try
        {
        GeoAdapter geo = new GeoAdapter();
        geocode.gapi.cdpisb.Data.GeoPositioningData data = geo.GeoEncode(strAddress);

        if(data.ResultType == geocode.gapi.cdpisb.Data.GeoPositioningData.GeoPositionResultType.OK && data.IsPartial == false)
            {
            if(data.Location.Longitude > 200 || data.Location.Longitude < -200)
                {
                data.Location.Longitude /= 10000000;
                data.Location.Latitude /= 10000000;
                }

            strGeo = data.Location.Longitude.ToString() + " " + data.Location.Latitude.ToString();
            strGeo = strGeo.Replace(',', '.');
            }
        else
            {
            Regex rx = new Regex(@".*[а-я].*", RegexOptions.IgnoreCase);
            
            if(rx.IsMatch(strAddress))
                {
    		    string[] arrWords =	strAddress.Split(new char[] {' ', '.','?', ',', '\n', '!', ';', ':'}, StringSplitOptions.RemoveEmptyEntries);

                int nWords = arrWords.GetLength(0);

                for(int i = nWords - 1; i > 1; --i)
                    {
                    string strNewAddr = JoinArray(ref arrWords, " ", i);

                    data = geo.GeoEncode(strNewAddr);

                    if(data.ResultType == geocode.gapi.cdpisb.Data.GeoPositioningData.GeoPositionResultType.OK && data.IsPartial == false)
                        {
                        if(data.Location.Longitude > 200 || data.Location.Longitude < -200)
                            {
                            data.Location.Longitude /= 10000000;
                            data.Location.Latitude /= 10000000;
                            }

                        strGeo = data.Location.Longitude.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " " + data.Location.Latitude.ToString(CultureInfo.CreateSpecificCulture("en-US"));
                        strGeo = strGeo.Replace(',', '.');

                        break;
                        }
                    }
                }
            }
        }
        catch(Exception e)
        {
        string str = e.Message;
        }
        
        return strGeo;
        }


        //////////////////////////////////////////////////////////////////////////
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
	    //////////////////////////////////////////////////////////////////////////
        public static string GetIniFilePath()
        {
        string strPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
        strPath += "settings.ini";

        return strPath;
        }





        //////////////////////////////////////////////////////////////////////////
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
	    //////////////////////////////////////////////////////////////////////////
        public static bool IdenticalFileExists(string strPath, int nFileSize)
        {
        FileInfo fi = new FileInfo(strPath);

        return (File.Exists(strPath) && fi.Length == nFileSize);
        }

        
        //////////////////////////////////////////////////////////////////////////
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
        //////////////////////////////////////////////////////////////////////////
        public static bool FileExists(string strFullPath)
        {
        FileInfo fi = new FileInfo(strFullPath);

        return (File.Exists(strFullPath));
        }


        
        //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {       
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
        
        return dtDateTime;
        }

        
        //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
        return (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }


        //////////////////////////////////////////////////////////////////////////
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
        //////////////////////////////////////////////////////////////////////////
        public static void UpdateStatData(ref NpgsqlConnection conn, int nIdUser, string strActivity, string strLocale, string strClientAppVersion, int nIdRecord, string strInputData)
        {
        string strPlatform = "";
        string strVersion = "";

        SplitClientAppVersionString(strClientAppVersion, ref strPlatform, ref strVersion);

        if(strPlatform == "console")
            return;

        NpgsqlCommand command = new NpgsqlCommand();
        command.Connection = conn;

        command.CommandText = @"INSERT INTO db_stat_data(id_user, activity, ""timestamp"", client_platform, id_record, input_data)
                                VALUES (:idu, :act, current_timestamp, :platf, :idr, :indata)";

        command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        command.Parameters.Add(new NpgsqlParameter("act", DbType.String));
        command.Parameters.Add(new NpgsqlParameter("platf", DbType.String));
        command.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
        command.Parameters.Add(new NpgsqlParameter("indata", DbType.String));

        command.Prepare();

        command.Parameters[0].Value = nIdUser;
        command.Parameters[1].Value = strActivity;
        command.Parameters[2].Value = strPlatform;

        if(nIdRecord > 0)
            command.Parameters[3].Value = nIdRecord;
        else
            command.Parameters[3].Value = null;

        if(strInputData != null && strInputData.Length > 0)
            command.Parameters[4].Value = strInputData;
        else
            command.Parameters[4].Value = null;

        command.ExecuteNonQuery();
        }




		//////////////////////////////////////////////////////////////////////////
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
		//////////////////////////////////////////////////////////////////////////
        //public static bool IsClientAppVersionValid(ref StructResult result, string strLocale, string strClientAppVersion)
        //{
        ////return true;

        //string strMinVersion = ReadStringFromConfig("min_client_app_version");

        //string strPlatform = "";
        //string strVersion = "";


        //bool bSplitRes = SplitClientAppVersionString(strClientAppVersion, ref strPlatform, ref strVersion);

        //strPlatform = strPlatform.Trim();
        //strVersion = strVersion.Trim();
        
        
        //if(!bSplitRes || (strPlatform != "browser" && String.Compare(strMinVersion, strVersion) > 0))
        //    {
        //    result.resultCode = ResultCode.Failure_ClientAppUpdateRequired;

        //    if(strLocale == "ru")
        //        result.strErrorInfo = "Требуется обновление приложения";
        //    else
        //        result.strErrorInfo = "Application update required";

        //    return false;
        //    }

        //return true;
        //}


        
        //////////////////////////////////////////////////////////////////////////
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


        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        public static bool AllAccountSymbolsValid(string strAccountName)
        {
   		string pat = @"[^1234567890abcdefghijklmnopqrstuvwxyz_]";
		
		Regex r = new Regex(pat, RegexOptions.IgnoreCase);
		bool bRes = r.IsMatch(strAccountName);

        return !bRes;
        }


        //+///////////////////////////////////////////////////////////////////////
        //public static bool SendEmail(string strTo, string strSubject, string strBody, bool bIsBodyHtml, ref StructResult result, string strLocale)
        //{
        //try
        //    {
        //    string strSMTPServer = ServUtility.ReadStringFromConfig("smtp_server");
        //    int nSMTPPort = ServUtility.ReadIntFromConfig("smtp_port");
        //    string strSMTPAccount = ServUtility.ReadStringFromConfig("smtp_account");
        //    string strSMTPPassword = ServUtility.ReadStringFromConfig("smtp_password");

        //    SmtpClient client = new SmtpClient(strSMTPServer, nSMTPPort);
        //    client.Credentials = new NetworkCredential(strSMTPAccount, strSMTPPassword);

        //    // Specify the e-mail sender. 
        //    MailAddress from = new MailAddress(strSMTPAccount, "FAVORaim");
                
        //    // Set destinations for the e-mail message.
        //    MailAddress to = new MailAddress(strTo);
                
        //    // Specify the message content.
        //    MailMessage message = new MailMessage(from, to);

        //    message.Body = strBody;
        //    message.BodyEncoding =  System.Text.Encoding.UTF8;

        //    if(bIsBodyHtml)
        //        message.IsBodyHtml = true;

        //    message.Subject = strSubject;
        //    message.SubjectEncoding = System.Text.Encoding.UTF8;

        //    message.Headers.Add("Precedence", "bulk");
        //    message.Headers.Add("Return-Path", "<>");
                
        //    // Set the method that is called back when the send operation ends.
        //    //client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
                
        //    // The userState can be any object that allows your callback  method to identify this send operation. 
        //    // For this example, the userToken is a string constant.
        //    //string userState = "test message1";
        //    //client.SendAsync(message, userState);

        //    client.Send(message);
                
        //    message.Dispose();
        //    client = null;
        //    }
        //catch(Exception e)
        //    {
        //    result.strErrorInfo = e.Message;
        //    result.resultCode = ResultCode.Failure_SMTPError;

        //    return false;
        //    }

        //return true;
        //}

        
        //+///////////////////////////////////////////////////////////////////////
        public static bool ClearCachedAvatarImages(int nIdUser)
        {
        string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");

        string [] fileEntries = Directory.GetFiles(strPicturesFolder, "avatar_" + nIdUser.ToString() + "_*.*");

		foreach(string file in fileEntries)
            {
            File.Delete(file);
            }

        fileEntries = Directory.GetFiles(strPicturesFolder, "avatar_" + nIdUser.ToString() + ".*");

		foreach(string file in fileEntries)
            {
            File.Delete(file);
            }

        return true;
        }


        //+///////////////////////////////////////////////////////////////////////
        public static bool ClearCachedRecordPhotos(int nIdRecord)
        {
        string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");

        string [] fileEntries = Directory.GetFiles(strPicturesFolder, "photo_" + nIdRecord.ToString() + "_*.*");

		foreach(string file in fileEntries)
            {
            File.Delete(file);
            }

        return true;
        }



        //+///////////////////////////////////////////////////////////////////////
        static public bool GetAvatarFromDb(ref NpgsqlConnection conn, int nIdUser, DateTime dtAvatarModificationTimestamp, string strAvatarExt, int nAvatarVersion, ref string strAvatarUrl)
        {
        //nAvatarVersion = 0;

        string strPicturesBaseUrl = ServUtility.ReadStringFromConfig("pictures_url");
        string strPicturesFolder = ServUtility.ReadStringFromConfig("pictures_folder");

        string strAvatarPath = strPicturesFolder + "avatar_" + nIdUser.ToString() + "_" + nAvatarVersion.ToString() + "." + strAvatarExt;
        strAvatarUrl = strPicturesBaseUrl + "avatar_" + nIdUser.ToString() + "_" + nAvatarVersion.ToString() + "." + strAvatarExt;

        //userInfo.strAvatarUrl = strAvatarUrl;

        if(!ServUtility.IsFileActual(strAvatarPath, dtAvatarModificationTimestamp))
            {
			NpgsqlCommand command2 = new NpgsqlCommand();
			command2.Connection = conn;

            command2.CommandText = "SELECT decode(avatar_enc, 'base64') FROM db_users WHERE id_user = :idu;";
					
    		command2.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
				
	    	command2.Prepare();
		    command2.Parameters[0].Value = nIdUser;
				
			NpgsqlDataReader data2 = command2.ExecuteReader();
			
			if(data2.HasRows)
				{
				data2.Read();
                        
                byte [] arrAvatar = ServUtility.GetBinaryFieldValue(ref data2, 0);

                if(arrAvatar != null && arrAvatar.GetLength(0) > 0)
                    {
                    File.WriteAllBytes(strAvatarPath, arrAvatar);
                    }
                }
            }
        
        return true;
        }



        //////////////////////////////////////////////////////////////////////////
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


        //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        public static bool StringContainsLetters(string str)
        {
		string pat = @".*\w.*";
		
		Regex rx = new Regex(pat, RegexOptions.IgnoreCase);
		
        bool bRes =  rx.IsMatch(str);

        rx = null;

        return bRes;
        }


        //////////////////////////////////////////////////////////////////////////
	    //////////////////////////////////////////////////////////////////////////
        public static bool IsAccountNameReserved(string strName)
        {
		string pat = @"^register$|^logout$|^authvk$|^authfb$|^about$|^legal$|^list$|^nearby$|^liked$|^add$|^profile$|^interests$|^howitworks$|^business$|^topic\d+$";
		
		Regex rx = new Regex(pat, RegexOptions.IgnoreCase);
		
        bool bRes =  rx.IsMatch(strName);

        rx = null;

        return bRes;
        }

		
        
        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
//        public static void UpdateUserTopicWeight2(ref NpgsqlConnection conn, int nIdUser, int nIdTopic, bool bAddLinkedTopics)
//        {
//        NpgsqlCommand command = new NpgsqlCommand();
//        command.Connection = conn;


//        NpgsqlCommand commandInner = new NpgsqlCommand();
//        commandInner.Connection = conn;
//        commandInner.CommandText = "INSERT INTO dbl_user_topics_2 (id_user, id_topic, weight, numerator, denominator) VALUES(:idu, :idt, :w, :nu, :de);";
//        commandInner.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
//        commandInner.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
//        commandInner.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
//        commandInner.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
//        commandInner.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));

//        commandInner.Prepare();


//        NpgsqlCommand commandCheck3 = new NpgsqlCommand();
//        commandCheck3.Connection = conn;
//        commandCheck3.CommandText = "SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND id_topic = :idt AND explicit = TRUE;";
//        commandCheck3.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
//        commandCheck3.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
//        commandCheck3.Prepare();

			
//        command.CommandText = @"SELECT COUNT(*), first(db_record_likes.rating) FROM dbl_record_topics 
//                                   INNER JOIN db_record_likes ON dbl_record_topics.id_record = db_record_likes.id_record AND db_record_likes.id_user=:idu AND db_record_likes.rating <> 0 AND dbl_record_topics.id_topic=:idt
//                                   GROUP BY db_record_likes.rating ORDER BY db_record_likes.rating;";
        
//        command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
//        command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
			
		
//        command.Prepare();

//        command.Parameters[0].Value = nIdUser;
//        command.Parameters[1].Value = nIdTopic;

//        NpgsqlDataReader data = command.ExecuteReader();
			
//        data = command.ExecuteReader();
			
//        int nSumLikes = 0;
//        int nSumUnlikes = 0;

//        if(data.HasRows)
//            {
//            while(data.Read())
//                {
//                string strType = data.GetDataTypeName(0);

//                int nCount = (int) data.GetInt64(0);
//                int nRating = data.GetInt32(1);

//                if(nRating == 1)
//                    nSumLikes = nCount;
//                else if(nRating == -1)
//                    nSumUnlikes = nCount;
//                }
//            }

//        commandCheck3.Parameters[0].Value = nIdUser;
//        commandCheck3.Parameters[1].Value = nIdTopic;

//        NpgsqlDataReader data_ch3 = commandCheck3.ExecuteReader();
			
//        if(data_ch3.HasRows)
//            {
//            nSumLikes += 2;
//            }


//        int nSum = nSumLikes + nSumUnlikes;

//        double dblWeight = 0;
            
//        if(nSum > 0)
//            dblWeight = (nSumLikes - nSumUnlikes) / (double) nSum;

//        commandInner.Parameters[0].Value = nIdUser;
//        commandInner.Parameters[1].Value = nIdTopic;
//        commandInner.Parameters[2].Value = dblWeight;
//        commandInner.Parameters[3].Value = nSumLikes - nSumUnlikes;
//        commandInner.Parameters[4].Value = nSum;

//        try
//            {
//            commandInner.ExecuteNonQuery();
//            }
//        catch
//            {
//            commandInner.CommandText = "UPDATE dbl_user_topics_2 SET weight = :w, numerator = :nu, denominator = :de, update_timestamp = current_timestamp WHERE id_user = :idu AND id_topic = :idt;";

//            commandInner.Parameters.Clear();

//            commandInner.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
//            commandInner.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
//            commandInner.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
//            commandInner.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
//            commandInner.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));

//            commandInner.Prepare();

//            commandInner.Parameters[0].Value = nIdUser;
//            commandInner.Parameters[1].Value = nIdTopic;
//            commandInner.Parameters[2].Value = dblWeight;
//            commandInner.Parameters[3].Value = nSumLikes - nSumUnlikes;
//            commandInner.Parameters[4].Value = nSum;

//            commandInner.ExecuteNonQuery();
//            }

//        //if(bAddLinkedTopics)
//        //    {
//        //    List<int> lstLinked = GetLinkedTopics(ref conn, nIdTopic);

//        //    foreach(int nIdT in lstLinked)
//        //        {
//        //        commandInner.Parameters[0].Value = nIdUser;
//        //        commandInner.Parameters[1].Value = nIdT;
//        //        commandInner.Parameters[2].Value = 1.0;
//        //        commandInner.Parameters[3].Value = 1;
//        //        commandInner.Parameters[4].Value = 1;

//        //        try
//        //            {
//        //            commandInner.ExecuteNonQuery();
//        //            }
//        //        catch
//        //            {
//        //            }
//        //        }

//        //    }

//        return;
//        }


        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static void UpdateUserTopicWeightAltLike(ref NpgsqlConnection conn, int nIdUser, List<StructUserTopic> listTopics, int nNumeratorDelta, int nDenominatorDelta)
        //{
        //NpgsqlCommand commandUpdate = new NpgsqlCommand();
        //commandUpdate.Connection = conn;
        //commandUpdate.CommandText = "UPDATE dbl_user_topics_2 SET weight = :w, numerator = :nu, denominator = :de, update_timestamp = current_timestamp WHERE id_user = :idu AND id_topic = :idt;";
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandUpdate.Prepare();

        //NpgsqlCommand commandInsert = new NpgsqlCommand();
        //commandInsert.Connection = conn;
        //commandInsert.CommandText = "INSERT INTO dbl_user_topics_2 (id_user, id_topic, weight, numerator, denominator, update_timestamp) VALUES (:idu, :idt, :w, :nu, :de, current_timestamp);";
        //commandInsert.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandInsert.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandInsert.Prepare();

        //foreach(StructUserTopic topic in listTopics)
        //    {
        //    int nNumerator = 0;
        //    int nDenominator = 0;
        //    double dblWeight = 0.0;

        //    bool bExists = GetNumeratorDenominatorValues(ref conn, nIdUser, topic.nIdTopic, ref nNumerator, ref nDenominator);

        //    if(bExists)
        //        {
        //        nNumerator += nNumeratorDelta;
        //        nDenominator += nDenominatorDelta;

        //        if(nDenominator <= 0)
        //            {
        //            nDenominator = 0;
        //            nNumerator = 0;
        //            dblWeight = 0.0;
        //            }
        //        else if((double)nNumerator / (double)nDenominator > 1.0)
        //            {
        //            nNumerator = nDenominator;
        //            dblWeight = 1.0;
        //            }
        //        else if((double)nNumerator / (double)nDenominator < -1.0)
        //            {
        //            nNumerator = - nDenominator;
        //            dblWeight = -1.0;
        //            }
        //        else
        //            {
        //            dblWeight = (double)nNumerator / (double)nDenominator;
        //            }

        //        commandUpdate.Parameters[0].Value = nIdUser;
        //        commandUpdate.Parameters[1].Value = topic.nIdTopic;
        //        commandUpdate.Parameters[2].Value = dblWeight;
        //        commandUpdate.Parameters[3].Value = nNumerator;
        //        commandUpdate.Parameters[4].Value = nDenominator;

        //        int nRecs = commandUpdate.ExecuteNonQuery();
        //        }
        //    else
        //        {
        //        if(nNumeratorDelta == 1 && nDenominatorDelta == 1) // лайк
        //            {
        //            nDenominator = 1;
        //            nNumerator = 1;
        //            dblWeight = 1.0;
        //            }
        //        else if(nNumeratorDelta == 2 && nDenominatorDelta == 0) // лайк
        //            {
        //            nDenominator = 1;
        //            nNumerator = 1;
        //            dblWeight = 1.0;
        //            }
        //        else if(nNumeratorDelta == -1 && nDenominatorDelta == 1) // анлайк
        //            {
        //            nDenominator = 1;
        //            nNumerator = -1;
        //            dblWeight = -1.0;
        //            }
        //        else if(nNumeratorDelta == -2 && nDenominatorDelta == 0) // анлайк
        //            {
        //            nDenominator = 1;
        //            nNumerator = -1;
        //            dblWeight = -1.0;
        //            }
        //        else if(nNumeratorDelta == -1 && nDenominatorDelta == -1) // отмена лайка
        //            {
        //            nDenominator = 0;
        //            nNumerator = 0;
        //            dblWeight = 0.0;
        //            }
        //        else if(nNumeratorDelta == 1 && nDenominatorDelta == -1) // восстановить
        //            {
        //            nDenominator = 0;
        //            nNumerator = 0;
        //            dblWeight = 0.0;
        //            }

                
        //        commandInsert.Parameters[0].Value = nIdUser;
        //        commandInsert.Parameters[1].Value = topic.nIdTopic;
        //        commandInsert.Parameters[2].Value = dblWeight;
        //        commandInsert.Parameters[3].Value = nNumerator;
        //        commandInsert.Parameters[4].Value = nDenominator;

        //        int nRecs = commandInsert.ExecuteNonQuery();
        //        }
        //    }
        //}


        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static void UpdateUserTopicWeightAltPhraze(ref NpgsqlConnection conn, int nIdUser, List<StructUserTopic> listTopics, int nDelta, bool bUpdateLinked)
        //{
        //NpgsqlCommand commandUpdate = new NpgsqlCommand();
        //commandUpdate.Connection = conn;
        //commandUpdate.CommandText = "UPDATE dbl_user_topics_2 SET weight = :w, numerator = :nu, denominator = :de, update_timestamp = current_timestamp WHERE id_user = :idu AND id_topic = :idt;";
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandUpdate.Prepare();

        //NpgsqlCommand commandInsert = new NpgsqlCommand();
        //commandInsert.Connection = conn;
        //commandInsert.CommandText = "INSERT INTO dbl_user_topics_2 (id_user, id_topic, weight, numerator, denominator, update_timestamp) VALUES(:idu, :idt, :w, :nu, :de, current_timestamp);";
        //commandInsert.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandInsert.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandInsert.Prepare();

        //foreach(StructUserTopic topic in listTopics)
        //    {
        //    int nNumerator = 0;
        //    int nDenominator = 0;
        //    double dblWeight = 0.0;

        //    bool bExists = GetNumeratorDenominatorValues(ref conn, nIdUser, topic.nIdTopic, ref nNumerator, ref nDenominator);

        //    if(bExists)
        //        {
        //        nNumerator += nDelta;
        //        nDenominator += nDelta;

        //        dblWeight = (double)nNumerator / (double)nDenominator;

        //        commandUpdate.Parameters[0].Value = nIdUser;
        //        commandUpdate.Parameters[1].Value = topic.nIdTopic;
        //        commandUpdate.Parameters[2].Value = dblWeight;
        //        commandUpdate.Parameters[3].Value = nNumerator;
        //        commandUpdate.Parameters[4].Value = nDenominator;

        //        int nRecs = commandUpdate.ExecuteNonQuery();
        //        }
        //    else
        //        {
        //        nNumerator = nDelta;
        //        nDenominator = nDelta;
        //        dblWeight = 1;
                
        //        commandInsert.Parameters[0].Value = nIdUser;
        //        commandInsert.Parameters[1].Value = topic.nIdTopic;
        //        commandInsert.Parameters[2].Value = dblWeight;
        //        commandInsert.Parameters[3].Value = nNumerator;
        //        commandInsert.Parameters[4].Value = nDenominator;

        //        int nRecs = commandInsert.ExecuteNonQuery();
        //        }
        //    }
        //}

        
        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static void UpdateUserTopicWeightAltPhraze(ref NpgsqlConnection conn, int nIdUser, List<int> listTopics, int nDelta, bool bUpdateLinked)
        //{
        //NpgsqlCommand commandUpdate = new NpgsqlCommand();
        //commandUpdate.Connection = conn;
        //commandUpdate.CommandText = "UPDATE dbl_user_topics_2 SET weight = :w, numerator = :nu, denominator = :de, update_timestamp = current_timestamp WHERE id_user = :idu AND id_topic = :idt;";
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandUpdate.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandUpdate.Prepare();

        //NpgsqlCommand commandInsert = new NpgsqlCommand();
        //commandInsert.Connection = conn;
        //commandInsert.CommandText = "INSERT INTO dbl_user_topics_2 (id_user, id_topic, weight, numerator, denominator, update_timestamp) VALUES(:idu, :idt, :w, :nu, :de, current_timestamp);";
        //commandInsert.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        //commandInsert.Parameters.Add(new NpgsqlParameter("nu", DbType.Int32));
        //commandInsert.Parameters.Add(new NpgsqlParameter("de", DbType.Int32));
        //commandInsert.Prepare();

        //commandUpdate.Prepare();

        //foreach(int nIdTopic in listTopics)
        //    {
        //    int nNumerator = 0;
        //    int nDenominator = 0;
        //    double dblWeight = 0.0;

        //    bool bExists = GetNumeratorDenominatorValues(ref conn, nIdUser, nIdTopic, ref nNumerator, ref nDenominator);

        //    if(bExists)
        //        {
        //        nNumerator += nDelta;
        //        nDenominator += nDelta;

        //        dblWeight = (double)nNumerator / (double)nDenominator;

        //        commandUpdate.Parameters[0].Value = nIdUser;
        //        commandUpdate.Parameters[1].Value = nIdTopic;
        //        commandUpdate.Parameters[2].Value = dblWeight;
        //        commandUpdate.Parameters[3].Value = nNumerator;
        //        commandUpdate.Parameters[4].Value = nDenominator;

        //        int nRecs = commandUpdate.ExecuteNonQuery();
        //        }
        //    else
        //        {
        //        nNumerator = nDelta;
        //        nDenominator = nDelta;
        //        dblWeight = 1;
                
        //        commandInsert.Parameters[0].Value = nIdUser;
        //        commandInsert.Parameters[1].Value = nIdTopic;
        //        commandInsert.Parameters[2].Value = dblWeight;
        //        commandInsert.Parameters[3].Value = nNumerator;
        //        commandInsert.Parameters[4].Value = nDenominator;

        //        int nRecs = commandInsert.ExecuteNonQuery();
        //        }
            
        //    }
        //}


        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        //public static bool GetNumeratorDenominatorValues(ref NpgsqlConnection conn, int nIdUser, int nIdTopic, ref int nNumerator, ref int nDenominator)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT numerator, denominator FROM dbl_user_topics_2 WHERE id_user = :idu AND id_topic = :idt;";
        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Prepare();
        //command.Parameters[0].Value = nIdUser;
        //command.Parameters[1].Value = nIdTopic;

        //NpgsqlDataReader data = command.ExecuteReader();
			
        //data = command.ExecuteReader();
			
        //if(data.HasRows)
        //    {
        //    data.Read();
        //        {
        //        nNumerator = (int) data.GetInt32(0);
        //        nDenominator = (int) data.GetInt32(1);

        //        return true;
        //        }
        //    }

        //return false;
        //}



        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        public static List<int> GetTopicsForPhraze(ref NpgsqlConnection conn, int nIdPhraze)
        {
        List<int> lstTopicIds = new List<int>();

		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
		command.CommandText = "SELECT id_topic FROM dbl_user_topics WHERE id_phraze = :idph;";
        command.Parameters.Add(new NpgsqlParameter("idph", DbType.Int32));
		command.Prepare();
		command.Parameters[0].Value = nIdPhraze;

		NpgsqlDataReader data = command.ExecuteReader();
			
		data = command.ExecuteReader();
			
		if(data.HasRows)
    		{
			while(data.Read())
				{
				int nId = (int) data.GetInt32(0);

                lstTopicIds.Add(nId);
                }
            }

        return lstTopicIds;
        }


    
    
        //////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////
        public static int SaveRecordKeywordString(ref NpgsqlConnection conn, int nIdRecord, string strString)
        {
        int nIdKeywordString = -1;

		NpgsqlCommand command = new NpgsqlCommand();
		command.Connection = conn;
			
		command.CommandText = "INSERT INTO db_keyword_strings(keyword_string, id_record) VALUES (:ks, :idr) RETURNING id_keyword_string;";
			
		command.Parameters.Add(new NpgsqlParameter("ks", DbType.String));
		command.Parameters.Add(new NpgsqlParameter("idr", DbType.Int32));
			
		command.Prepare();

		command.Parameters[0].Value = strString;
		command.Parameters[1].Value = nIdRecord;

        try
        {
		Object obj = command.ExecuteScalar();

        nIdKeywordString = (int)obj;
        }
        catch
        {
        }

        return nIdKeywordString;
        }




        
        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        //public static bool IsUserPaymentValid(ref NpgsqlConnection conn, int nIdUser)
        //{
        //bool bRes = false;

        //return true;

        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
			
        //command.CommandText = "SELECT paid_until >= current_timestamp FROM db_users WHERE id_user = :idu;";
			
        //command.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
			
        //command.Prepare();

        //command.Parameters[0].Value = nIdUser;

        //try
        //{
        //Object obj = command.ExecuteScalar();

        //bRes = (bool)obj;
        //}
        //catch
        //{
        //}

        //return bRes;
        //}



        
        ////+///////////////////////////////////////////////////////////////////////
        //public static bool IsCountryCodeValid(ref NpgsqlConnection conn, string strCountryCode)
        //{
        //bool bRes = false;

        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
			
        //command.CommandText = "SELECT id_country FROM db_countries WHERE short_name = :sn;";
			
        //command.Parameters.Add(new NpgsqlParameter("sn", DbType.String));
        //command.Prepare();
        //command.Parameters[0].Value = strCountryCode;
			
        //NpgsqlDataReader data = command.ExecuteReader();
			
        //if(data.HasRows)
        //    {
        //    bRes = true;
        //    }

        //return bRes;
        //}




    //////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
    public static double GetGeoDistance(string strPt1, string strPt2)
    {
    try
        {
        if(strPt1.StartsWith("P"))
            strPt1 = strPt1.Substring(6, strPt1.Length - 7);

        if(strPt2.StartsWith("P"))
            strPt2 = strPt2.Substring(6, strPt2.Length - 7);


		string[] arrPt1 =	strPt1.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        double l1 = Convert.ToDouble(arrPt1[0], new CultureInfo("en-US"));
        double f1 = Convert.ToDouble(arrPt1[1], new CultureInfo("en-US"));

		string[] arrPt2 =	strPt2.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        double l2 = Convert.ToDouble(arrPt2[0], new CultureInfo("en-US"));
        double f2 = Convert.ToDouble(arrPt2[1], new CultureInfo("en-US"));

        return GetGeoDistance(f1, l1, f2, l2);
        }
    catch
        {
        return -1.0;
        }
    }


    
    //////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
    public static double GetGeoDistance(double f1, double l1, double f2, double l2)
    {
    f1 = f1 * Math.PI/180;
    l1 = l1 * Math.PI/180;
    f2 = f2 * Math.PI/180;
    l2 = l2 * Math.PI/180;

    double df = Math.Abs(f1 - f2);
    double dl = Math.Abs(l1 - l2);

    double num1 = Math.Pow(Math.Cos(f2) * Math.Sin(dl), 2);
    double num2 = Math.Pow(Math.Cos(f1) * Math.Sin(f2) - Math.Sin(f1) * Math.Cos(f2) * Math.Cos(dl), 2);

    double num = Math.Sqrt(num1 + num2);

    double denom = Math.Sin(f1) * Math.Sin(f2) + Math.Cos(f1) * Math.Cos(f2) * Math.Cos(dl);

    double ang = Math.Atan2(num, denom);

    double res = ang * 6371000;

    return res;
    }


    //class StRecord
    //{
    //public int nId;
    //public string strCoord;
    //public DateTime dtBegin;
    //public DateTime dtEnd;
    //public List<int> lstTopics;
    //public bool bIgnore;
    //public int nDubOf;
    //public List<int> lstDubs;
    //}






    
    
    //+///////////////////////////////////////////////////////////////////////
    public static DateTime [] GetNearestWeekendBorders()
    {
    DateTime [] dtBorders = new DateTime [2];

    DateTime dtNow = DateTime.Now;
    dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day);

    int nDoWNow = (int) dtNow.DayOfWeek;

    if(nDoWNow == 6)
        {
        dtBorders[0] = dtNow;
        dtBorders[1] = dtBorders[0].AddDays(2);
        }
    else if(nDoWNow == 0)
        {
        dtBorders[0] = dtNow.AddDays(-1);
        dtBorders[1] = dtBorders[0].AddDays(2);
        }
    else
        {
        int nSpan = 6 - nDoWNow;

        dtBorders[0] = dtNow.AddDays(nSpan);
        dtBorders[1] = dtBorders[0].AddDays(2);
        }

    return dtBorders;
    }


    //+///////////////////////////////////////////////////////////////////////
    public static List<int> GetUserExplicitTopics(ref NpgsqlConnection conn, int nIdUser)
        {
        List<int> lstExplicitTopics = new List<int>();

        NpgsqlCommand commandSelectExplicitTopics = new NpgsqlCommand();
        commandSelectExplicitTopics.Connection = conn;
        commandSelectExplicitTopics.CommandText = @"SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND explicit = TRUE;";
        commandSelectExplicitTopics.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandSelectExplicitTopics.Prepare();

        commandSelectExplicitTopics.Parameters[0].Value = nIdUser;

        NpgsqlDataReader dataSelectExplicitTopics = commandSelectExplicitTopics.ExecuteReader();

        if (dataSelectExplicitTopics.HasRows)
            {
            while (dataSelectExplicitTopics.Read())
                {
                lstExplicitTopics.Add(dataSelectExplicitTopics.GetInt32(0));
                }
            }

        return lstExplicitTopics;
        }

        
        
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static void UpdateRibbonTimestamp(ref NpgsqlConnection conn, int nIdUser, string strCoords, string strLocale)
        {
        NpgsqlCommand commandUpdateRibbonTimestamp = new NpgsqlCommand();
        commandUpdateRibbonTimestamp.Connection = conn;

        commandUpdateRibbonTimestamp.CommandText = @"UPDATE db_users SET last_ribbon_timestamp = current_timestamp, last_ribbon_coords = :coords, last_ribbon_locale = :loc, last_ribbon_timestamp_notification = current_timestamp 
                                                     WHERE id_user = :idu;";

        commandUpdateRibbonTimestamp.Parameters.Add(new NpgsqlParameter("coords", DbType.String));
        commandUpdateRibbonTimestamp.Parameters.Add(new NpgsqlParameter("loc", DbType.String));
        commandUpdateRibbonTimestamp.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));

        commandUpdateRibbonTimestamp.Prepare();

        if (strCoords != null && strCoords.Length != 0)
            commandUpdateRibbonTimestamp.Parameters[0].Value = strCoords;
        else
            commandUpdateRibbonTimestamp.Parameters[0].Value = null;

        commandUpdateRibbonTimestamp.Parameters[1].Value = strLocale;
        commandUpdateRibbonTimestamp.Parameters[2].Value = nIdUser;

        commandUpdateRibbonTimestamp.ExecuteNonQuery();

        return;
        }


        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static bool IsEmailValid(string strEmail)
        {
        //regular expression pattern for valid email addresses, allows for the following domains:
        //com,edu,info,gov,int,mil,net,org,biz,name,museum,coop,aero,pro,tv
        string pattern = @"^[-a-zA-Z0-9_][-.a-zA-Z0-9_]*@[-.a-zA-Z0-9_]+(\.[-.a-zA-Z0-9]+)*\.(com|edu|info|gov|int|mil|net|org|biz|name|museum|coop|aero|pro|tv|[a-zA-Z]{2,})$";
        
        Regex check = new Regex(pattern,RegexOptions.IgnorePatternWhitespace);
        
        bool valid = false;

        if (string.IsNullOrEmpty(strEmail))
            {
            valid = false;
            }
        else
            {
            valid = check.IsMatch(strEmail);
            }

        return valid;
        }



        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static bool RenewUserTopics(int nIdUser, ref  NpgsqlConnection conn)
        {
        NpgsqlTransaction trans = null;

        NpgsqlCommand commandMinMax = new NpgsqlCommand();
        commandMinMax.Connection = conn;
        commandMinMax.Transaction = trans;
        commandMinMax.CommandText = @"SELECT MIN(inn.cnt), MAX (inn.cnt) FROM 
							(SELECT COUNT(id_record) cnt, id_topic FROM dbl_record_topics GROUP BY id_topic ORDER BY cnt) AS inn;";
        commandMinMax.Prepare();


        NpgsqlCommand commandDeleteU = new NpgsqlCommand();
        commandDeleteU.Connection = conn;
        commandDeleteU.Transaction = trans;
        commandDeleteU.CommandText = "DELETE FROM dbl_user_topics_3 WHERE id_user = :idu;";
        commandDeleteU.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandDeleteU.Prepare();


        NpgsqlCommand commandTopics = new NpgsqlCommand();
        commandTopics.Connection = conn;
        commandTopics.Transaction = trans;
        commandTopics.CommandText = @"SELECT DISTINCT id_topic FROM (
						SELECT id_topic FROM dbl_record_topics WHERE id_record IN (SELECT id_record FROM db_record_likes WHERE id_user = :idu AND rating <> 0)
						UNION
						SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND explicit = TRUE) AS foo ORDER BY id_topic;";
        commandTopics.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandTopics.Prepare();


        NpgsqlCommand commandTopicCount = new NpgsqlCommand();
        commandTopicCount.Connection = conn;
        commandTopicCount.Transaction = trans;
        commandTopicCount.CommandText = @"SELECT COALESCE(counter, 0) FROM db_topic_ties WHERE id_topic_1 = :idt AND id_topic_2 = :idt;";
        commandTopicCount.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandTopicCount.Prepare();


        NpgsqlCommand commandLikes = new NpgsqlCommand();
        commandLikes.Connection = conn;
        commandLikes.Transaction = trans;
        commandLikes.CommandText = @"SELECT COUNT(*), first(db_record_likes.rating) FROM dbl_record_topics 
						INNER JOIN db_record_likes ON dbl_record_topics.id_record = db_record_likes.id_record AND db_record_likes.id_user=:idu AND db_record_likes.rating <> 0 AND dbl_record_topics.id_topic=:idt
						GROUP BY db_record_likes.rating ORDER BY db_record_likes.rating;";
        commandLikes.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandLikes.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandLikes.Prepare();


        NpgsqlCommand commandExpl = new NpgsqlCommand();
        commandExpl.Connection = conn;
        commandExpl.Transaction = trans;
        commandExpl.CommandText = "SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND id_topic = :idt AND explicit = TRUE;";
        commandExpl.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandExpl.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandExpl.Prepare();


        NpgsqlCommand commandInsert3 = new NpgsqlCommand();
        commandInsert3.Connection = conn;
        commandInsert3.Transaction = trans;
        commandInsert3.CommandText = "INSERT INTO dbl_user_topics_3 (id_user, id_topic, weight, update_timestamp, lim, summ) VALUES(:idu, :idt, :w, current_timestamp, :l, :s);";
        commandInsert3.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandInsert3.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandInsert3.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        commandInsert3.Parameters.Add(new NpgsqlParameter("l", DbType.Double));
        commandInsert3.Parameters.Add(new NpgsqlParameter("s", DbType.Double));
        commandInsert3.Prepare();

        NpgsqlCommand commandUpdate3 = new NpgsqlCommand();
        commandUpdate3.Connection = conn;
        commandUpdate3.Transaction = trans;
        commandUpdate3.CommandText = "UPDATE dbl_user_topics_3 SET weight = :w, update_timestamp = current_timestamp, lim = :l, summ = :s WHERE id_user = :idu AND id_topic = :idt;";
        commandUpdate3.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("l", DbType.Double));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("s", DbType.Double));
        commandUpdate3.Prepare();

        const int MIN_RATINGS_COUNT = 10;
        const int MAX_RATINGS_COUNT = 50;


        int nMinC = 0;
        int nMaxC = 0;


        NpgsqlDataReader data_minmax = commandMinMax.ExecuteReader();

        if (data_minmax.HasRows)
            {
            data_minmax.Read();
                {
                nMinC = (int)data_minmax.GetInt64(0);
                nMaxC = (int)data_minmax.GetInt64(1);
                }
            }


        commandDeleteU.Parameters[0].Value = nIdUser;
        int nDeleted = commandDeleteU.ExecuteNonQuery();

        List<int> lstTopics = new List<int>();

        commandTopics.Parameters[0].Value = nIdUser;
        NpgsqlDataReader data_tp = commandTopics.ExecuteReader();

        if (data_tp.HasRows)
            {
            while (data_tp.Read())
                {
                int nIdTopic = data_tp.GetInt32(0);
                lstTopics.Add(nIdTopic);
                }
            }


        foreach (int nTopicId in lstTopics)
            {
            int nCurrentTopicCount = 0;

            commandTopicCount.Parameters[0].Value = nTopicId;

            try
                {
                nCurrentTopicCount = (int)commandTopicCount.ExecuteScalar();
                }
            catch { }

            double dblTopicLimit = MIN_RATINGS_COUNT + (MAX_RATINGS_COUNT - MIN_RATINGS_COUNT) * (double)(nCurrentTopicCount - nMinC) / (double)(nMaxC - nMinC);

            commandLikes.Parameters[0].Value = nIdUser;
            commandLikes.Parameters[1].Value = nTopicId;

            NpgsqlDataReader data = commandLikes.ExecuteReader();

            int nSumLikes = 0;
            int nSumUnlikes = 0;

            if (data.HasRows)
                {
                while (data.Read())
                    {
                    string strType = data.GetDataTypeName(0);

                    int nCount = (int)data.GetInt64(0);
                    int nRating = data.GetInt32(1);

                    if (nRating == 1)
                        nSumLikes = nCount;
                    else if (nRating == -1)
                        nSumUnlikes = nCount;
                    }
                }

            bool bIsTopicExplicit = false;

            commandExpl.Parameters[0].Value = nIdUser;
            commandExpl.Parameters[1].Value = nTopicId;

            NpgsqlDataReader data_expl = commandExpl.ExecuteReader();

            if (data_expl.HasRows)
                {
                bIsTopicExplicit = true;
                }


            int nSum = nSumLikes + nSumUnlikes;

            double dblTopicWeight = 0;

            if (nSum >= dblTopicLimit)
                {
                if (nSum > 0)
                    dblTopicWeight = (double)(nSumLikes - nSumUnlikes) / nSum;
                else
                    dblTopicWeight = 0;
                }
            else
                {
                if (bIsTopicExplicit)
                    dblTopicWeight = 1;
                else
                    dblTopicWeight = 0;
                }

            commandInsert3.Parameters[0].Value = nIdUser;
            commandInsert3.Parameters[1].Value = nTopicId;
            commandInsert3.Parameters[2].Value = dblTopicWeight;
            commandInsert3.Parameters[3].Value = dblTopicLimit;
            commandInsert3.Parameters[4].Value = nSum;

            try
                {
                commandInsert3.ExecuteNonQuery();
                }
            catch
                {
                commandUpdate3.Parameters[0].Value = nIdUser;
                commandUpdate3.Parameters[1].Value = nTopicId;
                commandUpdate3.Parameters[2].Value = dblTopicWeight;
                commandUpdate3.Parameters[3].Value = dblTopicLimit;
                commandUpdate3.Parameters[4].Value = nSum;

                commandUpdate3.ExecuteNonQuery();
                }
            }

        return true;
        }


        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static bool RenewUserTopics3(int nIdUser, ref  NpgsqlConnection conn)
        {
        NpgsqlCommand commandMinMax = new NpgsqlCommand();
        commandMinMax.Connection = conn;
        //commandMinMax.Transaction = trans;
        commandMinMax.CommandText = @"SELECT MIN(inn.cnt), MAX (inn.cnt) FROM 
                                        (SELECT COUNT(id_record) cnt, id_topic FROM dbl_record_topics GROUP BY id_topic ORDER BY cnt) AS inn;";
        commandMinMax.Prepare();



        NpgsqlCommand commandDeleteU = new NpgsqlCommand();
        commandDeleteU.Connection = conn;
        //commandDeleteU.Transaction = trans;
        commandDeleteU.CommandText = "DELETE FROM dbl_user_topics_3 WHERE id_user = :idu AND id_topic = :idt;";
        commandDeleteU.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandDeleteU.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandDeleteU.Prepare();


        NpgsqlCommand commandTopics = new NpgsqlCommand();
        commandTopics.Connection = conn;
        //commandTopics.Transaction = trans;
        commandTopics.CommandText = @"SELECT DISTINCT id_topic FROM (
                                    SELECT id_topic FROM dbl_record_topics WHERE id_record IN (SELECT id_record FROM db_record_likes WHERE id_user = :idu AND rating <> 0)
                                    UNION
                                    SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND explicit = TRUE) AS foo ORDER BY id_topic;";
        commandTopics.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandTopics.Prepare();


        NpgsqlCommand commandOldTopics = new NpgsqlCommand();
        commandOldTopics.Connection = conn;
        commandOldTopics.CommandText = @"SELECT id_topic, weight FROM dbl_user_topics_3 WHERE id_user = :idu";
        commandOldTopics.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandOldTopics.Prepare();



        NpgsqlCommand commandTopicCount = new NpgsqlCommand();
        commandTopicCount.Connection = conn;
        //commandTopicCount.Transaction = trans;
        commandTopicCount.CommandText = @"SELECT COALESCE(counter, 0) FROM db_topic_ties WHERE id_topic_1 = :idt AND id_topic_2 = :idt;";
        commandTopicCount.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandTopicCount.Prepare();


        NpgsqlCommand commandLikes = new NpgsqlCommand();
        commandLikes.Connection = conn;
        //commandLikes.Transaction = trans;
        commandLikes.CommandText = @"SELECT COUNT(*), first(db_record_likes.rating) FROM dbl_record_topics 
                                    INNER JOIN db_record_likes ON dbl_record_topics.id_record = db_record_likes.id_record AND db_record_likes.id_user=:idu AND db_record_likes.rating <> 0 AND dbl_record_topics.id_topic=:idt
                                    GROUP BY db_record_likes.rating ORDER BY db_record_likes.rating;";
        commandLikes.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandLikes.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandLikes.Prepare();


        NpgsqlCommand commandAllExpl = new NpgsqlCommand();
        commandAllExpl.Connection = conn;
        //commandAllExpl.Transaction = trans;
        commandAllExpl.CommandText = "SELECT id_topic FROM dbl_user_topics WHERE id_user = :idu AND explicit = TRUE;";
        commandAllExpl.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandAllExpl.Prepare();



        NpgsqlCommand commandInsert3 = new NpgsqlCommand();
        commandInsert3.Connection = conn;
        //commandInsert3.Transaction = trans;
        commandInsert3.CommandText = "INSERT INTO dbl_user_topics_3 (id_user, id_topic, weight, update_timestamp, lim, summ) VALUES(:idu, :idt, :w, current_timestamp, :l, :s);";
        commandInsert3.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandInsert3.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandInsert3.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        commandInsert3.Parameters.Add(new NpgsqlParameter("l", DbType.Double));
        commandInsert3.Parameters.Add(new NpgsqlParameter("s", DbType.Double));
        commandInsert3.Prepare();


        NpgsqlCommand commandUpdate3 = new NpgsqlCommand();
        commandUpdate3.Connection = conn;
        //commandUpdate3.Transaction = trans;
        commandUpdate3.CommandText = "UPDATE dbl_user_topics_3 SET weight = :w, update_timestamp = current_timestamp, lim = :l, summ = :s WHERE id_user = :idu AND id_topic = :idt;";
        commandUpdate3.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("w", DbType.Double));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("l", DbType.Double));
        commandUpdate3.Parameters.Add(new NpgsqlParameter("s", DbType.Double));
        commandUpdate3.Prepare();

        NpgsqlCommand commandAllLikes = new NpgsqlCommand();
        commandAllLikes.Connection = conn;
        commandAllLikes.CommandText = @"SELECT first(rt.id_topic) idt, COUNT(rl.rating) ct, -1 rt FROM dbl_record_topics AS rt
                                        INNER JOIN db_record_likes AS rl ON rt.id_record = rl.id_record AND rl.id_user = {0} AND rt.id_topic IN ({1}) AND rl.rating =-1
                                        GROUP BY rt.id_topic
                                        UNION
                                        SELECT first(rt.id_topic), COUNT(rl.rating), 1 FROM dbl_record_topics AS rt
                                        INNER JOIN db_record_likes AS rl ON rt.id_record = rl.id_record AND rl.id_user = {0} AND rt.id_topic IN ({1}) AND rl.rating =1
                                        GROUP BY rt.id_topic;";


        const int MIN_RATINGS_COUNT = 10;
        const int MAX_RATINGS_COUNT = 50;


        int nMinC = 0;
        int nMaxC = 0;

        //strComment += "Mark 5; ";


        NpgsqlDataReader data_minmax = commandMinMax.ExecuteReader();

        if (data_minmax.HasRows)
            {
            data_minmax.Read();
                {
                nMinC = (int)data_minmax.GetInt64(0);
                nMaxC = (int)data_minmax.GetInt64(1);
                }
            }


        //strComment += "Mark 6; ";


        Dictionary<int, double> dictOldWeights = new Dictionary<int, double>();

        commandOldTopics.Parameters[0].Value = nIdUser;

        NpgsqlDataReader dataOldTopics = commandOldTopics.ExecuteReader();

        if (dataOldTopics.HasRows)
            {
            while (dataOldTopics.Read())
                {
                int nIdTopic = dataOldTopics.GetInt32(0);
                double dblWeight = (double)dataOldTopics.GetDouble(1);

                dictOldWeights.Add(nIdTopic, dblWeight);
                }
            }

        //strComment += "Mark 7; ";


        List<int> lstTopics = new List<int>();

        commandTopics.Parameters[0].Value = nIdUser;
        NpgsqlDataReader data_tp = commandTopics.ExecuteReader();

        if (data_tp.HasRows)
            {
            while (data_tp.Read())
                {
                int nIdTopic = data_tp.GetInt32(0);
                lstTopics.Add(nIdTopic);
                }
            }

        Dictionary<int, int> dictTopicLikes = new Dictionary<int, int>();
        Dictionary<int, int> dictTopicUnlikes = new Dictionary<int, int>();

        if(lstTopics.Count > 0)
            { 
            string strTopics = lstTopics[0].ToString();

            for (int ii = 1; ii < lstTopics.Count(); ++ii)
                {
                strTopics += ", " + lstTopics[ii].ToString();
                }
            
            commandAllLikes.CommandText = String.Format(commandAllLikes.CommandText, nIdUser.ToString(), strTopics);

            commandAllLikes.Prepare();

            NpgsqlDataReader data_all_likes = commandAllLikes.ExecuteReader();

            if (data_all_likes.HasRows)
                {
                while (data_all_likes.Read())
                    {
                    int nIdTopic = data_all_likes.GetInt32(0);
                    int nCount = (int)data_all_likes.GetInt64(1);
                    int nRating = data_all_likes.GetInt32(2);

                    if(nRating == 1)
                        dictTopicLikes.Add(nIdTopic, nCount);
                    else if(nRating == -1)
                        dictTopicUnlikes.Add(nIdTopic, nCount);

                    }
                }
            }

        //strComment += "Mark 8; ";

        SortedSet<int> setExplTopics = new SortedSet<int>();

        commandAllExpl.Parameters[0].Value = nIdUser;

        NpgsqlDataReader data_all_expl = commandAllExpl.ExecuteReader();

        if (data_all_expl.HasRows)
            {
            while (data_all_expl.Read())
                {
                int nId = data_all_expl.GetInt32(0);

                setExplTopics.Add(nId);
                }
            }

        //strComment += "Mark 9; ";

        foreach (int nTopicId in lstTopics)
            {
            int nCurrentTopicCount = 0;

            commandTopicCount.Parameters[0].Value = nTopicId;

            try
                {
                nCurrentTopicCount = (int)commandTopicCount.ExecuteScalar();
                }
            catch { }

            double dblTopicLimit = MIN_RATINGS_COUNT + (MAX_RATINGS_COUNT - MIN_RATINGS_COUNT) * (double)(nCurrentTopicCount - nMinC) / (double)(nMaxC - nMinC);


            int nSumLikes = 0;
            int nSumUnlikes = 0;

            if(dictTopicLikes.ContainsKey(nTopicId))
                {
                nSumLikes = dictTopicLikes[nTopicId];
                }

            if (dictTopicUnlikes.ContainsKey(nTopicId))
                {
                nSumUnlikes = dictTopicUnlikes[nTopicId];
                }


            bool bIsTopicExplicit = false;

            if (setExplTopics.Contains(nTopicId))
                {
                bIsTopicExplicit = true;
                }


            int nSum = nSumLikes + nSumUnlikes;

            double dblTopicWeight = 0;

            if (nSum >= dblTopicLimit)
                {
                if (nSum > 0)
                    dblTopicWeight = (double)(nSumLikes - nSumUnlikes) / nSum;
                else
                    dblTopicWeight = 0;
                }
            else
                {
                if (bIsTopicExplicit)
                    dblTopicWeight = 1;
                else
                    dblTopicWeight = 0;
                }

            if (dictOldWeights.ContainsKey(nTopicId))
                {
                if (dictOldWeights[nTopicId] != dblTopicWeight)
                    {
                    commandUpdate3.Parameters[0].Value = nIdUser;
                    commandUpdate3.Parameters[1].Value = nTopicId;
                    commandUpdate3.Parameters[2].Value = dblTopicWeight;
                    commandUpdate3.Parameters[3].Value = dblTopicLimit;
                    commandUpdate3.Parameters[4].Value = nSum;

                    commandUpdate3.ExecuteNonQuery();
                    }

                dictOldWeights.Remove(nTopicId);
                }
            else
                {
                commandInsert3.Parameters[0].Value = nIdUser;
                commandInsert3.Parameters[1].Value = nTopicId;
                commandInsert3.Parameters[2].Value = dblTopicWeight;
                commandInsert3.Parameters[3].Value = dblTopicLimit;
                commandInsert3.Parameters[4].Value = nSum;


                try
                    { 
                    commandInsert3.ExecuteNonQuery();
                    }
                catch // для случая асинхронного вызова RateRecord, если запись отсутствует в dictOldWeights, но уже есть в базе
                    {

                    commandUpdate3.Parameters[0].Value = nIdUser;
                    commandUpdate3.Parameters[1].Value = nTopicId;
                    commandUpdate3.Parameters[2].Value = dblTopicWeight;
                    commandUpdate3.Parameters[3].Value = dblTopicLimit;
                    commandUpdate3.Parameters[4].Value = nSum;

                    try
                    { 
                    commandUpdate3.ExecuteNonQuery();
                    }
                    catch {}
                    }
                }
            }

        //strComment += "Mark 10; ";

        foreach (int nTopicId in dictOldWeights.Keys)
            {
            commandDeleteU.Parameters[0].Value = nIdUser;
            commandDeleteU.Parameters[1].Value = nTopicId;

            commandDeleteU.ExecuteNonQuery();
            }


        //if (trans != null)
        //    trans.Commit();

        return true;
        }




        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        //public static bool RenewRecommenderModelForUser(int nIdUser, ref  NpgsqlConnection conn, ref BPRSLIM recommender, ref HttpApplicationState app)
        //{
        //NpgsqlCommand commandUser = new NpgsqlCommand();
        //commandUser.Connection = conn;

        //bool bRes = false;

        //try
        //    {
        //    commandUser.CommandText = @"SELECT id_topic FROM dbl_user_topics_3 WHERE id_user = :idu AND weight > 0.5;";
        //    commandUser.Parameters.Add(new NpgsqlParameter("idu", DbType.Int32));

        //    commandUser.Prepare();

        //    commandUser.Parameters[0].Value = nIdUser;

        //    NpgsqlDataReader dataUser = commandUser.ExecuteReader();

        //    List<Tuple<int, int>> lstTrainingData = new List<Tuple<int, int>>();

        //    if (dataUser.HasRows)
        //        {
        //        while (dataUser.Read())
        //            {
        //            int nIdTopic = dataUser.GetInt32(0);

        //            Tuple<int, int> tpl = new Tuple<int, int>(nIdUser, nIdTopic);
        //            lstTrainingData.Add(tpl);
        //            }
        //        }

        //    if(recommender != null)
        //        {
        //        recommender.RemoveUser(nIdUser);
        //        recommender.AddFeedback(lstTrainingData);
                
        //        app["FavoramServer.Recommender.TrainTimestamp"] = DateTime.Now;

        //        bRes = true;
        //        }
        //    }
        //catch
        //    {
        //    bRes = false;
        //    }


        //return bRes;
        //}





        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        //public static void AddLinkedTopics(ref NpgsqlConnection conn, int nIdTopic, ref SortedSet<int> setNewTopics)
        //{
        //NpgsqlCommand command = new NpgsqlCommand();
        //command.Connection = conn;
        //command.CommandText = "SELECT id_topic, id_parent FROM db_topic_groups WHERE id_topic = :idt;";
        //command.Parameters.Add(new NpgsqlParameter("idt", DbType.Int32));
        //command.Prepare();

        //NpgsqlCommand command_2 = new NpgsqlCommand();
        //command_2.Connection = conn;
        //command_2.CommandText = "SELECT id_topic FROM db_topic_groups WHERE id_parent = :idp;";
        //command_2.Parameters.Add(new NpgsqlParameter("idp", DbType.Int32));
        //command_2.Prepare();

        //command.Parameters[0].Value = nIdTopic;

        //NpgsqlDataReader data = command.ExecuteReader();

        //if (data.HasRows)
        //    {
        //    while (data.Read())
        //        {
        //        int nIdT = data.GetInt32(0);
        //        int nIdParent = data.GetInt32(1);

        //        if (nIdParent == 0)
        //            {
        //            command_2.Parameters[0].Value = nIdTopic;
        //            NpgsqlDataReader data_2 = command_2.ExecuteReader();

        //            if (data_2.HasRows)
        //                {
        //                while (data_2.Read())
        //                    {
        //                    int nIdTopic_2 = data_2.GetInt32(0);

        //                    if (nIdTopic != nIdTopic_2 && !setNewTopics.Contains(nIdTopic_2))
        //                        setNewTopics.Add(nIdTopic_2);
        //                    }
        //                }
        //            }
        //        else
        //            {
        //            command_2.Parameters[0].Value = nIdParent;
        //            NpgsqlDataReader data_2 = command_2.ExecuteReader();

        //            if (data_2.HasRows)
        //                {
        //                while (data_2.Read())
        //                    {
        //                    int nIdTopic_2 = data_2.GetInt32(0);

        //                    if (nIdTopic != nIdTopic_2 && !setNewTopics.Contains(nIdTopic_2))
        //                        setNewTopics.Add(nIdTopic_2);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        
        
        //+///////////////////////////////////////////////////////////////////////
        //public static void RecommenderTrain(ref NpgsqlConnection conn)
        //{
        //NpgsqlCommand commandUsers = new NpgsqlCommand();
        //commandUsers.Connection = conn;

        //commandUsers.CommandText = @"SELECT id_user, id_topic FROM dbl_user_topics_3 WHERE weight > 0.5;";
        //commandUsers.Prepare();

        //NpgsqlDataReader dataUsers = commandUsers.ExecuteReader();

        //var user_mapping = new IdentityMapping();
        //var item_mapping = new IdentityMapping();

        //var training_data = ItemData.Read((IDataReader)dataUsers, user_mapping, item_mapping);

        //var recommender = new BPRSLIM();
        //recommender.Feedback = training_data;
        //recommender.Train();

        ////recommender.SaveModel("model.mdl");
        //recommender.SaveModel("/var/www/main2/bin/model.mdl");
        //}



        //+///////////////////////////////////////////////////////////////////////
        public static void DeleteMarkedUsers(ref NpgsqlConnection conn)
        {
        NpgsqlCommand commandDeleteMarked = new NpgsqlCommand();
        commandDeleteMarked.Connection = conn;
        commandDeleteMarked.CommandText = "UPDATE db_users SET deleted = TRUE WHERE current_timestamp >= (delete_mark_timestamp + INTERVAL '10' DAY);";
        commandDeleteMarked.Prepare();

        commandDeleteMarked.ExecuteNonQuery();
        }



        
        //////////////////////////////////////////////////////////////////////////
        public static double GetAvrImageBrightness(Byte[] arrPhoto, double dblFraction)
        {
        double dblBright = 0.5;

        try
            {
            MemoryStream ms = new MemoryStream(arrPhoto);
            Image img = Image.FromStream(ms);


            Bitmap bmpOrig = (Bitmap)img;

            PixelFormat fmt = bmpOrig.PixelFormat;

            int nWidth = bmpOrig.Width;
            int nHeight = bmpOrig.Height;

            Rectangle rectStrip = new Rectangle(0, 0, nWidth, (int)(nHeight * dblFraction));

            Bitmap bmpStrip = bmpOrig.Clone(rectStrip, PixelFormat.Format24bppRgb);
            bmpOrig = null;

            Bitmap bmpSingle = (Bitmap)ResizeImg(bmpStrip, 1, 1);

            Color clr = bmpSingle.GetPixel(0, 0);

            dblBright = (clr.R + clr.G + clr.B) / 3.0 / 255;
            }
        catch { }

        return dblBright;
        }


        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        public static double GetAvrImageRectBrightness(ref byte[] arrImg, int nX, int nY, int nWidth, int nHeight)
        {
        double dblBright = 0.5;

        try
            {
            MemoryStream ms = new MemoryStream(arrImg);
            Image img = Image.FromStream(ms);


            Bitmap bmpOrig = (Bitmap)img;

            PixelFormat fmt = bmpOrig.PixelFormat;

            int nW = bmpOrig.Width;
            int nH = bmpOrig.Height;

            if(nX < 0 || nY < 0 || nWidth <= 0 || nHeight <= 0 || nX + nWidth > nW || nY + nHeight > nH)
                return dblBright;

            Rectangle rectStrip = new Rectangle(nX, nY, nWidth, nHeight);

            Bitmap bmpStrip = bmpOrig.Clone(rectStrip, PixelFormat.Format24bppRgb);
            bmpOrig = null;

            Bitmap bmpSingle = (Bitmap)ResizeImg(bmpStrip, 1, 1);

            Color clr = bmpSingle.GetPixel(0, 0);

            //dblBright = (clr.R + clr.G + clr.B) / 3.0 / 255;
            dblBright = (clr.R * 0.3 + clr.G * 0.59 + clr.B * 0.11) / 255;

            if(dblBright > 1.0)
                dblBright = 1.0;


            }
        catch { }

        return dblBright;
        }




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
        public static string GetEventSiteUrl(int nIdEvent, string strTitle)
        {
        string strUrl = "http://favoraim.com/event" + nIdEvent.ToString() + "-" + TransliterateString(strTitle);

        return strUrl;
        }

        
        


        ////////////////////////////////////////////////////////////////////////
        public static int GetIdRegionForIP(ref NpgsqlConnection conn, string strIP, ref string strCountryCode)
        {
        int nIdRegion = -1;
        string strRegionCode = "";

        try
            {
            bool bIpExists = false;
            bool bIpObsolete = false;

            NpgsqlCommand command = new NpgsqlCommand();
            command.Connection = conn;

            command.CommandText = @"SELECT id_region, ts, region_code FROM db_ip_region WHERE ip = :ipa;";
            command.Parameters.Add(new NpgsqlParameter("ipa", DbType.String));

            command.Prepare();

            command.Parameters[0].Value = strIP;

            NpgsqlDataReader data = command.ExecuteReader();

            if (data.HasRows)
                {
                bIpExists = true;

                data.Read();

                if(!data.IsDBNull(0))
                    nIdRegion = data.GetInt32(0);

                DateTime ts = data.GetDateTime(1);

                if(ts < DateTime.Now - new TimeSpan(30, 0, 0, 0))
                    bIpObsolete = true;

                strRegionCode = data.GetString(2);
                }
            
            
            if(!bIpExists || bIpObsolete)
                {
                strRegionCode = ServUtility.GetRegionCodeForIP(strIP);

                if(strRegionCode.Length > 0)
                    {
                    if(!bIpExists)
                        {
                        NpgsqlCommand commandInsert = new NpgsqlCommand();
                        commandInsert.Connection = conn;

                        commandInsert.CommandText = @"INSERT INTO db_ip_region(ip, id_region, region_code) 
                                                      VALUES (:ipa, (SELECT id_region FROM db_regions WHERE region_code = :rc), :rc)
                                                      RETURNING id_region;";
                        
                        commandInsert.Parameters.Add(new NpgsqlParameter("ipa", DbType.String));
                        commandInsert.Parameters.Add(new NpgsqlParameter("rc", DbType.String));

                        commandInsert.Prepare();

                        commandInsert.Parameters[0].Value = strIP;
                        commandInsert.Parameters[1].Value = strRegionCode;

                        object objId = commandInsert.ExecuteScalar();

                        if (objId != null)
                            nIdRegion = (int)objId;
                        }
                    else
                        {
                        NpgsqlCommand commandUpdate = new NpgsqlCommand();
                        commandUpdate.Connection = conn;

                        commandUpdate.CommandText = @"UPDATE db_ip_region SET id_region = (SELECT id_region FROM db_regions WHERE region_code = :rc), ts = current_timestamp, region_code = :rc 
                                                      WHERE ip = :ipa
                                                      RETURNING id_region;";

                        commandUpdate.Parameters.Add(new NpgsqlParameter("ipa", DbType.String));
                        commandUpdate.Parameters.Add(new NpgsqlParameter("rc", DbType.String));

                        commandUpdate.Prepare();

                        commandUpdate.Parameters[0].Value = strIP;
                        commandUpdate.Parameters[1].Value = strRegionCode;

                        object objId = commandUpdate.ExecuteScalar();

                        if (objId != null)
                            nIdRegion = (int)objId;
                        }

                    }
                }
            else if(bIpExists && !bIpObsolete && nIdRegion == -1)
                {
                NpgsqlCommand commandUpdate = new NpgsqlCommand();
                commandUpdate.Connection = conn;

                commandUpdate.CommandText = @"UPDATE db_ip_region SET id_region = (SELECT id_region FROM db_regions WHERE region_code = :rc) WHERE ip = :ipa RETURNING id_region;";

                commandUpdate.Parameters.Add(new NpgsqlParameter("ipa", DbType.String));
                commandUpdate.Parameters.Add(new NpgsqlParameter("rc", DbType.String));

                commandUpdate.Prepare();

                commandUpdate.Parameters[0].Value = strIP;
                commandUpdate.Parameters[1].Value = strRegionCode;

                object objId = commandUpdate.ExecuteScalar();

                if (objId != null)
                    nIdRegion = (int)objId;
                }

            }
        catch
            {
            return nIdRegion;
            }
        finally
            {
            if(strRegionCode.Length > 0)
                {
                string[] arrSplit = strRegionCode.Split(new Char[] { ':' });

                if(arrSplit.GetLength(0) > 0)
                    strCountryCode = arrSplit[0];
                }

            }

        return nIdRegion;
        }



        
        ////////////////////////////////////////////////////////////////////////
        public static string GetRegionCodeForIP(string strIP)
        {
        string strRegionCode = "";

        try
            {
            ServicePointManager.ServerCertificateValidationCallback = Validator;

            string strGeoIPId = ReadStringFromConfig("geoip_id");
            string strGeoIPKey = ReadStringFromConfig("geoip_key");

            string post_data = "";
            //post_data = String.Format(post_data, strToken, strCurrency, nAmount);
            byte[] postBytes = Encoding.ASCII.GetBytes(post_data);

            string uri = "https://geoip.maxmind.com/geoip/v2.0/city_isp_org/{0}?pretty";
            uri = String.Format(uri, strIP);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "GET";

            //string strCredent = "90905:MWSANhcwlqAt";
            string strCredent = strGeoIPId + ":" + strGeoIPKey;

            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(strCredent);

            strCredent = System.Convert.ToBase64String(toEncodeAsBytes);
            strCredent = "Basic " + strCredent;

            request.Headers.Add("Authorization", strCredent);


            request.ContentType = "application/vnd.maxmind.com-city-isp-org+json; charset=UTF-8; version=2.0";
            request.ContentLength = postBytes.Length;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string strResponse = new StreamReader(response.GetResponseStream()).ReadToEnd();

            MemoryStream s = new MemoryStream(Encoding.UTF8.GetBytes(strResponse));

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(GeoIPResponse));

            object objResponse = jsonSerializer.ReadObject(s);

            GeoIPResponse jsonResp = objResponse as GeoIPResponse;

            strRegionCode = jsonResp.country.iso_code + "-";
            strRegionCode += jsonResp.subdivisions[0].iso_code;

            return strRegionCode;
            }
        catch
            {
            return strRegionCode;
            }
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

