# ApiTestScenario


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**service_id** | **int** |  | 
**name** | **str** |  | 
**endpoint** | **str** |  | 
**http_method** | **str** |  | [optional] 
**expected_status_code** | **int** |  | [optional] 
**last_result** | **str** |  | [optional] 
**last_run_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.api_test_scenario import ApiTestScenario

# TODO update the JSON string below
json = "{}"
# create an instance of ApiTestScenario from a JSON string
api_test_scenario_instance = ApiTestScenario.from_json(json)
# print the JSON string representation of the object
print(ApiTestScenario.to_json())

# convert the object into a dict
api_test_scenario_dict = api_test_scenario_instance.to_dict()
# create an instance of ApiTestScenario from a dict
api_test_scenario_from_dict = ApiTestScenario.from_dict(api_test_scenario_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


