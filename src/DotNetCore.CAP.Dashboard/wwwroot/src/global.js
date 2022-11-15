let baseURL = "";

switch (process.env.NODE_ENV) {
    case 'development':
        baseURL = "/cap/api";
        break
    default:
        baseURL = window.serverUrl;
        break
}

export default baseURL;