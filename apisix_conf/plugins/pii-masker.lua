local core = require("apisix.core")
local cjson = require("cjson.safe")
local ngx = ngx
local string = string
local table = table

local schema = {
    type = "object",
    properties = {
        rules = {
            type = "array",
            items = {
                type = "object",
                properties = {
                    field_name = {type = "string"},
                    regex = {type = "string"},
                    replace = {type = "string"}
                },
                required = {"field_name", "regex", "replace"}
            }
        }
    }
}

local plugin_name = "pii-masker"

local _M = {
    version = 0.1,
    priority = 1000,
    name = plugin_name,
    schema = schema,
}

function _M.check_schema(conf)
    return core.schema.check(schema, conf)
end

-- Metrics
local metric_pii_masked
local function get_metric()
    if metric_pii_masked then return metric_pii_masked end

    local status, exporter = pcall(require, "apisix.plugins.prometheus.exporter")
    if status and exporter and exporter.registry then
        local valid
        valid, metric_pii_masked = pcall(function()
            return exporter.registry:counter(
                "pii_masked_total",
                "Total number of PII fields masked",
                {"route_id", "field"}
            )
        end)
        
        if not valid then
             core.log.error("failed to register pii_masked_total metric: ", metric_pii_masked)
             metric_pii_masked = nil
        end
    end
    return metric_pii_masked
end

local function split_path(path)
    local parts = {}
    for part in string.gmatch(path, "[^.]+") do
        table.insert(parts, part)
    end
    return parts
end

local function apply_regex(val, regex_pattern, replace_pattern)
    if type(val) == "string" or type(val) == "number" then
        -- Use "jo" for JIT compilation and caching of the regex
        local new_val, n, err = ngx.re.gsub(tostring(val), regex_pattern, replace_pattern, "jo")
        if not err and n > 0 then
            return new_val, true
        end
    end
    return val, false
end

local function process_node(node, parts, idx, regex, replace, on_replace)
    -- Handle array of objects (distribute key lookup to children)
    if type(node) == "table" and #node > 0 then
        for _, item in ipairs(node) do
            if type(item) == "table" then
                process_node(item, parts, idx, regex, replace, on_replace)
            end
        end
        return
    end

    if idx > #parts then return end
    
    local key = parts[idx]
    local val = node[key]
    
    if val == nil then return end

    if idx == #parts then
        -- Reached the target field
        local new_val, replaced = apply_regex(val, regex, replace)
        if replaced then
            node[key] = new_val
            if on_replace then on_replace() end
        end
    else
        -- Traverse deeper
        if type(val) == "table" then
            process_node(val, parts, idx + 1, regex, replace, on_replace)
        end
    end
end

function _M.body_filter(conf, ctx)
    local body = core.response.hold_body_chunk(ctx)
    if not body then
        return
    end

    -- Attempt to decode JSON
    local data, err = cjson.decode(body)
    if not data then
        -- Not valid JSON or decode failed, return original body
        ngx.arg[1] = body
        return
    end

    local route_id = (ctx.matched_route and ctx.matched_route.value and ctx.matched_route.value.id) or "unknown"

    if conf.rules then
        for _, rule in ipairs(conf.rules) do
            local parts = split_path(rule.field_name)
            if #parts > 0 then
                process_node(data, parts, 1, rule.regex, rule.replace, function()
                    local ctr = get_metric()
                    if ctr then
                        ctr:inc(1, {route_id, rule.field_name})
                    end
                end)
            end
        end
    end

    -- Re-encode and return
    local new_body = cjson.encode(data)
    ngx.arg[1] = new_body
end

return _M
