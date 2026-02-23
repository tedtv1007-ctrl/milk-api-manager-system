# Route


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **str** |  | [optional] 
**name** | **str** |  | [optional] 
**uri** | **str** |  | [optional] 
**uris** | **List[str]** |  | [optional] 
**methods** | **List[str]** |  | [optional] 
**service_id** | **str** |  | [optional] 
**plugins** | **Dict[str, object]** |  | [optional] 

## Example

```python
from milk_api_client.models.route import Route

# TODO update the JSON string below
json = "{}"
# create an instance of Route from a JSON string
route_instance = Route.from_json(json)
# print the JSON string representation of the object
print(Route.to_json())

# convert the object into a dict
route_dict = route_instance.to_dict()
# create an instance of Route from a dict
route_from_dict = Route.from_dict(route_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


