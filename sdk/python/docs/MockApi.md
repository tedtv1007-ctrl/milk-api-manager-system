# milk_api_client.MockApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_mock_get**](MockApi.md#api_mock_get) | **GET** /api/Mock | 
[**api_mock_id_delete**](MockApi.md#api_mock_id_delete) | **DELETE** /api/Mock/{id} | 
[**api_mock_id_put**](MockApi.md#api_mock_id_put) | **PUT** /api/Mock/{id} | 
[**api_mock_post**](MockApi.md#api_mock_post) | **POST** /api/Mock | 


# **api_mock_get**
> List[MockRule] api_mock_get()

### Example


```python
import milk_api_client
from milk_api_client.models.mock_rule import MockRule
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
    api_instance = milk_api_client.MockApi(api_client)

    try:
        api_response = api_instance.api_mock_get()
        print("The response of MockApi->api_mock_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling MockApi->api_mock_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[MockRule]**](MockRule.md)

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

# **api_mock_id_delete**
> api_mock_id_delete(id)

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
    api_instance = milk_api_client.MockApi(api_client)
    id = 56 # int | 

    try:
        api_instance.api_mock_id_delete(id)
    except Exception as e:
        print("Exception when calling MockApi->api_mock_id_delete: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**|  | 

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

# **api_mock_id_put**
> api_mock_id_put(id, mock_rule=mock_rule)

### Example


```python
import milk_api_client
from milk_api_client.models.mock_rule import MockRule
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
    api_instance = milk_api_client.MockApi(api_client)
    id = 56 # int | 
    mock_rule = milk_api_client.MockRule() # MockRule |  (optional)

    try:
        api_instance.api_mock_id_put(id, mock_rule=mock_rule)
    except Exception as e:
        print("Exception when calling MockApi->api_mock_id_put: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**|  | 
 **mock_rule** | [**MockRule**](MockRule.md)|  | [optional] 

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

# **api_mock_post**
> MockRule api_mock_post(mock_rule=mock_rule)

### Example


```python
import milk_api_client
from milk_api_client.models.mock_rule import MockRule
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
    api_instance = milk_api_client.MockApi(api_client)
    mock_rule = milk_api_client.MockRule() # MockRule |  (optional)

    try:
        api_response = api_instance.api_mock_post(mock_rule=mock_rule)
        print("The response of MockApi->api_mock_post:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling MockApi->api_mock_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **mock_rule** | [**MockRule**](MockRule.md)|  | [optional] 

### Return type

[**MockRule**](MockRule.md)

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

