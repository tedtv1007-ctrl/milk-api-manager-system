# milk_api_client.AuditLogsApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_audit_logs_export_get**](AuditLogsApi.md#api_audit_logs_export_get) | **GET** /api/AuditLogs/export | 
[**api_audit_logs_get**](AuditLogsApi.md#api_audit_logs_get) | **GET** /api/AuditLogs | 
[**api_audit_logs_stats_get**](AuditLogsApi.md#api_audit_logs_stats_get) | **GET** /api/AuditLogs/stats | 


# **api_audit_logs_export_get**
> api_audit_logs_export_get()

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
    api_instance = milk_api_client.AuditLogsApi(api_client)

    try:
        api_instance.api_audit_logs_export_get()
    except Exception as e:
        print("Exception when calling AuditLogsApi->api_audit_logs_export_get: %s\n" % e)
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

# **api_audit_logs_get**
> List[AuditLogEntry] api_audit_logs_get(limit=limit)

### Example


```python
import milk_api_client
from milk_api_client.models.audit_log_entry import AuditLogEntry
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
    api_instance = milk_api_client.AuditLogsApi(api_client)
    limit = 100 # int |  (optional) (default to 100)

    try:
        api_response = api_instance.api_audit_logs_get(limit=limit)
        print("The response of AuditLogsApi->api_audit_logs_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AuditLogsApi->api_audit_logs_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **limit** | **int**|  | [optional] [default to 100]

### Return type

[**List[AuditLogEntry]**](AuditLogEntry.md)

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

# **api_audit_logs_stats_get**
> Dict[str, int] api_audit_logs_stats_get()

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
    api_instance = milk_api_client.AuditLogsApi(api_client)

    try:
        api_response = api_instance.api_audit_logs_stats_get()
        print("The response of AuditLogsApi->api_audit_logs_stats_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AuditLogsApi->api_audit_logs_stats_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

**Dict[str, int]**

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

