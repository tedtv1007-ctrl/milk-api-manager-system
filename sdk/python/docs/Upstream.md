# Upstream


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**type** | **str** |  | [optional] 
**nodes** | **Dict[str, int]** |  | [optional] 

## Example

```python
from milk_api_client.models.upstream import Upstream

# TODO update the JSON string below
json = "{}"
# create an instance of Upstream from a JSON string
upstream_instance = Upstream.from_json(json)
# print the JSON string representation of the object
print(Upstream.to_json())

# convert the object into a dict
upstream_dict = upstream_instance.to_dict()
# create an instance of Upstream from a dict
upstream_from_dict = Upstream.from_dict(upstream_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


