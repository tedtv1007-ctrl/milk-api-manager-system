# BlacklistUpdateRequest


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ip** | **str** |  | [optional] 
**action** | **str** |  | [optional] 
**reason** | **str** |  | [optional] 
**added_by** | **str** |  | [optional] 
**expires_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.blacklist_update_request import BlacklistUpdateRequest

# TODO update the JSON string below
json = "{}"
# create an instance of BlacklistUpdateRequest from a JSON string
blacklist_update_request_instance = BlacklistUpdateRequest.from_json(json)
# print the JSON string representation of the object
print(BlacklistUpdateRequest.to_json())

# convert the object into a dict
blacklist_update_request_dict = blacklist_update_request_instance.to_dict()
# create an instance of BlacklistUpdateRequest from a dict
blacklist_update_request_from_dict = BlacklistUpdateRequest.from_dict(blacklist_update_request_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


