# WhitelistUpdateRequest


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ip_cidr** | **str** |  | [optional] 
**action** | **str** |  | [optional] 
**reason** | **str** |  | [optional] 
**added_by** | **str** |  | [optional] 
**expires_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.whitelist_update_request import WhitelistUpdateRequest

# TODO update the JSON string below
json = "{}"
# create an instance of WhitelistUpdateRequest from a JSON string
whitelist_update_request_instance = WhitelistUpdateRequest.from_json(json)
# print the JSON string representation of the object
print(WhitelistUpdateRequest.to_json())

# convert the object into a dict
whitelist_update_request_dict = whitelist_update_request_instance.to_dict()
# create an instance of WhitelistUpdateRequest from a dict
whitelist_update_request_from_dict = WhitelistUpdateRequest.from_dict(whitelist_update_request_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


