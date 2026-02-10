from flask import Flask, request, jsonify
import requests
import os

app = Flask(__name__)

# APISIX Admin API Config
# Use localhost if running locally, or environment variable
APISIX_ADMIN_KEY = os.getenv("APISIX_ADMIN_KEY", "edd1c9f034335f136f87ad84b625c8f1")
APISIX_ADMIN_URL = os.getenv("APISIX_ADMIN_URL", "http://localhost:9180/apisix/admin")

@app.route('/api/v1/routes', methods=['GET'])
def get_routes():
    try:
        resp = requests.get(f"{APISIX_ADMIN_URL}/routes", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        return jsonify(resp.json()), resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Blacklist', methods=['GET'])
def get_blacklist():
    try:
        resp = requests.get(f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        if resp.status_code == 404:
            return jsonify([]), 200
        
        data = resp.json()
        blacklist = data.get('value', {}).get('blacklist', [])
        return jsonify(blacklist), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Blacklist', methods=['POST'])
def update_blacklist():
    data = request.json
    ip = data.get('ip')
    action = data.get('action', 'add') # add | remove
    
    if not ip:
        return jsonify({"error": "IP is required"}), 400

    try:
        # Fetch current metadata
        meta_resp = requests.get(f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        
        if meta_resp.status_code == 404:
            metadata_value = {"blacklist": []}
        else:
            metadata_value = meta_resp.json().get('value', {"blacklist": []})
        
        blacklist = set(metadata_value.get('blacklist', []))
        if action == 'add':
            blacklist.add(ip)
        elif action == 'remove':
            blacklist.discard(ip)
            
        # Update metadata
        update_resp = requests.put(
            f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker",
            headers={"X-API-KEY": APISIX_ADMIN_KEY},
            json={"blacklist": list(blacklist)}
        )
        return jsonify({"message": f"IP {ip} {action}ed successfully"}), update_resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
