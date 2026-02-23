# WhitelistEntry


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**route_id** | **str** |  | 
**ip_cidr** | **str** |  | 
**reason** | **str** |  | [optional] 
**added_by** | **str** |  | [optional] 
**added_at** | **datetime** |  | [optional] 
**expires_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.whitelist_entry import WhitelistEntry

# TODO update the JSON string below
json = "{}"
# create an instance of WhitelistEntry from a JSON string
whitelist_entry_instance = WhitelistEntry.from_json(json)
# print the JSON string representation of the object
print(WhitelistEntry.to_json())

# convert the object into a dict
whitelist_entry_dict = whitelist_entry_instance.to_dict()
# create an instance of WhitelistEntry from a dict
whitelist_entry_from_dict = WhitelistEntry.from_dict(whitelist_entry_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


