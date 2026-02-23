# milk_api_client.AnalyticsApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_analytics_errors_get**](AnalyticsApi.md#api_analytics_errors_get) | **GET** /api/Analytics/errors | 
[**api_analytics_latency_get**](AnalyticsApi.md#api_analytics_latency_get) | **GET** /api/Analytics/latency | 
[**api_analytics_requests_get**](AnalyticsApi.md#api_analytics_requests_get) | **GET** /api/Analytics/requests | 
[**api_analytics_sla_get**](AnalyticsApi.md#api_analytics_sla_get) | **GET** /api/Analytics/sla | 
[**api_analytics_top_slow_routes_get**](AnalyticsApi.md#api_analytics_top_slow_routes_get) | **GET** /api/Analytics/top-slow-routes | 


# **api_analytics_errors_get**
> api_analytics_errors_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)

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
    api_instance = milk_api_client.AnalyticsApi(api_client)
    consumer = 'consumer_example' # str |  (optional)
    route = 'route_example' # str |  (optional)
    start_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    end_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    step = 'step_example' # str |  (optional)

    try:
        api_instance.api_analytics_errors_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)
    except Exception as e:
        print("Exception when calling AnalyticsApi->api_analytics_errors_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **consumer** | **str**|  | [optional] 
 **route** | **str**|  | [optional] 
 **start_time** | **datetime**|  | [optional] 
 **end_time** | **datetime**|  | [optional] 
 **step** | **str**|  | [optional] 

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

# **api_analytics_latency_get**
> api_analytics_latency_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)

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
    api_instance = milk_api_client.AnalyticsApi(api_client)
    consumer = 'consumer_example' # str |  (optional)
    route = 'route_example' # str |  (optional)
    start_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    end_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    step = 'step_example' # str |  (optional)

    try:
        api_instance.api_analytics_latency_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)
    except Exception as e:
        print("Exception when calling AnalyticsApi->api_analytics_latency_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **consumer** | **str**|  | [optional] 
 **route** | **str**|  | [optional] 
 **start_time** | **datetime**|  | [optional] 
 **end_time** | **datetime**|  | [optional] 
 **step** | **str**|  | [optional] 

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

# **api_analytics_requests_get**
> api_analytics_requests_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)

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
    api_instance = milk_api_client.AnalyticsApi(api_client)
    consumer = 'consumer_example' # str |  (optional)
    route = 'route_example' # str |  (optional)
    start_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    end_time = '2013-10-20T19:20:30+01:00' # datetime |  (optional)
    step = 'step_example' # str |  (optional)

    try:
        api_instance.api_analytics_requests_get(consumer=consumer, route=route, start_time=start_time, end_time=end_time, step=step)
    except Exception as e:
        print("Exception when calling AnalyticsApi->api_analytics_requests_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **consumer** | **str**|  | [optional] 
 **route** | **str**|  | [optional] 
 **start_time** | **datetime**|  | [optional] 
 **end_time** | **datetime**|  | [optional] 
 **step** | **str**|  | [optional] 

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

# **api_analytics_sla_get**
> api_analytics_sla_get()

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
    api_instance = milk_api_client.AnalyticsApi(api_client)

    try:
        api_instance.api_analytics_sla_get()
    except Exception as e:
        print("Exception when calling AnalyticsApi->api_analytics_sla_get: %s\n" % e)
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

# **api_analytics_top_slow_routes_get**
> api_analytics_top_slow_routes_get(limit=limit)

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
    api_instance = milk_api_client.AnalyticsApi(api_client)
    limit = 5 # int |  (optional) (default to 5)

    try:
        api_instance.api_analytics_top_slow_routes_get(limit=limit)
    except Exception as e:
        print("Exception when calling AnalyticsApi->api_analytics_top_slow_routes_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **limit** | **int**|  | [optional] [default to 5]

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

