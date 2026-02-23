# MockRule


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**route_id** | **str** |  | 
**response_status_code** | **int** |  | [optional] 
**response_body** | **str** |  | [optional] 
**content_type** | **str** |  | [optional] 
**is_enabled** | **bool** |  | [optional] 
**created_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.mock_rule import MockRule

# TODO update the JSON string below
json = "{}"
# create an instance of MockRule from a JSON string
mock_rule_instance = MockRule.from_json(json)
# print the JSON string representation of the object
print(MockRule.to_json())

# convert the object into a dict
mock_rule_dict = mock_rule_instance.to_dict()
# create an instance of MockRule from a dict
mock_rule_from_dict = MockRule.from_dict(mock_rule_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


