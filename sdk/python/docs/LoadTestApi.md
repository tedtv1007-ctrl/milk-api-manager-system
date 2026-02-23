# milk_api_client.LoadTestApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_load_test_run_post**](LoadTestApi.md#api_load_test_run_post) | **POST** /api/LoadTest/run | 


# **api_load_test_run_post**
> api_load_test_run_post(url=url, vus=vus, duration=duration)

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
    api_instance = milk_api_client.LoadTestApi(api_client)
    url = 'url_example' # str |  (optional)
    vus = 10 # int |  (optional) (default to 10)
    duration = 30 # int |  (optional) (default to 30)

    try:
        api_instance.api_load_test_run_post(url=url, vus=vus, duration=duration)
    except Exception as e:
        print("Exception when calling LoadTestApi->api_load_test_run_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **url** | **str**|  | [optional] 
 **vus** | **int**|  | [optional] [default to 10]
 **duration** | **int**|  | [optional] [default to 30]

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

