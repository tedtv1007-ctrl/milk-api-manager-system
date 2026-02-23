# milk_api_client.AlertRulesApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_alert_rules_get**](AlertRulesApi.md#api_alert_rules_get) | **GET** /api/AlertRules | 
[**api_alert_rules_id_delete**](AlertRulesApi.md#api_alert_rules_id_delete) | **DELETE** /api/AlertRules/{id} | 
[**api_alert_rules_id_toggle_put**](AlertRulesApi.md#api_alert_rules_id_toggle_put) | **PUT** /api/AlertRules/{id}/toggle | 
[**api_alert_rules_post**](AlertRulesApi.md#api_alert_rules_post) | **POST** /api/AlertRules | 


# **api_alert_rules_get**
> List[AlertRule] api_alert_rules_get()

### Example


```python
import milk_api_client
from milk_api_client.models.alert_rule import AlertRule
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
    api_instance = milk_api_client.AlertRulesApi(api_client)

    try:
        api_response = api_instance.api_alert_rules_get()
        print("The response of AlertRulesApi->api_alert_rules_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AlertRulesApi->api_alert_rules_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[AlertRule]**](AlertRule.md)

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

# **api_alert_rules_id_delete**
> api_alert_rules_id_delete(id)

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
    api_instance = milk_api_client.AlertRulesApi(api_client)
    id = 'id_example' # str | 

    try:
        api_instance.api_alert_rules_id_delete(id)
    except Exception as e:
        print("Exception when calling AlertRulesApi->api_alert_rules_id_delete: %s\n" % e)
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

# **api_alert_rules_id_toggle_put**
> api_alert_rules_id_toggle_put(id)

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
    api_instance = milk_api_client.AlertRulesApi(api_client)
    id = 'id_example' # str | 

    try:
        api_instance.api_alert_rules_id_toggle_put(id)
    except Exception as e:
        print("Exception when calling AlertRulesApi->api_alert_rules_id_toggle_put: %s\n" % e)
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

# **api_alert_rules_post**
> AlertRule api_alert_rules_post(alert_rule=alert_rule)

### Example


```python
import milk_api_client
from milk_api_client.models.alert_rule import AlertRule
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
    api_instance = milk_api_client.AlertRulesApi(api_client)
    alert_rule = milk_api_client.AlertRule() # AlertRule |  (optional)

    try:
        api_response = api_instance.api_alert_rules_post(alert_rule=alert_rule)
        print("The response of AlertRulesApi->api_alert_rules_post:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling AlertRulesApi->api_alert_rules_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **alert_rule** | [**AlertRule**](AlertRule.md)|  | [optional] 

### Return type

[**AlertRule**](AlertRule.md)

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

