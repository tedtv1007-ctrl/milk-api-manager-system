# PiiMaskingRule


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**route_id** | **str** |  | 
**field_path** | **str** |  | 
**regex_pattern** | **str** |  | 
**replace_pattern** | **str** |  | 
**is_active** | **bool** |  | [optional] 
**updated_at** | **datetime** |  | [optional] 
**description** | **str** |  | [optional] 

## Example

```python
from milk_api_client.models.pii_masking_rule import PiiMaskingRule

# TODO update the JSON string below
json = "{}"
# create an instance of PiiMaskingRule from a JSON string
pii_masking_rule_instance = PiiMaskingRule.from_json(json)
# print the JSON string representation of the object
print(PiiMaskingRule.to_json())

# convert the object into a dict
pii_masking_rule_dict = pii_masking_rule_instance.to_dict()
# create an instance of PiiMaskingRule from a dict
pii_masking_rule_from_dict = PiiMaskingRule.from_dict(pii_masking_rule_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


