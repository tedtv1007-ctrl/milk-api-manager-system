#!/bin/bash

# 設定 APISIX Admin API 地址
APISIX_ADMIN_URL="http://127.0.0.1:9080/apisix/admin"
API_KEY="edd1c9f034335f136f87ad84b625c8f1" # 預設 Key

echo "Configuring Consumers and Limits..."

# 1. Gold User (VIP): 每分鐘 1000 次
curl -i -X PUT $APISIX_ADMIN_URL/consumers \
-H "X-API-KEY: $API_KEY" \
-d '{
    "username": "gold_user",
    "plugins": {
        "key-auth": {
            "key": "gold-key"
        },
        "limit-count": {
            "count": 1000,
            "time_window": 60,
            "rejected_code": 429,
            "rejected_msg": "Rate Limit Exceeded (Gold Tier)"
        }
    }
}'

# 2. Silver User (Standard): 每分鐘 100 次
curl -i -X PUT $APISIX_ADMIN_URL/consumers \
-H "X-API-KEY: $API_KEY" \
-d '{
    "username": "silver_user",
    "plugins": {
        "key-auth": {
            "key": "silver-key"
        },
        "limit-count": {
            "count": 100,
            "time_window": 60,
            "rejected_code": 429,
            "rejected_msg": "Rate Limit Exceeded (Silver Tier)"
        }
    }
}'

echo "Done."
