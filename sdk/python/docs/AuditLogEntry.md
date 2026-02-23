# AuditLogEntry


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**timestamp** | **datetime** |  | [optional] 
**user** | **str** |  | [optional] 
**action** | **str** |  | [optional] 
**resource** | **str** |  | [optional] 
**status_code** | **int** |  | [optional] 
**details** | **object** |  | [optional] 
**details_json** | **str** |  | [optional] 
**correlation_id** | **str** |  | [optional] 
**operator_ip** | **str** |  | [optional] 
**request_id** | **str** |  | [optional] 

## Example

```python
from milk_api_client.models.audit_log_entry import AuditLogEntry

# TODO update the JSON string below
json = "{}"
# create an instance of AuditLogEntry from a JSON string
audit_log_entry_instance = AuditLogEntry.from_json(json)
# print the JSON string representation of the object
print(AuditLogEntry.to_json())

# convert the object into a dict
audit_log_entry_dict = audit_log_entry_instance.to_dict()
# create an instance of AuditLogEntry from a dict
audit_log_entry_from_dict = AuditLogEntry.from_dict(audit_log_entry_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


