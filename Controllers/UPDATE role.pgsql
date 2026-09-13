UPDATE public.user_role_mappings urm
SET is_active = true,
    assigned_by = 1,
    assigned_at = CURRENT_TIMESTAMP
FROM public."Users" u, public.roles r
WHERE urm.user_id = u."Id"
  AND urm.role_id = r.id
  AND u."Email" = 'arcanaamar67@gmail.com'
  AND r.role_code = 'AMAR_ADMIN';

UPDATE public.user_roles ur
SET is_active = true,
    assigned_by = 1,
    assigned_at = CURRENT_TIMESTAMP
FROM public."Users" u, public.roles r
WHERE ur.user_id = u."Id"
  AND ur.role_id = r.id
  AND u."Email" = 'arcanaamar67@gmail.com'
  AND r.role_code = 'AMAR_ADMIN';