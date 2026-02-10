local core = require("apisix.core")
local plugin = require("apisix.plugin")

local schema = {
    type = "object",
    properties = {
        block_message = {type = "string", default = "Your IP is blocked by Milk APIM."},
    },
}

local _M = {
    version = 0.2,
    priority = 10000,
    name = "traffic-blocker",
    schema = schema,
}

function _M.check_schema(conf)
    return core.schema.check(schema, conf)
end

function _M.access(conf, ctx)
    local ip = core.request.get_remote_client_ip(ctx)
    
    -- Fetch blacklist from plugin metadata (synced via Admin API)
    local metadata = plugin.plugin_metadata(_M.name)
    if not metadata or not metadata.value or not metadata.value.blacklist then
        return
    end

    local blacklist = metadata.value.blacklist
    for _, blocked_ip in ipairs(blacklist) do
        if ip == blocked_ip then
            return 403, {message = conf.block_message}
        end
    end
end

return _M
