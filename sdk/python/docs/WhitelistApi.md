# milk_api_client.WhitelistApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_whitelist_route_route_id_get**](WhitelistApi.md#api_whitelist_route_route_id_get) | **GET** /api/Whitelist/route/{routeId} | Retrieves the IP whitelist for a specific route.
[**api_whitelist_route_route_id_post**](WhitelistApi.md#api_whitelist_route_route_id_post) | **POST** /api/Whitelist/route/{routeId} | Adds or removes an IP/CIDR to/from a route&#39;s whitelist.


# **api_whitelist_route_route_id_get**
> List[WhitelistEntry] api_whitelist_route_route_id_get(route_id)

Retrieves the IP whitelist for a specific route.

### Example


```python
import milk_api_client
from milk_api_client.models.whitelist_entry import WhitelistEntry
from milk_api_client.rest import ApiException
from pprint import pprint

# Defining the host is optional and defaults to http://localhost
# See configuration.py for a list of all supported configuration parameters.
configuration = milk_api_client.Configuration(
    host = "http://localhost"
)


# Enter a context with an instance of the API client
with milk_api_client.ApiClient(configuration) as api_client:
    # Create an instance of the API class
    api_instance = milk_api_client.WhitelistApi(api_client)
    route_id = 'route_id_example' # str | The target APISIX route ID.

    try:
        # Retrieves the IP whitelist for a specific route.
        api_response = api_instance.api_whitelist_route_route_id_get(route_id)
        print("The response of WhitelistApi->api_whitelist_route_route_id_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling WhitelistApi->api_whitelist_route_route_id_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **route_id** | **str**| The target APISIX route ID. | 

### Return type

[**List[WhitelistEntry]**](WhitelistEntry.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_whitelist_route_route_id_post**
> api_whitelist_route_route_id_post(route_id, whitelist_update_request=whitelist_update_request)

Adds or removes an IP/CIDR to/from a route's whitelist.

### Example


```python
import milk_api_client
from milk_api_client.models.whitelist_update_request import WhitelistUpdateRequest
from milk_api_client.rest import ApiException
from pprint import pprint

# Defining the host is optional and defaults to http://localhost
# See configuration.py for a list of all supported configuration parameters.
configuration = milk_api_client.Configuration(
    host = "http://localhost"
)


# Enter a context with an instance of the API client
with milk_api_client.ApiClient(configuration) as api_client:
    # Create an instance of the API class
    api_instance = milk_api_client.WhitelistApi(api_client)
    route_id = 'route_id_example' # str | The target APISIX route ID.
    whitelist_update_request = milk_api_client.WhitelistUpdateRequest() # WhitelistUpdateRequest | The whitelist update instruction. (optional)

    try:
        # Adds or removes an IP/CIDR to/from a route's whitelist.
        api_instance.api_whitelist_route_route_id_post(route_id, whitelist_update_request=whitelist_update_request)
    except Exception as e:
        print("Exception when calling WhitelistApi->api_whitelist_route_route_id_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **route_id** | **str**| The target APISIX route ID. | 
 **whitelist_update_request** | [**WhitelistUpdateRequest**](WhitelistUpdateRequest.md)| The whitelist update instruction. | [optional] 

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |
**400** | Bad Request |  -  |
**500** | Server Error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

