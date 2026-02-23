# milk_api_client.ConsumerGroupApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_consumer_group_get**](ConsumerGroupApi.md#api_consumer_group_get) | **GET** /api/ConsumerGroup | 
[**api_consumer_group_id_delete**](ConsumerGroupApi.md#api_consumer_group_id_delete) | **DELETE** /api/ConsumerGroup/{id} | 
[**api_consumer_group_id_put**](ConsumerGroupApi.md#api_consumer_group_id_put) | **PUT** /api/ConsumerGroup/{id} | 


# **api_consumer_group_get**
> api_consumer_group_get()

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
    api_instance = milk_api_client.ConsumerGroupApi(api_client)

    try:
        api_instance.api_consumer_group_get()
    except Exception as e:
        print("Exception when calling ConsumerGroupApi->api_consumer_group_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

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

# **api_consumer_group_id_delete**
> api_consumer_group_id_delete(id)

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
    api_instance = milk_api_client.ConsumerGroupApi(api_client)
    id = 'id_example' # str | 

    try:
        api_instance.api_consumer_group_id_delete(id)
    except Exception as e:
        print("Exception when calling ConsumerGroupApi->api_consumer_group_id_delete: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **str**|  | 

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

# **api_consumer_group_id_put**
> api_consumer_group_id_put(id, consumer_group=consumer_group)

### Example


```python
import milk_api_client
from milk_api_client.models.consumer_group import ConsumerGroup
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
    api_instance = milk_api_client.ConsumerGroupApi(api_client)
    id = 'id_example' # str | 
    consumer_group = milk_api_client.ConsumerGroup() # ConsumerGroup |  (optional)

    try:
        api_instance.api_consumer_group_id_put(id, consumer_group=consumer_group)
    except Exception as e:
        print("Exception when calling ConsumerGroupApi->api_consumer_group_id_put: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **str**|  | 
 **consumer_group** | [**ConsumerGroup**](ConsumerGroup.md)|  | [optional] 

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

