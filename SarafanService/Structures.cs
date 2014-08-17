using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;


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


//public struct StructFindProductsArgs
//    {
//    public int picture_id;
//    public List<int> retailer_id_list;
//    public List<StructFilterItemPair> filter_item_list;
//    public string locale;
//    }

//public struct StructFilterItemPair
//    {
//    public string name;
//    public string value;
//    }

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

    //public string sql;

    public StructResult result;
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

    public StructResult result;
    }

public struct StructProduct
    {
    public int id;
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
    public string picture_remote;
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



}

