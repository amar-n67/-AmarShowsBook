const auth = JSON.parse(localStorage.getItem("auth"));

function hasPermission(permission){
    return auth.permissions.includes(permission);
}

function hasRole(role){
    return auth.role===role;
}