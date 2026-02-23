# ApiServiceMetadata


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**name** | **str** |  | 
**description** | **str** |  | [optional] 
**base_path** | **str** |  | 
**open_api_url** | **str** |  | 
**owner_team** | **str** |  | [optional] 
**is_public** | **bool** |  | [optional] 
**last_synced_at** | **datetime** |  | [optional] 

## Example

```python
from milk_api_client.models.api_service_metadata import ApiServiceMetadata

# TODO update the JSON string below
json = "{}"
# create an instance of ApiServiceMetadata from a JSON string
api_service_metadata_instance = ApiServiceMetadata.from_json(json)
# print the JSON string representation of the object
print(ApiServiceMetadata.to_json())

# convert the object into a dict
api_service_metadata_dict = api_service_metadata_instance.to_dict()
# create an instance of ApiServiceMetadata from a dict
api_service_metadata_from_dict = ApiServiceMetadata.from_dict(api_service_metadata_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


