# BlacklistEntry


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**ip_or_cidr** | **str** |  | 
**reason** | **str** |  | [optional] 
**added_by** | **str** |  | [optional] 
**added_at** | **datetime** |  | [optional] 
**expires_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.blacklist_entry import BlacklistEntry

# TODO update the JSON string below
json = "{}"
# create an instance of BlacklistEntry from a JSON string
blacklist_entry_instance = BlacklistEntry.from_json(json)
# print the JSON string representation of the object
print(BlacklistEntry.to_json())

# convert the object into a dict
blacklist_entry_dict = blacklist_entry_instance.to_dict()
# create an instance of BlacklistEntry from a dict
blacklist_entry_from_dict = BlacklistEntry.from_dict(blacklist_entry_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


