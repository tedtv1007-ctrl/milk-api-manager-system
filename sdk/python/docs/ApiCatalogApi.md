# milk_api_client.ApiCatalogApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_api_catalog_get**](ApiCatalogApi.md#api_api_catalog_get) | **GET** /api/ApiCatalog | 
[**api_api_catalog_register_post**](ApiCatalogApi.md#api_api_catalog_register_post) | **POST** /api/ApiCatalog/register | 


# **api_api_catalog_get**
> List[ApiServiceMetadata] api_api_catalog_get()

### Example


```python
import milk_api_client
from milk_api_client.models.api_service_metadata import ApiServiceMetadata
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
    api_instance = milk_api_client.ApiCatalogApi(api_client)

    try:
        api_response = api_instance.api_api_catalog_get()
        print("The response of ApiCatalogApi->api_api_catalog_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling ApiCatalogApi->api_api_catalog_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[ApiServiceMetadata]**](ApiServiceMetadata.md)

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

# **api_api_catalog_register_post**
> api_api_catalog_register_post(api_service_metadata=api_service_metadata)

### Example


```python
import milk_api_client
from milk_api_client.models.api_service_metadata import ApiServiceMetadata
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
    api_instance = milk_api_client.ApiCatalogApi(api_client)
    api_service_metadata = milk_api_client.ApiServiceMetadata() # ApiServiceMetadata |  (optional)

    try:
        api_instance.api_api_catalog_register_post(api_service_metadata=api_service_metadata)
    except Exception as e:
        print("Exception when calling ApiCatalogApi->api_api_catalog_register_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **api_service_metadata** | [**ApiServiceMetadata**](ApiServiceMetadata.md)|  | [optional] 

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: Not defined

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

