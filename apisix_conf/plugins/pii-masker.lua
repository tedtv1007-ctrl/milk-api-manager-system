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
        local new_val, _, err = ngx.re.gsub(tostring(val), regex_pattern, replace_pattern, "jo")
        if not err then
            return new_val
        end
    end
    return val
end

local function process_node(node, parts, idx, regex, replace)
    -- Handle array of objects (distribute key lookup to children)
    if type(node) == "table" and #node > 0 then
        for _, item in ipairs(node) do
            if type(item) == "table" then
                process_node(item, parts, idx, regex, replace)
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
        node[key] = apply_regex(val, regex, replace)
    else
        -- Traverse deeper
        if type(val) == "table" then
            process_node(val, parts, idx + 1, regex, replace)
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

    if conf.rules then
        for _, rule in ipairs(conf.rules) do
            local parts = split_path(rule.field_name)
            if #parts > 0 then
                process_node(data, parts, 1, rule.regex, rule.replace)
            end
        end
    end

    -- Re-encode and return
    local new_body = cjson.encode(data)
    ngx.arg[1] = new_body
end

return _M
