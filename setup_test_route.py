import requests
import json

url = "http://localhost:9180/apisix/admin/routes/test-ping"
headers = {
    "X-API-KEY": "edd1c9f034335f136f87ad84b625c88b",
    "Content-Type": "application/json"
}
# 使用 mocking 插件直接回傳，不依賴後端
data = {
    "uri": "/test-ping",
    "name": "Pure Gateway Performance Test",
    "plugins": {
        "mocking": {
            "response_status": 200,
            "response_example": "{\"status\":\"ok\",\"pii\":\"***-***-1234\"}",
            "content_type": "application/json"
        }
    }
}

try:
    response = requests.put(url, headers=headers, json=data)
    print(f"Status: {response.status_code}")
except Exception as e:
    print(f"Error: {e}")
