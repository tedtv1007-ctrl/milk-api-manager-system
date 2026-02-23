# milk_api_client.BlacklistApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_blacklist_get**](BlacklistApi.md#api_blacklist_get) | **GET** /api/Blacklist | Retrieves the current IP blacklist.
[**api_blacklist_post**](BlacklistApi.md#api_blacklist_post) | **POST** /api/Blacklist | Adds or removes an IP/CIDR to/from the blacklist.


# **api_blacklist_get**
> List[BlacklistEntry] api_blacklist_get()

Retrieves the current IP blacklist.

### Example


```python
import milk_api_client
from milk_api_client.models.blacklist_entry import BlacklistEntry
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
    api_instance = milk_api_client.BlacklistApi(api_client)

    try:
        # Retrieves the current IP blacklist.
        api_response = api_instance.api_blacklist_get()
        print("The response of BlacklistApi->api_blacklist_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling BlacklistApi->api_blacklist_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[BlacklistEntry]**](BlacklistEntry.md)

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

# **api_blacklist_post**
> api_blacklist_post(blacklist_update_request=blacklist_update_request)

Adds or removes an IP/CIDR to/from the blacklist.

### Example


```python
import milk_api_client
from milk_api_client.models.blacklist_update_request import BlacklistUpdateRequest
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
    api_instance = milk_api_client.BlacklistApi(api_client)
    blacklist_update_request = milk_api_client.BlacklistUpdateRequest() # BlacklistUpdateRequest | The blacklist update instruction. (optional)

    try:
        # Adds or removes an IP/CIDR to/from the blacklist.
        api_instance.api_blacklist_post(blacklist_update_request=blacklist_update_request)
    except Exception as e:
        print("Exception when calling BlacklistApi->api_blacklist_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **blacklist_update_request** | [**BlacklistUpdateRequest**](BlacklistUpdateRequest.md)| The blacklist update instruction. | [optional] 

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

