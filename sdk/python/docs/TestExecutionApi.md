# milk_api_client.TestExecutionApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_test_execution_run_id_post**](TestExecutionApi.md#api_test_execution_run_id_post) | **POST** /api/TestExecution/run/{id} | 
[**api_test_execution_scenarios_post**](TestExecutionApi.md#api_test_execution_scenarios_post) | **POST** /api/TestExecution/scenarios | 
[**api_test_execution_scenarios_service_id_get**](TestExecutionApi.md#api_test_execution_scenarios_service_id_get) | **GET** /api/TestExecution/scenarios/{serviceId} | 


# **api_test_execution_run_id_post**
> api_test_execution_run_id_post(id)

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
    api_instance = milk_api_client.TestExecutionApi(api_client)
    id = 56 # int | 

    try:
        api_instance.api_test_execution_run_id_post(id)
    except Exception as e:
        print("Exception when calling TestExecutionApi->api_test_execution_run_id_post: %s\n" % e)
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

# **api_test_execution_scenarios_post**
> ApiTestScenario api_test_execution_scenarios_post(api_test_scenario=api_test_scenario)

### Example


```python
import milk_api_client
from milk_api_client.models.api_test_scenario import ApiTestScenario
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
    api_instance = milk_api_client.TestExecutionApi(api_client)
    api_test_scenario = milk_api_client.ApiTestScenario() # ApiTestScenario |  (optional)

    try:
        api_response = api_instance.api_test_execution_scenarios_post(api_test_scenario=api_test_scenario)
        print("The response of TestExecutionApi->api_test_execution_scenarios_post:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling TestExecutionApi->api_test_execution_scenarios_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **api_test_scenario** | [**ApiTestScenario**](ApiTestScenario.md)|  | [optional] 

### Return type

[**ApiTestScenario**](ApiTestScenario.md)

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

# **api_test_execution_scenarios_service_id_get**
> List[ApiTestScenario] api_test_execution_scenarios_service_id_get(service_id)

### Example


```python
import milk_api_client
from milk_api_client.models.api_test_scenario import ApiTestScenario
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
    api_instance = milk_api_client.TestExecutionApi(api_client)
    service_id = 56 # int | 

    try:
        api_response = api_instance.api_test_execution_scenarios_service_id_get(service_id)
        print("The response of TestExecutionApi->api_test_execution_scenarios_service_id_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling TestExecutionApi->api_test_execution_scenarios_service_id_get: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **service_id** | **int**|  | 

### Return type

[**List[ApiTestScenario]**](ApiTestScenario.md)

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

