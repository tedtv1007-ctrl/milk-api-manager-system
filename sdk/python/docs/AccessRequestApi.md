# milk_api_client.AccessRequestApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_access_request_get**](AccessRequestApi.md#api_access_request_get) | **GET** /api/AccessRequest | 
[**api_access_request_id_approve_post**](AccessRequestApi.md#api_access_request_id_approve_post) | **POST** /api/AccessRequest/{id}/approve | 
[**api_access_request_id_reject_post**](AccessRequestApi.md#api_access_request_id_reject_post) | **POST** /api/AccessRequest/{id}/reject | 
[**api_access_request_submit_post**](AccessRequestApi.md#api_access_request_submit_post) | **POST** /api/AccessRequest/submit | 


# **api_access_request_get**
> List[AccessRequest] api_access_request_get()

### Example


```python
import milk_api_client
from milk_api_client.models.access_request import AccessRequest
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
    api_instance = milk_api_client.AccessRequestApi(api_client)

    try:
        api_response = api_instance.api_access_request_get()
        print("The response of AccessRequestApi->api_access_request_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AccessRequestApi->api_access_request_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[AccessRequest]**](AccessRequest.md)

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

# **api_access_request_id_approve_post**
> api_access_request_id_approve_post(id, comment=comment)

### Example


```python
import milk_api_client
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
    api_instance = milk_api_client.AccessRequestApi(api_client)
    id = 56 # int | 
    comment = '' # str |  (optional) (default to '')

    try:
        api_instance.api_access_request_id_approve_post(id, comment=comment)
    except Exception as e:
        print("Exception when calling AccessRequestApi->api_access_request_id_approve_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**|  | 
 **comment** | **str**|  | [optional] [default to &#39;&#39;]

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_access_request_id_reject_post**
> api_access_request_id_reject_post(id, reason=reason)

### Example


```python
import milk_api_client
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
    api_instance = milk_api_client.AccessRequestApi(api_client)
    id = 56 # int | 
    reason = '' # str |  (optional) (default to '')

    try:
        api_instance.api_access_request_id_reject_post(id, reason=reason)
    except Exception as e:
        print("Exception when calling AccessRequestApi->api_access_request_id_reject_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**|  | 
 **reason** | **str**|  | [optional] [default to &#39;&#39;]

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_access_request_submit_post**
> AccessRequest api_access_request_submit_post(access_request=access_request)

### Example


```python
import milk_api_client
from milk_api_client.models.access_request import AccessRequest
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
    api_instance = milk_api_client.AccessRequestApi(api_client)
    access_request = milk_api_client.AccessRequest() # AccessRequest |  (optional)

    try:
        api_response = api_instance.api_access_request_submit_post(access_request=access_request)
        print("The response of AccessRequestApi->api_access_request_submit_post:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AccessRequestApi->api_access_request_submit_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **access_request** | [**AccessRequest**](AccessRequest.md)|  | [optional] 

### Return type

[**AccessRequest**](AccessRequest.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | Success |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

