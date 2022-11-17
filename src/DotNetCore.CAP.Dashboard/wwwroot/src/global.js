let baseURL = "";

switch (import.meta.env.MODE) {
    case 'development':
        baseURL = "/cap/api";
        break
    default:
        baseURL = window.serverUrl;
        break
}

export default baseURL;