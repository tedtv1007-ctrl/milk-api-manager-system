# milk_api_client.PiiMaskingApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_pii_masking_get**](PiiMaskingApi.md#api_pii_masking_get) | **GET** /api/PiiMasking | Retrieves all PII masking rules.
[**api_pii_masking_id_delete**](PiiMaskingApi.md#api_pii_masking_id_delete) | **DELETE** /api/PiiMasking/{id} | Deletes a PII masking rule from the system and APISIX.
[**api_pii_masking_id_put**](PiiMaskingApi.md#api_pii_masking_id_put) | **PUT** /api/PiiMasking/{id} | Updates an existing PII masking rule.
[**api_pii_masking_post**](PiiMaskingApi.md#api_pii_masking_post) | **POST** /api/PiiMasking | Creates a new PII masking rule governing APISIX traffic.


# **api_pii_masking_get**
> List[PiiMaskingRule] api_pii_masking_get()

Retrieves all PII masking rules.

### Example


```python
import milk_api_client
from milk_api_client.models.pii_masking_rule import PiiMaskingRule
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
    api_instance = milk_api_client.PiiMaskingApi(api_client)

    try:
        # Retrieves all PII masking rules.
        api_response = api_instance.api_pii_masking_get()
        print("The response of PiiMaskingApi->api_pii_masking_get:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling PiiMaskingApi->api_pii_masking_get: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**List[PiiMaskingRule]**](PiiMaskingRule.md)

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

# **api_pii_masking_id_delete**
> api_pii_masking_id_delete(id)

Deletes a PII masking rule from the system and APISIX.

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
    api_instance = milk_api_client.PiiMaskingApi(api_client)
    id = 56 # int | The rule ID to delete.

    try:
        # Deletes a PII masking rule from the system and APISIX.
        api_instance.api_pii_masking_id_delete(id)
    except Exception as e:
        print("Exception when calling PiiMaskingApi->api_pii_masking_id_delete: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**| The rule ID to delete. | 

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**204** | No Content |  -  |
**404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_pii_masking_id_put**
> api_pii_masking_id_put(id, pii_masking_rule=pii_masking_rule)

Updates an existing PII masking rule.

### Example


```python
import milk_api_client
from milk_api_client.models.pii_masking_rule import PiiMaskingRule
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
    api_instance = milk_api_client.PiiMaskingApi(api_client)
    id = 56 # int | The rule ID to update.
    pii_masking_rule = milk_api_client.PiiMaskingRule() # PiiMaskingRule | The new rule content. (optional)

    try:
        # Updates an existing PII masking rule.
        api_instance.api_pii_masking_id_put(id, pii_masking_rule=pii_masking_rule)
    except Exception as e:
        print("Exception when calling PiiMaskingApi->api_pii_masking_id_put: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | **int**| The rule ID to update. | 
 **pii_masking_rule** | [**PiiMaskingRule**](PiiMaskingRule.md)| The new rule content. | [optional] 

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**204** | No Content |  -  |
**400** | Bad Request |  -  |
**404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_pii_masking_post**
> PiiMaskingRule api_pii_masking_post(pii_masking_rule=pii_masking_rule)

Creates a new PII masking rule governing APISIX traffic.

### Example


```python
import milk_api_client
from milk_api_client.models.pii_masking_rule import PiiMaskingRule
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
    api_instance = milk_api_client.PiiMaskingApi(api_client)
    pii_masking_rule = milk_api_client.PiiMaskingRule() # PiiMaskingRule | The rule definition. (optional)

    try:
        # Creates a new PII masking rule governing APISIX traffic.
        api_response = api_instance.api_pii_masking_post(pii_masking_rule=pii_masking_rule)
        print("The response of PiiMaskingApi->api_pii_masking_post:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling PiiMaskingApi->api_pii_masking_post: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **pii_masking_rule** | [**PiiMaskingRule**](PiiMaskingRule.md)| The rule definition. | [optional] 

### Return type

[**PiiMaskingRule**](PiiMaskingRule.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**201** | Created |  -  |
**400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

