# milk_api_client.KeysApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_keys_consumer_name_rotate_post**](KeysApi.md#api_keys_consumer_name_rotate_post) | **POST** /api/Keys/{consumerName}/rotate | 
[**api_keys_post**](KeysApi.md#api_keys_post) | **POST** /api/Keys | 


# **api_keys_consumer_name_rotate_post**
> api_keys_consumer_name_rotate_post(consumer_name)

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
    api_instance = milk_api_client.KeysApi(api_client)
    consumer_name = 'consumer_name_example' # str | 

    try:
        api_instance.api_keys_consumer_name_rotate_post(consumer_name)
    except Exception as e:
        print("Exception when calling KeysApi->api_keys_consumer_name_rotate_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **consumer_name** | **str**|  | 

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

# **api_keys_post**
> api_keys_post(create_key_request=create_key_request)

### Example


```python
import milk_api_client
from milk_api_client.models.create_key_request import CreateKeyRequest
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
    api_instance = milk_api_client.KeysApi(api_client)
    create_key_request = milk_api_client.CreateKeyRequest() # CreateKeyRequest |  (optional)

    try:
        api_instance.api_keys_post(create_key_request=create_key_request)
    except Exception as e:
        print("Exception when calling KeysApi->api_keys_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **create_key_request** | [**CreateKeyRequest**](CreateKeyRequest.md)|  | [optional] 

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

