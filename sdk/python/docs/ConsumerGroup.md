# ConsumerGroup


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **str** |  | [optional] 
**plugins** | **Dict[str, object]** |  | [optional] 

## Example

```python
from milk_api_client.models.consumer_group import ConsumerGroup

# TODO update the JSON string below
json = "{}"
# create an instance of ConsumerGroup from a JSON string
consumer_group_instance = ConsumerGroup.from_json(json)
# print the JSON string representation of the object
print(ConsumerGroup.to_json())

# convert the object into a dict
consumer_group_dict = consumer_group_instance.to_dict()
# create an instance of ConsumerGroup from a dict
consumer_group_from_dict = ConsumerGroup.from_dict(consumer_group_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


