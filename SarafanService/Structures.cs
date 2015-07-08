using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using System.Runtime.InteropServices;

namespace SarafanService
{

	public enum ResultCode
	{
	Success = 0,
	Failure_InvalidInputJson,
    Failure_InternalServiceError,
    Failure_SessionExpired
    }

	public class StructResult
	{
    public StructResult()
        {
        result_code = ResultCode.Success;
        message = "";
        }

	public ResultCode result_code;
	public string message;

    public override string ToString()
    {
    return "{" + result_code.ToString() + "} " + message;
    }

	}


public struct StructGetRetailersArgs
    {
    public string locale;
    }

public struct StructGetFiltersArgs
    {
    public List<int> retailer_id_list;
    public string locale;
    }
public struct StructPutModelPictureArgs
    {
    public string picture;
    public string locale;
    }


public struct StructComparePicturesArgs
    {
    public string picture1;
    public string picture2;
    public string locale;
    }

public struct StructFindProductsArgs2
    {
    public int picture_id;
    public List<int> retailer_id_list;
    public List<StructFilterItemPair2> filter_item_list;
    public string locale;
    }

public struct StructFilterItemPair2
    {
    public int id_filter;
    public int id_item;
    }


public struct StructGetProductInfoArgs
    {
    public int find_request_id;
    public int max_items;
    public int offset;
    public string locale;
    }


public struct StructGetModelPictureArgs
    {
    public int id_model_picture;
    public string locale;
    }

public struct StructFindSimiliarProductsArgs
    {
    public string offer_id;
    public int id_retailer;
    public string locale;
    }

///////////////////////////////////////////////////////////////

public struct StructPicturesCompared
    {
    public double distance;

    public float[] descs_1;
    public float[] descs_2;

    public StructResult result;
    }



public struct StructRetailers
    {
    public List<StructRetailer> retailers;

    public StructResult result;
    }

public struct StructRetailer
    {
    public int id;
    public string name;
    public string description;
    public string picture;
    }


public struct StructModelPictureId
    {
    public int model_picture_id;

    public StructResult result;
    }


public struct StructFindRequestId
    {
    public int find_request_id;
    public int num_products_found;

    public string dbg1;
    public string dbg2;
    public string dbg3;

    public StructResult result;
    }


public struct StructFindSimiliarResult
    {
    public List<StructSimiliarProduct> offers;

    public string dbg1;
    public string dbg2;
    //public string dbg3;

    public StructResult result;
    }

public struct StructSimiliarProduct
    {
    public string offer_id;
    public string name;
    public string image_url;
    public string buy_url;
    public double dom_dist;
    }



public struct StructProductIds
    {
    public List<int> product_ids;
    public int list_size;

    public StructResult result;
    }

public struct StructProducts
    {
    public List<StructProduct> products;

    //public string sql;

    //public string strExc;

    public StructResult result;
    }

public struct StructProduct
    {
    public int id;
    public int id_search_result;
    public string brand_name;
    public string name;
    public string description;
    public string buyurl;
    public string picture;
    public int picture_width;
    public int picture_height;
    public string currency;
    public double price;
    public int retailer_id;
    public double distance;
    public double distance_dom;
    public double distance_bow;
    //public string most_relevant_picture;
    //public string picture_remote;
    public List<StructPicture> pictures;
    }

public struct StructPicture
    {
    public int id;
    public string url;
    public int width;
    public int height;
    }

public struct StructFilters
    {
    public List<StructFilter> filters;

    public StructResult result;
    }

public struct StructFilter
    {
    public int id;
    public string name;
    public string type;
    public bool multiselect;
    public bool required;
    public List<StructFilterItem> items;
    }

public struct StructFilterItem
    {
    public int item_id;
    public string item_name;
    }

public struct StructModelPicture
    {
    public int id_model_picture;
    public string url;
    //public int width;
    //public int height;

    public StructResult result;
    }



public struct StructDescriptors
    {
    public float[] descs_1;
    public float[] descs_2;

    public string dbg;

    public StructResult result;
    }

[StructLayout(LayoutKind.Sequential)]
public struct StructColorImage
    {
    public int width;
    public int height;

    public float[] c1;
    public float[] c2;
    public float[] c3;
    };

[StructLayout(LayoutKind.Sequential)]
public struct StructBWImage
    {
    public int width;
    public int height;

    public float[] c1;
    };


}

