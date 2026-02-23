# AlertRule


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **str** |  | [optional] 
**name** | **str** |  | 
**metric_name** | **str** |  | 
**threshold** | **float** |  | [optional] 
**duration** | **str** |  | [optional] 
**is_enabled** | **bool** |  | [optional] 
**notification_channels** | **List[str]** |  | [optional] 
**severity** | **str** |  | [optional] 

## Example

```python
from milk_api_client.models.alert_rule import AlertRule

# TODO update the JSON string below
json = "{}"
# create an instance of AlertRule from a JSON string
alert_rule_instance = AlertRule.from_json(json)
# print the JSON string representation of the object
print(AlertRule.to_json())

# convert the object into a dict
alert_rule_dict = alert_rule_instance.to_dict()
# create an instance of AlertRule from a dict
alert_rule_from_dict = AlertRule.from_dict(alert_rule_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


