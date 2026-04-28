
-- we now have new token module event types, so we need to alter the token_module_type enum to include the new event types
ALTER TYPE token_module_type ADD VALUE IF NOT EXISTS 'AssignAdminRoles';
ALTER TYPE token_module_type ADD VALUE IF NOT EXISTS 'RevokeAdminRoles';
ALTER TYPE token_module_type ADD VALUE IF NOT EXISTS 'UpdateMetadata';

-- 